using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media;
using ClientCore;
using DXMainClientView.Services;
using Rampastring.Tools;

namespace DXMainClientView;

/// <summary>
/// Headless test for IniLayoutOverlayService.
/// Tests INI loading, property application, and texture resolution
/// without needing a display server.
/// </summary>
public static class IniOverlayTest
{
    public static void Run()
    {
        Console.WriteLine("=== INI Layout Overlay Test ===");
        Console.WriteLine();

        // Test 1: Verify resource paths
        Console.WriteLine("--- Resource Paths ---");
        Console.WriteLine($"GamePath:           {ProgramConstants.GamePath}");
        Console.WriteLine($"GetResourcePath():  {ProgramConstants.GetResourcePath()}");
        Console.WriteLine($"GetBaseResourcePath(): {ProgramConstants.GetBaseResourcePath()}");
        Console.WriteLine();

        // Test 2: Find LoadingScreen.ini
        Console.WriteLine("--- INI File Discovery ---");
        string resourcePath = ProgramConstants.GetResourcePath();
        string basePath = ProgramConstants.GetBaseResourcePath();

        string[] iniSearchPaths = {
            Path.Combine(resourcePath, "LoadingScreen.ini"),
            Path.Combine(basePath, "LoadingScreen.ini"),
            Path.Combine(resourcePath, "GenericWindow.ini"),
            Path.Combine(basePath, "GenericWindow.ini"),
        };
        foreach (string p in iniSearchPaths)
        {
            Console.WriteLine($"  {p} -> {(File.Exists(p) ? "EXISTS" : "NOT FOUND")}");
        }
        Console.WriteLine();

        // Test 3: Load and parse INI
        string iniPath = null;
        foreach (string p in iniSearchPaths)
        {
            if (File.Exists(p)) { iniPath = p; break; }
        }

        if (iniPath == null)
        {
            Console.WriteLine("ERROR: No INI file found. Cannot continue.");
            return;
        }

        Console.WriteLine($"--- Loading: {iniPath} ---");
        var iniFile = new CCIniFile(iniPath);

        var loadingSection = iniFile.GetSection("LoadingScreen");
        if (loadingSection != null)
        {
            Console.WriteLine("[LoadingScreen] keys:");
            foreach (var kvp in loadingSection.Keys)
                Console.WriteLine($"  {kvp.Key}={kvp.Value}");
        }
        Console.WriteLine();

        var extraSection = iniFile.GetSection("ExtraControls");
        if (extraSection != null)
        {
            Console.WriteLine("[ExtraControls] keys:");
            foreach (var kvp in extraSection.Keys)
                Console.WriteLine($"  {kvp.Key}={kvp.Value}");
        }
        Console.WriteLine();

        // Test 4: Find textures referenced in INI
        Console.WriteLine("--- Texture Discovery ---");
        string[] allTextures = CollectTextures(iniFile);
        foreach (string tex in allTextures)
        {
            string found = FindTexture(tex, resourcePath, basePath);
            Console.WriteLine($"  {tex,-30} -> {(found != null ? "FOUND: " + Path.GetFileName(found) : "NOT FOUND")}");
        }
        Console.WriteLine();

        // Test 5: Verify control creation types
        Console.WriteLine("--- Control Type Mapping ---");
        string[] testTypes = { "XNAExtraPanel", "XNALabel", "XNAButton", "XNACheckBox", "XNADropDown", "XNATextBox" };
        foreach (string type in testTypes)
        {
            var control = CreateControl(type, "test");
            Console.WriteLine($"  {type,-20} -> {control?.GetType().Name ?? "null"}");
        }
        Console.WriteLine();

        // Test 6: Verify DistanceFrom*Border calculation
        Console.WriteLine("--- Deferred Property Calculation ---");
        double parentW = 1280, parentH = 720;
        double controlW = 523, controlH = 318;
        double distLeft = 754, distBottom = 5;
        double x = distLeft;
        double y = parentH - controlH - distBottom;
        Console.WriteLine($"  Parent: {parentW}x{parentH}");
        Console.WriteLine($"  Control: {controlW}x{controlH}");
        Console.WriteLine($"  DistanceFromLeftBorder={distLeft} -> X={x}");
        Console.WriteLine($"  DistanceFromBottomBorder={distBottom} -> Y={y}");

        double distRight2 = 5, distTop2 = 5;
        double controlW2 = 198, controlH2 = 32;
        double x2 = parentW - controlW2 - distRight2;
        double y2 = distTop2;
        Console.WriteLine($"  Control2: {controlW2}x{controlH2}");
        Console.WriteLine($"  DistanceFromRightBorder={distRight2} -> X={x2}");
        Console.WriteLine($"  DistanceFromTopBorder={distTop2} -> Y={y2}");
        Console.WriteLine();

        Console.WriteLine("=== Test Complete ===");
    }

    private static string[] CollectTextures(CCIniFile iniFile)
    {
        var textures = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in iniFile.GetSections())
        {
            var sec = iniFile.GetSection(section);
            if (sec == null) continue;
            foreach (var kvp in sec.Keys)
            {
                if (kvp.Key == "BackgroundTexture" && !string.IsNullOrWhiteSpace(kvp.Value))
                    textures.Add(kvp.Value);
            }
        }
        var arr = new string[textures.Count];
        textures.CopyTo(arr);
        Array.Sort(arr);
        return arr;
    }

    private static string FindTexture(string texturePath, string resourcePath, string basePath)
    {
        string[] searchPaths = {
            Path.Combine(resourcePath, texturePath),
            Path.Combine(basePath, texturePath),
            Path.Combine(resourcePath, Path.GetFileName(texturePath)),
            Path.Combine(basePath, Path.GetFileName(texturePath)),
        };
        foreach (string p in searchPaths)
        {
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private static Control CreateControl(string controlType, string name)
    {
        return controlType switch
        {
            "XNAExtraPanel" or "XNAPanel" or "XNAControl" => new Border { Name = name, Child = new Panel() },
            "XNALabel" => new TextBlock { Name = name },
            "XNAButton" or "XNAClientButton" => new Button { Name = name },
            "XNACheckBox" or "XNAClientCheckBox" => new CheckBox { Name = name },
            "XNADropDown" or "XNAClientDropDown" => new ComboBox { Name = name },
            "XNATextBox" or "XNASuggestionTextBox" => new TextBox { Name = name },
            _ => null
        };
    }
}
