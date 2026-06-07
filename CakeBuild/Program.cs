using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Vintagestory.API.Common;

namespace CakeBuild;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class BuildContext : FrostingContext
{
    public string BuildConfiguration { get; }
    public bool SkipJsonValidation { get; }

    private List<string> ProjectFilter { get; }

    public IReadOnlyList<ProjectInfo> Projects { get; }

    public string InputPath { get; }

    public string OutputPath { get; }

    public string VSDataDirectory { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        BuildConfiguration = context.Argument("configuration", "Release");
        SkipJsonValidation = context.Argument("skipJsonValidation", false);
        OutputPath = context.Argument("out", "../Releases");
        InputPath = context.Argument("in", String.Empty);
        if (InputPath.Length == 0)
        {
            throw new ArgumentException("Mods build directory must be specified using the --in argument.");
        }

        VSDataDirectory = context.Argument("vs", String.Empty);
        if (VSDataDirectory.Length == 0)
        {
            throw new ArgumentException("Vintage Story installation directory must be specified using the --vs argument.");
        }

        var filterRaw = context.Argument("projects", string.Empty);

        ProjectFilter = string.IsNullOrWhiteSpace(filterRaw)
            ? []
            : filterRaw
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLowerInvariant())
                .ToList();

        Projects = DiscoverProjects(context);
    }

    private bool MatchesFilter(string project) =>
        !ProjectFilter.Any() || ProjectFilter.Contains(project.ToLowerInvariant());

    private bool ContainsFile(IDirectory dir, string fileName) => dir.GetFiles(fileName, SearchScope.Current).Any();

    private GlobberSettings IsModDirectory =>
        new()
        {
            Predicate = dir =>
            {
                var isDirectory = dir is { Exists: true };
                if (!isDirectory) return false;
                var name = dir.Path.GetDirectoryName();
                return name != "CakeBuild"
                       && MatchesFilter(name)
                       && ContainsFile(dir, "modinfo.json")
                       && ContainsFile(dir, "*.csproj");
            }
        };

    private List<ProjectInfo> DiscoverProjects(ICakeContext context) => context
        .GetDirectories("../*", IsModDirectory)
        .Select(dir =>
            {
                var modInfoPath = context.File($"{dir}/modinfo.json").Path.FullPath;
                var modInfo = context.DeserializeJsonFromFile<ModInfo>(modInfoPath);
                return new ProjectInfo
                {
                    Name = dir.GetDirectoryName(),
                    Version = modInfo.Version
                };
            }
        ).ToList();
}

public sealed class ProjectInfo
{
    public string Name { get; init; }
    public string Version { get; init; }
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.SkipJsonValidation)
        {
            return;
        }

        foreach (var project in context.Projects)
        {
            var jsonFiles = context.GetFiles($"../{project.Name}/assets/**/*.json");
            context.Log.Information("{0}: found {1} JSON files to validate.", project.Name, jsonFiles.Count);
            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(file.FullPath);
                    JToken.Parse(json);
                }
                catch (JsonException ex)
                {
                    throw new Exception(
                        $"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
                }
            }
        }
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var project in context.Projects)
        {
            context.DotNetClean($"../{project.Name}/{project.Name}.csproj",
                new DotNetCleanSettings
                {
                    Configuration = context.BuildConfiguration
                });

            context.DotNetMSBuild($"../{project.Name}/{project.Name}.csproj",
                new()
                {
                    Properties =
                    {
                        { "Configuration", [context.BuildConfiguration] },
                        { "VintageStoryPath", [context.VSDataDirectory] },
                        { "ModDeployRoot", [context.InputPath] },
                        { "SolutionDir", [".."] }
                    },
                });
        }
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var fullOutPath = context.File(context.OutputPath).Path.MakeAbsolute(context.Environment);
        context.EnsureDirectoryExists(context.OutputPath);
        context.Log.Information("Packaging {0} projects to {1}", context.Projects.Count, fullOutPath);
        context.Log.Information("If you want a different output direcotory, use the --out argument.");

        foreach (var project in context.Projects)
        {
            var tempDir = $"{context.OutputPath}/{project.Name}";
            var zipName = $"{project.Name}_{project.Version}.zip";
            var zipPath = $"{context.OutputPath}/{zipName}";
            if (context.FileExists(zipPath))
            {
                context.DeleteFile(zipPath);
            }
            context.EnsureDirectoryExists(tempDir);
            context.CopyDirectory($"{context.InputPath}/{project.Name}", tempDir);
            context.Zip(tempDir, $"{context.OutputPath}/{zipName}");
            context.Log.Information("Packaged {0} to {1}", project.Name, $"{fullOutPath.GetFilename()}/{zipName}");
            context.DeleteDirectory(tempDir, new() { Recursive = true, Force = true });
        }
    }
    
    [TaskName("Default")]
    [IsDependentOn(typeof(PackageTask))]
    public class DefaultTask : FrostingTask;
}
