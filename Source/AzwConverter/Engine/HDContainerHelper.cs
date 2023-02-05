using MobiMetadata;
using System.Text;

namespace AzwConverter.Engine
{
    public class HDContainerHelper
    {
        public static FileInfo? FindHDContainer(MobiMetadata.MobiMetadata metadata, List<Azw6Head>? hdHeaderList)
        {
            var title = metadata!.MobiHeader.FullName;

            var list = hdHeaderList!.Where(header => header.Title == title).ToList();

            if (list.Count > 1)
            {
                var errorMsg = 
                    $"Found {list.Count} HD containers with the same title [{title}]:{Environment.NewLine}";

                var sb = new StringBuilder(errorMsg);
                foreach (var header in list)
                {
                    sb.AppendLine(header.Path!.FullName);
                }

                throw new ArgumentException(sb.ToString());
            }

            return list.Count == 1 ? list[0].Path : null;
        }
    }
}
