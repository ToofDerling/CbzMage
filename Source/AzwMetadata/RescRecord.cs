using System.Text;
using System.Xml;

namespace AzwMetadata
{
    public class RescRecord : PageRecord
    {
        private static XmlReaderSettings XmlReaderSettings => new()
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            ConformanceLevel = ConformanceLevel.Fragment,
        };

        private static XmlWriterSettings XmlWriterSettings => new()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            Indent = true,
            //NewLineOnAttributes = true,
        };

        public string RawXml { get; private set; }

        public int PageCount { get; private set; }

        public RescRecord(Stream stream, long pos, uint len) : base(stream, pos, len)
        { }

        public string GetPrettyPrintXml()
        {
            var sb = new StringBuilder();

            using (var xmlReader = XmlReader.Create(new StringReader(RawXml), XmlReaderSettings))
            using (var xmlWriter = XmlWriter.Create(sb, XmlWriterSettings))
            {
                xmlWriter.WriteNode(xmlReader, false);

            }
            return sb.ToString();
        }

        public void ParseXml()
        {
            var data = ReadData();

            var xmlDataStr = Encoding.UTF8.GetString(data);
            xmlDataStr = xmlDataStr.Replace("\0", null).Trim();

            var xmlBegin = xmlDataStr.IndexOf("<");
            var xmlEnd = xmlDataStr.LastIndexOf(">");

            var xmlStr = xmlDataStr.Substring(xmlBegin, xmlEnd - xmlBegin + 1);
            int pageCount = 0;

            var strReader = new StringReader(xmlStr);
            using (XmlReader xmlReader = XmlReader.Create(strReader, XmlReaderSettings))
            {
                xmlReader.MoveToContent();
                while (xmlReader.Read())
                {
                    if (xmlReader.Name == "itemref")
                    {
                        var idRef = xmlReader.GetAttribute("idref");
                        var skelId = xmlReader.GetAttribute("skelid");

                        if (idRef != null && skelId != null)
                        {
                            pageCount++;
                        }
                    }
                }
            }

            RawXml = xmlStr;
            PageCount = pageCount;
        }

        public override Span<byte> ReadData()
        {
            return _reader.ReadBytes(_len - 4);
        }
    }
}
