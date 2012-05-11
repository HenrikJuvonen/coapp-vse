namespace CoGet.VisualStudio
{
    public interface IFileSystemProvider
    {
        IFileSystem GetFileSystem(string path);
    }
}
