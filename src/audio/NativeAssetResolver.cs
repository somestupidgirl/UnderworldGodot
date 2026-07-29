using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Underworld;

internal static class NativeAssetResolver
{
    public static string? ResolveLibraryPath(string libraryName, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(libraryName))
            throw new ArgumentException("Library name must not be empty.", nameof(libraryName));
        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory must not be empty.", nameof(baseDirectory));

        string fileName = GetNativeLibraryFileName(libraryName);
        foreach (string baseRoot in GetCandidateBaseDirectories(baseDirectory))
        {
            foreach (string runtimeId in GetCandidateRuntimeIds())
            {
                string fullPath = Path.Combine(baseRoot, "runtimes", runtimeId, "native", fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
        }

        return null;
    }

    private static string GetNativeLibraryFileName(string libraryName)
    {
        if (Path.HasExtension(libraryName))
            return libraryName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return libraryName + ".dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return libraryName + ".dylib";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return libraryName + ".so";

        throw new PlatformNotSupportedException();
    }

    private static IEnumerable<string> GetCandidateBaseDirectories(string baseDirectory)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return;

            string fullPath = Path.GetFullPath(candidate);
            if (seen.Add(fullPath))
                candidates.Add(fullPath);
        }

        AddCandidate(baseDirectory);
        AddCandidate(Path.Combine(baseDirectory, "..", "Resources"));
        AddCandidate(Path.Combine(baseDirectory, "..", "..", "Resources"));
        AddCandidate(Path.Combine(baseDirectory, "..", "..", "..", "Resources"));

        return candidates;
    }

    private static IEnumerable<string> GetCandidateRuntimeIds()
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? runtimeId)
        {
            if (!string.IsNullOrWhiteSpace(runtimeId) && seen.Add(runtimeId))
                candidates.Add(runtimeId);
        }

        AddCandidate(RuntimeInformation.RuntimeIdentifier);

        string? os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "osx"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? "linux"
                    : null;

        string? arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => null,
        };

        if (!string.IsNullOrEmpty(os) && !string.IsNullOrEmpty(arch))
        {
            AddCandidate($"{os}-{arch}");

            if (os == "osx")
            {
                if (arch != "x64") AddCandidate("osx-x64");
                if (arch != "arm64") AddCandidate("osx-arm64");
            }
            else if (os == "linux")
            {
                if (arch != "x64") AddCandidate("linux-x64");
                if (arch != "arm64") AddCandidate("linux-arm64");
            }
            else if (os == "win")
            {
                if (arch != "x64") AddCandidate("win-x64");
                if (arch != "x86") AddCandidate("win-x86");
                if (arch != "arm64") AddCandidate("win-arm64");
            }
        }

        return candidates;
    }
}
