using System.Text;
using System.Xml;

namespace AzwConverter.Metadata
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

        public List<string> Pages { get; private set; }

        public RescRecord(Stream stream, long pos, uint len) : base(stream, pos, len)
        { }

        public string GetPrettyPrintXml(string bookId)
        {
            var cleanId = bookId.Replace("_EBOK", null);
            var cleanStr = $"<dc:relation>{cleanId}</dc:relation>";

            var xml = RawXml.Replace(cleanStr, null);

            var sb = new StringBuilder();

            using (var xmlReader = XmlReader.Create(new StringReader(xml), XmlReaderSettings))
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
            var pages = new List<string>();

            var strReader = new StringReader(xmlStr);
            var sb = new StringBuilder(xmlStr);

            using (XmlReader xmlReader = XmlReader.Create(strReader, XmlReaderSettings))
            {
                var nextPageId = 0;

                xmlReader.MoveToContent();
                while (xmlReader.Read())
                {
                    if (xmlReader.Name == "itemref")
                    {
                        var pageRef = xmlReader.GetAttribute("idref");
                        var pageIdStr = xmlReader.GetAttribute("skelid");
                        if (pageRef != null && pageIdStr != null)
                        {
                            var pageId = int.Parse(pageIdStr);
                            if (pageId != nextPageId)
                            {
                                throw new Exception($"Expected skelId {nextPageId} got {pageId}");
                            }

                            var pageName = $"page-{(pageId + 1).ToString().PadLeft(4, '0')}";
                            sb.Replace(pageRef, pageName);

                            pages.Add(pageRef);
                            nextPageId++;
                        }
                    }
                }
            }

            RawXml = sb.ToString();
            Pages = pages;
        }

        public override Span<byte> ReadData()
        {
            return _reader.ReadBytes(_len - 4);
        }
    }
}
