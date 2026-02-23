# Game Browser & Fork-to-Mod Feature — Implementation Plan

## Overview

Add a "Browse Games" feature to the launcher that lets users browse published cloud game packages, select one, and fork it into a local editable project. The forked project extracts all code from the game's `.cll` archives into real `.cs`/`.razor` files, creates a new `.sbproj`, and mounts the original game's assets as read-only. The result is a fully functional editor project.

## Architecture

```
[Launcher: StartupWindow]
    ├── Sidebar (existing)
    └── Body
        ├── HomeWidget (existing — local projects)
        └── GameBrowserWidget (NEW — cloud game catalog)
            ├── Search bar + filters
            ├── Game grid (thumbnails, titles, orgs)
            └── "Fork" button → GameForkDialog
                                    ├── Project name/ident/location inputs
                                    └── "Create" → GameForker
                                                    ├── Download package
                                                    ├── Extract CLLs → Code/
                                                    ├── Generate .sbproj
                                                    └── Open project
```

## Detailed Steps

### Step 1: Add Tab Navigation to StartupWindow

**File:** `engine/Launcher/StandaloneTest/Widgets/StartupWindow.cs`

Currently the Body section just adds `HomeWidget` directly (line 103-104). Change this to a simple tab system with two tabs: "My Projects" (HomeWidget) and "Browse Games" (GameBrowserWidget).

- Add two `SidebarButton`-style tab buttons in the sidebar: "My Projects" and "Browse Games"
- Swap the Body content between HomeWidget and GameBrowserWidget when tabs are clicked
- Default to "My Projects" tab on launch

### Step 2: Create GameBrowserWidget

**New file:** `engine/Launcher/StandaloneTest/Widgets/GameBrowserWidget.cs`

A widget that displays cloud game packages in a browsable grid. Modeled after HomeWidget's layout.

**Layout:**
- Top row: search `LineEdit` + sort dropdown
- Scroll area with `GridLayout` of `GameBrowserRow` items

**Data loading:**
- On construction and search, call `Package.FindAsync("type:game sort:trending {searchText}", 100, 0, token)`
- Display results as a grid of cards showing: `Package.Thumb`, `Package.Title`, `Package.Org.Title`, `Package.Summary`
- Paginate/load more on scroll if needed

**Selection:**
- Clicking a game card opens `GameForkDialog` with the selected `Package`

**Key APIs used:**
```csharp
Package.FindAsync(query, take, skip, token) → FindResult { Packages[] }
Package.Thumb, Package.Title, Package.Org.Title, Package.Summary
```

### Step 3: Create GameBrowserRow Widget

**New file (or inner class in GameBrowserWidget):** Game card widget for the browser grid.

- Similar to `ProjectRow` but for cloud packages
- Shows: thumbnail (from `Package.Thumb` URL), title, organization, summary
- Paint method renders thumbnail on left, text on right (same pattern as `PackageSelector.PaintAddonItem`)
- On click: invokes callback with the `Package`

### Step 4: Create GameForkDialog

**New file:** `engine/Launcher/StandaloneTest/Widgets/GameForkDialog.cs`

A dialog (inherits `Dialog`) similar to `ProjectCreator` but specifically for forking a game.

**Layout (800x500):**
- Left pane (30%): Game info preview — thumbnail, title, org, description
- Right pane (70%): Project setup form
  - "Title" — `LineEdit`, defaults to `"{GameTitle} - Fork"`
  - "Ident" — `LineEdit`, auto-derived from title, validated `[a-z0-9_]{2,32}`
  - "Location" — `FolderEdit`, defaults to `LauncherPreferences.DefaultProjectLocation`
  - `.gitignore` checkbox
  - Error display (if path already exists)
  - "Fork" button → triggers `GameForker.ForkAsync()`

**Callback:** `Action<string> OnProjectCreated` — returns the `.sbproj` path on success.

### Step 5: Create GameForker (Core Logic)

**New file:** `engine/Launcher/StandaloneTest/Widgets/GameForker.cs`

Static utility class that performs the actual fork operation.

```csharp
public static async Task<string> ForkAsync(Package sourceGame, string projectDir, string title, string ident)
```

**Process:**

1. **Create directory structure:**
   ```
   {projectDir}/
   ├── Code/
   ├── Editor/
   ├── Assets/
   └── .gitignore (optional)
   ```

2. **Download the game package:**
   ```csharp
   var ap = await PackageManager.InstallAsync(new PackageLoadOptions(sourceGame.FullIdent, "fork"));
   ```

3. **Find and extract .cll files:**
   ```csharp
   var cllFiles = ap.FileSystem.FindFile("/", "*.cll", true);
   foreach (var cllPath in cllFiles)
   {
       var bytes = await ap.FileSystem.ReadAllBytesAsync(cllPath);
       var archive = new CodeArchive(bytes);

       // Extract C# source files
       foreach (var syntaxTree in archive.SyntaxTrees)
       {
           var localPath = archive.FileMap.GetValueOrDefault(syntaxTree.FilePath, syntaxTree.FilePath);
           var sourceText = syntaxTree.GetText().ToString();
           var outputPath = Path.Combine(projectDir, "Code", localPath);
           Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
           File.WriteAllText(outputPath, sourceText);
       }

       // Extract Razor files
       foreach (var additional in archive.AdditionalFiles)
       {
           var outputPath = Path.Combine(projectDir, "Code", additional.LocalPath);
           Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
           File.WriteAllText(outputPath, additional.Text);
       }
   }
   ```

4. **Generate .sbproj:**
   ```csharp
   var config = new ProjectConfig
   {
       Title = title,
       Ident = ident,
       Org = "local",
       Type = "game",
       Schema = 1,
       PackageReferences = sourceGame.PackageReferences?.ToList() ?? new(),
   };
   config.SetMeta("ForkedFrom", sourceGame.FullIdent);

   // Copy relevant metadata from source (TickRate, MaxPlayers, etc.)
   // ...

   var configPath = Path.Combine(projectDir, $"{ident}.sbproj");
   File.WriteAllText(configPath, config.ToJson());
   ```

5. **Cleanup:**
   ```csharp
   PackageManager.UnmountTagged("fork");
   ```

6. **Return** the `.sbproj` path.

### Step 6: Modify StartupLoadProject for ForkedFrom Asset Mounting

**File:** `engine/Sandbox.Tools/StartupLoadProject.cs`

After the built-in projects are loaded (line 141), add a new block for "ForkedFrom" games — similar to the existing addon/ParentPackage block but only for asset mounting (since the code is already local):

```csharp
// After line 141 (built-in projects loaded), before line 146 (addon check):

var forkedFrom = project.Config.GetMetaOrDefault<string>("ForkedFrom", null);
if (project.Config.Type == "game" && !string.IsNullOrWhiteSpace(forkedFrom))
{
    Step($"Loading source game assets ({forkedFrom})");
    using (var _ = Bootstrap.StartupTiming?.ScopeTimer($"Load Project: ForkedFrom"))
    {
        // Install the source package (downloads assets, compiles dependencies)
        await PackageManager.InstallAsync(new PackageLoadOptions(forkedFrom, "tools"));

        // Install assets into asset system so they're browseable in editor
        await AssetSystem.InstallAsync(forkedFrom, false);
    }
}
```

This makes the original game's published assets available in the editor as read-only cloud assets (mounted in `.sbox/cloud/`), while the local project's Code/ directory has the extracted editable source.

The existing flow then continues normally: solution generation, compilation, game loading — all using the local extracted code.

### Step 7: Wire Up the Full Flow in HomeWidget

**File:** `engine/Launcher/StandaloneTest/Widgets/HomeWidget.cs`

No changes needed to HomeWidget itself. The flow is:

1. User clicks "Browse Games" tab in sidebar → `StartupWindow` swaps to `GameBrowserWidget`
2. User selects a game → `GameForkDialog` opens
3. User configures project → `GameForker.ForkAsync()` runs
4. Fork dialog calls `OnProjectCreated` callback
5. `StartupWindow` switches back to "My Projects", adds project to `ProjectList`, calls `OpenProject()`

## Files Summary

| Action | File |
|--------|------|
| **Modify** | `engine/Launcher/StandaloneTest/Widgets/StartupWindow.cs` — Add tab navigation |
| **Create** | `engine/Launcher/StandaloneTest/Widgets/GameBrowserWidget.cs` — Cloud game browser |
| **Create** | `engine/Launcher/StandaloneTest/Widgets/GameForkDialog.cs` — Fork configuration dialog |
| **Create** | `engine/Launcher/StandaloneTest/Widgets/GameForker.cs` — Fork execution logic |
| **Modify** | `engine/Sandbox.Tools/StartupLoadProject.cs` — ForkedFrom asset mounting |

## Key Decisions

1. **Type "game" not "addon"** — The forked project is a full game with local code, not an addon referencing a parent. This avoids the parent's CLLs being compiled again (which would cause duplicate type errors).

2. **Assets via ForkedFrom metadata** — Rather than copying potentially huge asset files, we reference the source game's ident in metadata. At startup, `StartupLoadProject` installs those assets from the cloud package. They appear as read-only cloud assets in the editor.

3. **PackageReferences copied** — The forked project inherits the source game's library dependencies so compilation succeeds.

4. **Tab system in launcher** — Adding tabs to the sidebar keeps the launcher clean while making game browsing a first-class feature alongside "My Projects".

5. **Code extraction preserves file structure** — `FileMap` in the CodeArchive maps syntax tree paths to project-local paths, so the extracted code directory mirrors the original project's structure.
