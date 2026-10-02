using System.Text.RegularExpressions;

namespace SEBlueprint.Core.Data;

/// <summary>
/// Finds WeaponCore / CoreSystems weapon blocks declared in mod scripts. Such weapons are usually
/// ConveyorSorter blocks in the .sbc files, so they can only be recognised from the script definitions.
/// The script is read as text only — it is never compiled or executed.
/// </summary>
public static class WeaponCoreScanner
{
    static readonly Regex MountPoint = new(@"new\s+MountPointDef\s*\{(.*?)\}", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex Subtype = new(@"SubtypeId\s*=\s*""([^""]+)""", RegexOptions.Compiled);
    static readonly Regex Azimuth = new(@"AzimuthPartId\s*=\s*""([^""]*)""", RegexOptions.Compiled);

    /// <summary>Map of block SubtypeId → true when it is a turret (has an azimuth part), false for fixed weapons.</summary>
    public static IReadOnlyDictionary<string, bool> Scan(string csharpSource)
    {
        var result = new Dictionary<string, bool>();
        foreach (Match m in MountPoint.Matches(csharpSource))
        {
            var body = m.Groups[1].Value;
            var sub = Subtype.Match(body);
            if (!sub.Success) continue;
            var az = Azimuth.Match(body);
            var isTurret = az.Success && az.Groups[1].Value.Length > 0 && az.Groups[1].Value != "None";
            result[sub.Groups[1].Value] = result.GetValueOrDefault(sub.Groups[1].Value) || isTurret;
        }
        return result;
    }
}
