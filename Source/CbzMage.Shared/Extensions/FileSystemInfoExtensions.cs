using System.IO;

namespace CbzMage.Shared.Extensions
{
    public static class FileSystemInfoExtensions
    {
        public static bool IsDirectory(this FileSystemInfo e) => (e.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
    }
}
