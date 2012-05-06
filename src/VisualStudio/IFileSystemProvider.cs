namespace CoApp.VsExtension.VisualStudio
{
    public interface IFileSystemProvider
    {
        IFileSystem GetFileSystem(string path);
    }
}
