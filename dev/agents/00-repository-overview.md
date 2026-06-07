# Repository Overview

## Repository Layout
- `Arts.sln` is the top-level solution.
- Mod projects are flattened: each mod directory contains its matching `.csproj`, `modinfo.json`, source (when applicable), and `assets/`.
- Main modules:
  - `CoreOfArts` (`modid: coreofartspatch`): shared systems, Harmony patches, class remaps, and asset patches.
  - `ArtOfGrowing` (`modid: artofgrowingpatch`): crop and plant gameplay changes.
  - `ArtOfCooking` (`modid: artofcookingpatch`): food and cooking systems.
  - `ArtsXSkills` (`modid: artsxskillspatch`): compatibility with XSkills.
  - `AOGBreedingAddon` (`modid: aogbreedingaddonpatch`): content-only addon with no C# source; its SDK project builds and packages its assets.
- Runtime dependency truth is in each module's `modinfo.json`. `Arts.sln` also contains build-order dependencies, which are not necessarily identical to runtime dependencies.
- `CakeBuild` is the single repository-wide build and packaging tool, not a game mod.

## Runtime Architecture Patterns
- Each code mod registers game types in `ModSystem.Start`; block, item, entity, and behavior strings must match their JSON declarations.
  - Example registration: `ArtOfGrowing/ArtOfGrowingModSystem.cs`.
  - Example JSON usage: `ArtOfGrowing/assets/artofgrowing/blocktypes/haystorage.json` (`"class": "AOGBlockHayStorage"`, `"entityClass": "AOGHayStorage"`).
- `CoreOfArts/CoreOfArtModSystem.cs` remaps selected vanilla block classes through `ClassRegistryNative.BlockClassToTypeMapping`.
- `CoreOfArts` applies Harmony patches client-side in `StartClientSide -> PatchGame -> PatchAll`, using Harmony id `coapatch`.
- `ArtOfGrowing` uses Harmony id `artofgrowing`.
- `ArtOfCooking` fills `CookingRecipe.NamingRegistry` with `AOCRecipeNames` during `AssetsFinalize`.

## Build and Packaging
- `global.json` accepts stable .NET 10 SDKs from `10.0.100` onward, including later .NET 10 feature bands and patches.
- Build the complete solution from the repository root:
```powershell
dotnet build .\Arts.sln
```
- `Directory.Build.props` supplies shared defaults and imports `dev/Local.Build.props` when present. Copy `dev/Local.Build.props.template` for local overrides.
- Important build properties:
  - `VintageStoryPath`: Vintage Story installation directory.
  - `VSDataPath`: Vintage Story data directory.
  - `ModDeployRoot`: directory where built mod folders are deployed.
- `Directory.Build.targets` reads each mod version from `modinfo.json` and prints `VintageStoryPath` and `ModDeployRoot` during mod builds.
- Packaging is centralized in `CakeBuild/CakeBuild.csproj`. Run it from `CakeBuild`:
```powershell
dotnet run --project .\CakeBuild.csproj -- --vs="<game-path>" --in="<deployed-mods-path>"
```
- Use `--projects=CoreOfArts,ArtOfGrowing` to package only selected projects. The default output is `Releases/`.
- Cake discovers flattened directories containing both `<DirectoryName>.csproj` and `modinfo.json`.
- Cake tasks run `ValidateJson -> Build -> Package`: validate asset JSON, clean and build each selected project into `ModDeployRoot`, then zip the deployed files.

## Project Conventions
- Prefixes map module ownership: `COA*` (CoreOfArts), `AOG*` (ArtOfGrowing), and `AOC*` (ArtOfCooking).
- Preserve established namespace and folder spellings, including `ArtOfGrowing/BlockEntites`.
- Assets are first-class project content. Each mod project copies `assets/**` and `modinfo.json` to its output.
- When moving or renaming registered types or assets, update C# registrations, JSON references, and project content rules together.
- All mod projects currently target `net10.0`.
- `ArtsXSkills` references `xlib.dll` and `xskills.dll` from `dev/dependencies` through `$(SolutionDir)`.

## Integration Points
- Game integration uses Vintage Story DLL references resolved from `$(VintageStoryPath)`.
- Cross-mod integration uses:
  - Runtime dependencies in `modinfo.json`.
  - Shared runtime types from `CoreOfArts`.
  - A compile-time `ProjectReference` from `ArtOfCooking` to `CoreOfArts`.
  - JSON patches, including `CoreOfArts/assets/coreofart/patches/**`.
- `Arts.sln` gives `ArtsXSkills` build-order dependencies on both `CoreOfArts` and `ArtOfGrowing`.
- No automated test project is currently present. Validation consists of JSON validation, build/package verification, and in-game testing.
