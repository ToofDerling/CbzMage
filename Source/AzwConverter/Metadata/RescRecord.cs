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
                var pageId = 1; // Start with 1 because we usually have a cover

                xmlReader.MoveToContent();
                while (xmlReader.Read())
                {
                    if (xmlReader.Name == "itemref")
                    {
                        var idRef = xmlReader.GetAttribute("idref");
                        // Can't use skelid to generate pagenumber because it isn't always sequential
                        var skelId = xmlReader.GetAttribute("skelid");
                        if (idRef != null && skelId != null)
                        {
                            // page-0001, page-0002...
                            var pageName = $"page-{(pageId).ToString().PadLeft(4, '0')}";
                            
                            // Normalize pagename in the xml
                            sb.Replace(idRef, pageName);

                            pages.Add(pageName);
                            pageId++;
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
