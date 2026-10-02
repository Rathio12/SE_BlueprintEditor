using SEBlueprint.Core.Limits;

namespace SEBlueprint.Core.Tests;

[Collection("Storage")]
public class StorageTests
{
    [Fact]
    public void Portable_root_next_to_exe_when_writable()
    {
        var exeDir = Directory.CreateTempSubdirectory().FullName;
        var root = Storage.ChoosePortableRoot(exeDir);
        Assert.Equal(Path.Combine(exeDir, Storage.PortableFolderName), root);
        Assert.True(Directory.Exists(root));
    }

    [Fact]
    public void Falls_back_when_exe_folder_is_not_usable()
    {
        var root = Storage.ChoosePortableRoot(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "does-not-exist", "\0bad"));
        Assert.Equal(Storage.DefaultRoot, root);
    }

    [Fact]
    public void Log_and_profiles_follow_the_root()
    {
        var old = Storage.Root;
        try
        {
            Storage.Root = Directory.CreateTempSubdirectory().FullName;
            Assert.Equal(Path.Combine(Storage.Root, "profiles"), ProfileStore.Dir);
            Assert.Equal(Storage.Root, Log.Dir);
            Log.Write("hello");
            Assert.True(File.Exists(Path.Combine(Storage.Root, "log.txt")));
        }
        finally { Storage.Root = old; }
    }
}
