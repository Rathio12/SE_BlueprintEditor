namespace SEBlueprint.Core.Tests;

/// <summary>Builds a throwaway fake game "Content/Data" folder and Workshop folder from the fixtures.</summary>
internal static class TestData
{
    public const string ModId = "1919062467";
    public static string Fx(params string[] parts) => Path.Combine(new[] { AppContext.BaseDirectory, "Fixtures" }.Concat(parts).ToArray());

    public static (string Data, string Workshop) Fake()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var data = Path.Combine(root, "Data");
        Directory.CreateDirectory(Path.Combine(data, "CubeBlocks"));
        Directory.CreateDirectory(Path.Combine(data, "Blueprints"));
        Directory.CreateDirectory(Path.Combine(data, "Localization"));
        File.Copy(Fx("CubeBlocks_Test.sbc"), Path.Combine(data, "CubeBlocks", "CubeBlocks_Test.sbc"));
        File.Copy(Fx("Components_Test.sbc"), Path.Combine(data, "Components.sbc"));
        File.Copy(Fx("Blueprints_Test.sbc"), Path.Combine(data, "Blueprints", "Blueprints_Test.sbc"));
        File.Copy(Fx("MyTexts.resx"), Path.Combine(data, "Localization", "MyTexts.resx"));

        var ws = Path.Combine(root, "ws");
        var mod = Path.Combine(ws, ModId);
        Directory.CreateDirectory(Path.Combine(mod, "Data", "Scripts", "WeaponThread"));
        File.Copy(Fx("CubeBlocks_Test.sbc"), Path.Combine(mod, "Data", "Blocks.sbc"));
        File.Copy(Fx("WeaponCorePart.cs.txt"), Path.Combine(mod, "Data", "Scripts", "WeaponThread", "Part.cs"));
        File.WriteAllText(Path.Combine(mod, "Data", "Broken.sbc"), "<Definitions><oops");
        Directory.CreateDirectory(Path.Combine(ws, "555")); // a workshop item that is not a mod
        return (data, ws);
    }
}
