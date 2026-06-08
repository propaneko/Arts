# Arts

The volunteer-supported fork of Fedarmens' Arts mod series for Vintage Story.

**This is not the place** for player instructions and colorful descriptions; we have
the Vintage Story ModDB for that.

## Modules

| Abbreviation | Project            | Mod ID                  | Purpose                                                          | Runtime dependencies | ModDB page                                      |
|--------------|--------------------|-------------------------|------------------------------------------------------------------|----------------------|-------------------------------------------------|
| COA          | `CoreOfArts`       | `coreofartspatch`       | Shared systems, class remaps, Harmony patches, and common assets | Vintage Story        | [Core of Arts [unofficial]](https://mods.vintagestory.at/show/mod/28843) |
| AOG          | `ArtOfGrowing`     | `artofgrowingpatch`     | Crop, plant, grass, and hay gameplay                             | COA                  | [Art of Growing [unofficial]](https://mods.vintagestory.at/show/mod/28845) |
| AOGBA        | `AOGBreedingAddon` | `aogbreedingaddonpatch` | Content-only plant breeding addon                                | COA, AOG             | [Art of Growing: Breeding Addon [unofficial]](https://mods.vintagestory.at/show/mod/28854) |
| AOC          | `ArtOfCooking`     | `artofcookingpatch`     | Food preparation, cookware, and cooking systems                  | COA                  | [Art of Cooking [unofficial]](https://mods.vintagestory.at/show/mod/28850) |
| AXS          | `ArtsXSkills`      | `artsxskillspatch`      | Compatibility between the Arts mods and XSkills                  | COA, XSkills         | [Arts XSkills [unofficial]](https://mods.vintagestory.at/show/mod/28848) |

## Development Setup

### Requirements

- .NET 10 SDK
- a Vintage Story installation (duh)
- **AXS is special.** For code and build purposes, the binaries are kept in `dev/dependencies/`
    - But for runtime testing you need the actual XSkills mod installed
    - Some of us have never touched XSkills nor intend to. "Unload Project" is a nifty functionality in some IDEs for that matter.

### Local Configuration

On Windows with default installation directories, it should all work out of the box:

- The mods are compiled straight into your /Mods folder
- The installation directory is the value of VINTAGE_STORY
- The data directory is the one under AppData/Roaming.

#### Non-default overrides

If you are, however, on a different OS or make use of game profiles, launchers or simply have your _particular
preferences_, there are Build Properties you can override.

Copy the local build-property template:

```powershell
Copy-Item .\dev\Local.Build.props.template .\dev\Local.Build.props
```

Then adjust these properties in `dev/Local.Build.props`:

- `VintageStoryPath`: the directory containing the game executables and API DLLs.
- `VSDataPath`: the Vintage Story data directory used for launching the Server and Client configs from this solution.
- `ModDeployRoot`: the directory where built mod folders are deployed as unpackaged folders. The game reads those just
  fine, they don't need to be ZIPs!

`dev/Local.Build.props` is local-only and ignored by Git, so no one will see yours.

#### AI instructions

Coding agents should begin with `AGENTS.md`, which directs them to the ordered
instructions under `dev/agents/`.

## Building

Build the complete solution from the repository root:

```powershell
dotnet build .\Arts.sln
```

Each mod project writes an unpacked mod directory beneath `ModDeployRoot`. Vintage
Story recognizes unpacked directories as mods, so packaging is not required for the
normal edit, build, launch, and test cycle.

## Packaging

Packaging is intended for release preparation. It validates asset JSON, rebuilds the
selected projects, and creates distributable ZIP archives in `Releases/`.

Using CakeBuild is not a necessity, it is there for the sole reason that the official mod template ships with it. We have modified the original to work on multiple projects at once but you can achieve the same results with simple shell scripts or... _shudders_ clicking around your files and folders in the UI?  

Run the centralized Cake project from its directory to package everything at once:

```powershell
# Note that the IDE should take care of this for you; 
# we have configs for Visual Studio and Rider
# but sadly, VSCode doesn't play as nice with Build Properties
Set-Location .\CakeBuild
dotnet run --project .\CakeBuild.csproj -- --vs="<game-path>" --in="<deployed-mods-path>"
```

Package selected projects with a comma-separated filter:

```powershell
# Same comment as above
dotnet run --project .\CakeBuild.csproj -- `
  --vs="<game-path>" `
  --in="<deployed-mods-path>" `
  --projects=CoreOfArts,ArtOfGrowing
```

Release packaging is expected to move into an automation pipeline eventually. It is
kept as an explicit local workflow for now.

## Development Files

The `dev/` directory contains development support files rather than mod content:

- `dev/Local.Build.props.template`: committed template for machine-local build paths.
- `dev/Local.Build.props`: ignored local overrides created from the template.
- `dev/dependencies/`: development-time third-party assemblies, currently xLib and XSkills.
- `dev/agents/`: ordered detailed instructions for coding agents.
- `dev/scripts/`: development and maintenance utilities.

Each mod directory is otherwise self-contained: its project file, `modinfo.json`,
source files where applicable, and assets live together at the same level.

## Credits and Third-Party Software

- The Arts mods were created by Fedarmens.
- Original upstream source:
  [Fedarmens/Arts](https://github.com/Fedarmens/Arts).
- Built for [Vintage Story](https://www.vintagestory.at/).
- Uses Harmony for runtime patching.
- AXS integrates with XSkills and xLib.
- Release tooling uses Cake Frosting and related .NET packages.
