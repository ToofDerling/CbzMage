namespace CbzMage.Shared.Helpers
{
    public class AsyncStreams
    {
        public static FileStream AsyncFileReadStream(string filePath)
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, 0, FileOptions.Asynchronous);
        }

        public static FileStream AsyncFileWriteStream(string filePath)
        {
            return new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite,
                    FileShare.ReadWrite, 0, FileOptions.Asynchronous);
        }
    }
}
