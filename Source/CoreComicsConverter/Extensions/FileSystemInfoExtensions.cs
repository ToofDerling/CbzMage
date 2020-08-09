using System.IO;

namespace CoreComicsConverter.Extensions
{
    public static class FileSystemInfoExtensions
    {
        public static bool IsDirectory(this FileSystemInfo e)
        {
            return (e.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }
    }
}
