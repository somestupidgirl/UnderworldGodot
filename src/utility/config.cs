using System.IO;
using System.Text.Json;
using System;
using Godot;
using System.Diagnostics;

namespace Underworld;

public class uwsettings
{

	private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        IgnoreReadOnlyProperties = true,
        PropertyNameCaseInsensitive = true,
    };

	private static readonly string FilePath
		= ProjectSettings.GlobalizePath("user://settings.json");

    public static uwsettings instance;

    // This initialises our instance as soon as the class is loaded.
    static uwsettings() => LoadSettings();

    public static void LoadSettings()
    {
        string defaultPathUW1 = DiscoverGameDataPath("UW1");
        string defaultPathUW2 = DiscoverGameDataPath("UW2");

        if (File.Exists(FilePath))
        {
            Debug.Print($"Loading settings from {FilePath}");
            using var stream = File.OpenRead(FilePath);
            instance = JsonSerializer.Deserialize<uwsettings>(stream, JsonOpts);
        }
        else
        {
            Debug.Print($"No existing settings at {FilePath}. Loading defaults.");
            instance = new();
        }

        instance.pathuw1 = IsUsableGamePath(instance.pathuw1)
            ? Path.GetFullPath(instance.pathuw1)
            : defaultPathUW1;
        instance.pathuw2 = IsUsableGamePath(instance.pathuw2)
            ? Path.GetFullPath(instance.pathuw2)
            : defaultPathUW2;

        if (string.IsNullOrWhiteSpace(instance.pathuw1))
            instance.pathuw1 = defaultPathUW1;
        if (string.IsNullOrWhiteSpace(instance.pathuw2))
            instance.pathuw2 = defaultPathUW2;

        if (main.cameraPitchGimbal_world != null)
        {
            main.cameraPitchGimbal_world.Fov = Math.Max(50, instance.FOV);
            main.cameraPitchGimbal_sprites.Fov = main.cameraPitchGimbal_world.Fov;
        }

        switch (instance.gametoload.ToUpper())
        {
            case "UW2":
            case "2":
                UWClass._RES = UWClass.GAME_UW2;
                UWClass.BasePath = instance.pathuw2;
                break;
            case "UW1":
            case "1":
                UWClass._RES = UWClass.GAME_UW1;
                UWClass.BasePath = instance.pathuw1;
                break;
            case "UWDEMO":
            case "0":
                UWClass._RES = UWClass.GAME_UWDEMO;
                break;
            default:
                throw new InvalidOperationException("Invalid Game Selected");
        }

        // Backward compat: if legacy 'rompath' is set but new 'synthpath' isn't,
        // promote rompath to synthpath.
        if (string.IsNullOrEmpty(instance.synthpath) && !string.IsNullOrEmpty(instance.rompath))
        {
            instance.synthpath = instance.rompath;
            Debug.Print("Warning: 'rompath' setting is deprecated, use 'synthpath' instead.");
        }

    }

    public string pathuw1 { get; set; } = string.Empty;
    public string pathuw2 { get; set; } = string.Empty;
    public string gametoload { get; set; } = "UW1";
    public int level { get; set; } = 0;
    public float FOV { get; set; } = 75;
    public bool showcolliders { get; set; }
    public int shaderbandsize { get; set; } = 8;
    public string synth { get; set; } = "soundfont";
    public string synthpath { get; set; } = "";
    // Legacy field, still read for backward compatibility. If set and synthpath is empty,
    // synthpath is populated from this in LoadSettings.
    public string rompath { get; set; } = "";

    private static bool IsUsableGamePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            string fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
                return false;

            return Directory.Exists(Path.Combine(fullPath, "DATA"))
                || Directory.Exists(Path.Combine(fullPath, "SOUND"))
                || Directory.Exists(Path.Combine(fullPath, "CRIT"));
        }
        catch (Exception ex)
        {
            Debug.Print($"Could not validate game path '{path}': {ex.Message}");
            return false;
        }
    }

    private static string DiscoverGameDataPath(string gameFolder)
    {
        string[] candidateRoots =
        [
            ProjectSettings.GlobalizePath("res://"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "Resources")),
        ];

        foreach (string root in candidateRoots)
        {
            if (TryResolveGameDataPath(root, gameFolder, out string resolvedPath))
                return resolvedPath;
        }

        foreach (string root in candidateRoots)
        {
            string? current = Path.GetFullPath(root);
            while (!string.IsNullOrEmpty(current))
            {
                if (TryResolveGameDataPath(current, gameFolder, out string resolvedPath))
                    return resolvedPath;

                string? parent = Directory.GetParent(current)?.FullName;
                if (string.IsNullOrEmpty(parent) || string.Equals(current, parent, StringComparison.OrdinalIgnoreCase))
                    break;
                current = parent;
            }
        }

        return string.Empty;
    }

    private static bool TryResolveGameDataPath(string root, string gameFolder, out string resolvedPath)
    {
        resolvedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(root))
            return false;

        string candidate = Path.Combine(root, "UWDATA", gameFolder);
        if (Directory.Exists(candidate))
        {
            resolvedPath = Path.GetFullPath(candidate);
            return true;
        }

        candidate = Path.Combine(root, gameFolder);
        if (Directory.Exists(candidate))
        {
            resolvedPath = Path.GetFullPath(candidate);
            return true;
        }

        return false;
    }

    public void Save()
    {
        Debug.Print($"Saving settings to {FilePath}");
        using var stream = File.OpenWrite(FilePath);
        JsonSerializer.Serialize(stream, this, JsonOpts);
    }

}
