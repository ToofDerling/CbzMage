namespace CbzMage.Shared.Extensions
{
    public static class DirectoryExtensions
    {
        public static void CreateDirIfNotExists(this string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void DeleteAndCreateDir(this string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
        }
    }
}
