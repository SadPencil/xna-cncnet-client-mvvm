using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ClientCore;
using Rampastring.Tools;

namespace IniLayoutDiag;

/// <summary>
/// Standalone diagnostic tool that tests the INI layout overlay service logic
/// without needing Avalonia or the full DXMainClientView. Replicates the
/// FindIniFile and FindTextureFile logic from IniLayoutOverlayService.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        // ---------------------------------------------------------------
        // 1. Set working directory the same way DXMainClientView does
        // ---------------------------------------------------------------
        string gameRoot = SetWorkingDirectoryToGameRoot();

        // ---------------------------------------------------------------
        // 2. Print the current working directory
        // ---------------------------------------------------------------
        Console.WriteLine($"Current working directory: {Environment.CurrentDirectory}");
        Console.WriteLine($"Game root:                 {gameRoot}");
        Console.WriteLine();

        // ---------------------------------------------------------------
        // 3. Call ProgramConstants.GetResourcePath() / GetBaseResourcePath()
        //    Also compute manually from gameRoot for comparison.
        // ---------------------------------------------------------------
        string manualResourcePath = Path.Combine(gameRoot, "Resources");
        string manualBaseResourcePath = manualResourcePath; // same when RESOURCES_DIR == BASE_RESOURCE_PATH

        string ppResourcePath = null;
        string ppBaseResourcePath = null;
        bool programConstantsOk = false;

        try
        {
            ppResourcePath = ProgramConstants.GetResourcePath();
            ppBaseResourcePath = ProgramConstants.GetBaseResourcePath();
            programConstantsOk = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProgramConstants threw: {ex.Message}");
        }

        Console.WriteLine("--- ProgramConstants ---");
        if (programConstantsOk)
        {
            Console.WriteLine($"  GetResourcePath():     {ppResourcePath}");
            Console.WriteLine($"  GetBaseResourcePath(): {ppBaseResourcePath}");
        }
        else
        {
            Console.WriteLine("  (not available - using manual paths)");
        }

        Console.WriteLine("--- Manual (from gameRoot) ---");
        Console.WriteLine($"  Resource path:     {manualResourcePath}");
        Console.WriteLine($"  Base resource path: {manualBaseResourcePath}");
        Console.WriteLine();

        // Use ProgramConstants paths if available, otherwise manual
        string resourcePath = ppResourcePath ?? manualResourcePath;
        string basePath = ppBaseResourcePath ?? manualBaseResourcePath;

        // ---------------------------------------------------------------
        // 3b. Test with RESOURCES_DIR set (simulating our fix)
        // ---------------------------------------------------------------
        Console.WriteLine("=== Testing RESOURCES_DIR initialization ===");
        string themePath = ReadThemePathFromIni(gameRoot);
        if (!string.IsNullOrEmpty(themePath))
        {
            string themedResourcePath = Path.Combine(gameRoot, "Resources", themePath);
            Console.WriteLine($"  Theme path from INI: {themePath}");
            Console.WriteLine($"  Themed resource path: {themedResourcePath}");
            Console.WriteLine($"  Directory exists: {Directory.Exists(themedResourcePath)}");

            if (Directory.Exists(themedResourcePath))
            {
                Console.WriteLine("  SWITCHING to themed resource path for remaining tests.");
                resourcePath = themedResourcePath;
            }
            else
            {
                Console.WriteLine("  Theme directory NOT found, keeping base resource path.");
            }
        }
        else
        {
            Console.WriteLine("  No theme path found in INI files.");
        }
        Console.WriteLine();

        // ---------------------------------------------------------------
        // 4. Try to find LoadingScreen.ini using FindIniFile logic
        // ---------------------------------------------------------------
        Console.WriteLine("=== INI File Search (LoadingScreen.ini) ===");
        string iniPath = FindIniFile("LoadingScreen", resourcePath, basePath);
        if (iniPath == null)
        {
            Console.WriteLine("  NOT FOUND in any search location.");
            Console.WriteLine("  Searched:");
            Console.WriteLine($"    {Path.Combine(resourcePath, "LoadingScreen.ini")}");
            Console.WriteLine($"    {Path.Combine(basePath, "LoadingScreen.ini")}");
            Console.WriteLine($"    {Path.Combine(resourcePath, "GenericWindow.ini")}");
            Console.WriteLine($"    {Path.Combine(basePath, "GenericWindow.ini")}");
            Console.WriteLine();
            Console.WriteLine("=== Done (no INI found) ===");
            return;
        }

        Console.WriteLine($"  FOUND: {iniPath}");
        Console.WriteLine();

        // ---------------------------------------------------------------
        // 5. Load with CCIniFile and print sections / keys
        // ---------------------------------------------------------------
        Console.WriteLine("=== Loading INI with CCIniFile ===");
        CCIniFile iniFile;
        try
        {
            iniFile = new CCIniFile(iniPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Failed to load: {ex.Message}");
            return;
        }

        // All sections
        var sections = iniFile.GetSections();
        Console.WriteLine($"  Total sections: {sections.Count}");
        Console.WriteLine("  Sections:");
        foreach (string section in sections)
        {
            Console.WriteLine($"    [{section}]");
        }
        Console.WriteLine();

        // [LoadingScreen] keys
        Console.WriteLine("  --- [LoadingScreen] keys ---");
        var loadingSection = iniFile.GetSection("LoadingScreen");
        if (loadingSection != null)
        {
            foreach (var kvp in loadingSection.Keys)
                Console.WriteLine($"    {kvp.Key}={kvp.Value}");
        }
        else
        {
            Console.WriteLine("    (section not found)");
        }
        Console.WriteLine();

        // [ExtraControls] keys
        Console.WriteLine("  --- [ExtraControls] keys ---");
        var extraSection = iniFile.GetSection("ExtraControls");
        if (extraSection != null)
        {
            foreach (var kvp in extraSection.Keys)
                Console.WriteLine($"    {kvp.Key}={kvp.Value}");
        }
        else
        {
            Console.WriteLine("    (section not found)");
        }
        Console.WriteLine();

        // Also check [$ExtraControls] (new format)
        var extraSection2 = iniFile.GetSection("$ExtraControls");
        if (extraSection2 != null)
        {
            Console.WriteLine("  --- [$ExtraControls] keys ---");
            foreach (var kvp in extraSection2.Keys)
                Console.WriteLine($"    {kvp.Key}={kvp.Value}");
            Console.WriteLine();
        }

        // Check for [logo], [text], [legal] sections
        Console.WriteLine("  --- Sub-section existence ---");
        string[] checkSections = { "logo", "text", "legal", "splash" };
        foreach (string name in checkSections)
        {
            var sec = iniFile.GetSection(name);
            if (sec != null)
            {
                Console.WriteLine($"    [{name}]: EXISTS ({sec.Keys.Count} keys)");
                foreach (var kvp in sec.Keys)
                    Console.WriteLine($"      {kvp.Key}={kvp.Value}");
            }
            else
            {
                Console.WriteLine($"    [{name}]: NOT FOUND");
            }
        }
        Console.WriteLine();

        // ---------------------------------------------------------------
        // 6. Try to find texture files referenced in the INI
        // ---------------------------------------------------------------
        Console.WriteLine("=== Texture File Search ===");

        // Collect BackgroundTexture values from [LoadingScreen] and sub-sections
        string[] textureNames = { "loading.png", "yrlogo.png", "loading_text.png", "legal_text.png",
                                  "lsbg.png", "lslogo.png", "lstext.png", "ts_legal_text.png",
                                  "launcherupdater.png" };

        // Also collect any BackgroundTexture values actually present in the INI
        var allTextures = new System.Collections.Generic.HashSet<string>(textureNames, StringComparer.OrdinalIgnoreCase);
        foreach (string section in sections)
        {
            var sec = iniFile.GetSection(section);
            if (sec == null) continue;
            foreach (var kvp in sec.Keys)
            {
                if (kvp.Key == "BackgroundTexture" && !string.IsNullOrWhiteSpace(kvp.Value))
                    allTextures.Add(kvp.Value);
            }
        }

        foreach (string textureName in allTextures.OrderBy(t => t))
        {
            string found = FindTextureFile(textureName, resourcePath, basePath);
            if (found != null)
                Console.WriteLine($"  {textureName,-30} -> FOUND: {found}");
            else
                Console.WriteLine($"  {textureName,-30} -> NOT FOUND");
        }

        Console.WriteLine();
        Console.WriteLine("=== Done ===");
    }

    // ---------------------------------------------------------------
    // Replicates IniLayoutOverlayService.FindIniFile logic
    // ---------------------------------------------------------------
    static string FindIniFile(string windowName, string resourcePath, string basePath)
    {
        // 1. Theme-specific: {ResourcePath}/{windowName}.ini
        string themeSpecific = Path.Combine(resourcePath, $"{windowName}.ini");
        if (File.Exists(themeSpecific))
            return themeSpecific;

        // 2. Base path: {BaseResourcePath}/{windowName}.ini
        string baseSpecific = Path.Combine(basePath, $"{windowName}.ini");
        if (File.Exists(baseSpecific))
            return baseSpecific;

        // 3. Theme GenericWindow.ini
        string themeGeneric = Path.Combine(resourcePath, "GenericWindow.ini");
        if (File.Exists(themeGeneric))
            return themeGeneric;

        // 4. Base GenericWindow.ini
        string baseGeneric = Path.Combine(basePath, "GenericWindow.ini");
        if (File.Exists(baseGeneric))
            return baseGeneric;

        return null;
    }

    // ---------------------------------------------------------------
    // Replicates IniLayoutOverlayService.FindTextureFile logic
    // ---------------------------------------------------------------
    static string FindTextureFile(string texturePath, string resourcePath, string basePath)
    {
        // Search in resource paths (theme first, then base)
        string themePath = Path.Combine(resourcePath, texturePath);
        if (File.Exists(themePath))
            return themePath;

        string basePathFull = Path.Combine(basePath, texturePath);
        if (File.Exists(basePathFull))
            return basePathFull;

        // Try without subdirectory
        string themePathDirect = Path.Combine(resourcePath, Path.GetFileName(texturePath));
        if (File.Exists(themePathDirect))
            return themePathDirect;

        string basePathDirect = Path.Combine(basePath, Path.GetFileName(texturePath));
        if (File.Exists(basePathDirect))
            return basePathDirect;

        return null;
    }

    // ---------------------------------------------------------------
    // Replicates DXMainClientView SetWorkingDirectoryToGameRoot logic.
    // Walks up from BaseDirectory looking for Resources/ClientDefinitions.ini,
    // then falls back to DXMainClient/ subdirectory.
    // ---------------------------------------------------------------
    static string SetWorkingDirectoryToGameRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        // Primary: walk up looking for a directory containing Resources/ClientDefinitions.ini
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Resources", "ClientDefinitions.ini")))
            {
                Directory.SetCurrentDirectory(dir.FullName);
                Console.WriteLine($"Working directory set to: {dir.FullName}");
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        // Fallback: look for DXMainClient/ subdirectory from each ancestor
        dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string dxDir = Path.Combine(dir.FullName, "DXMainClient");
            if (Directory.Exists(dxDir) &&
                File.Exists(Path.Combine(dxDir, "Resources", "ClientDefinitions.ini")))
            {
                Directory.SetCurrentDirectory(dxDir);
                Console.WriteLine($"Working directory set to DXMainClient: {dxDir}");
                return dxDir;
            }

            dir = dir.Parent;
        }

        // Last resort: use assembly location to find game root
        string assemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrEmpty(assemblyPath))
        {
            string found = FindGameRoot(assemblyPath);
            if (found != null)
            {
                Directory.SetCurrentDirectory(found);
                Console.WriteLine($"Working directory set to (fallback): {found}");
                return found;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not find game root directory. " +
            "Run this tool from within the client repo tree " +
            "(somewhere under a directory containing DXMainClient/Resources/).");
    }

    // ---------------------------------------------------------------
    // Reads theme path from ClientDefinitions.ini and UserDefaults.ini
    // ---------------------------------------------------------------
    static string ReadThemePathFromIni(string gameRoot)
    {
        string clientDefsPath = Path.Combine(gameRoot, "Resources", "ClientDefinitions.ini");
        if (!File.Exists(clientDefsPath))
            return null;

        var clientDefs = new IniFile(clientDefsPath);
        var themesSection = clientDefs.GetSection("Themes");
        if (themesSection == null || themesSection.Keys.Count == 0)
            return null;

        // Get first theme as default
        string firstThemeEntry = themesSection.Keys[0].Value;
        string defaultThemeName = firstThemeEntry.Split(',')[0];
        string defaultThemePath = firstThemeEntry.Contains(',') ? firstThemeEntry.Split(',')[1] : string.Empty;

        // Check user preference
        string themeName = defaultThemeName;
        foreach (string userIniName in new[] { "UserDefaults.ini", "User.ini" })
        {
            string userIniPath = Path.Combine(gameRoot, userIniName);
            if (File.Exists(userIniPath))
            {
                var userIni = new IniFile(userIniPath);
                string savedTheme = userIni.GetStringValue("MultiPlayer", "Theme", null);
                if (!string.IsNullOrEmpty(savedTheme))
                {
                    themeName = savedTheme;
                    break;
                }
            }
        }

        // Find matching theme path
        foreach (var key in themesSection.Keys)
        {
            var parts = key.Value.Split(',');
            if (parts.Length >= 2 && parts[0] == themeName)
                return parts[1];
        }

        return defaultThemePath;
    }

    // ---------------------------------------------------------------
    // Searches upward from a path for a directory containing a
    // Resources/ subdirectory (the game root).
    // ---------------------------------------------------------------
    static string FindGameRoot(string startPath = null)
    {
        DirectoryInfo currentDir = new DirectoryInfo(
            startPath ?? AppDomain.CurrentDomain.BaseDirectory);

        for (int i = currentDir.FullName.Contains("Resources") ? 0 : 3; i < 6; i++)
        {
            if (currentDir == null)
                break;

            if (string.Equals(currentDir.Name, "Resources", StringComparison.OrdinalIgnoreCase))
                return currentDir.Parent?.FullName;

            DirectoryInfo resourcesDir = currentDir.GetDirectories("Resources", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            if (resourcesDir != null)
                return currentDir.FullName;

            currentDir = currentDir.Parent;
        }

        return null;
    }
}
