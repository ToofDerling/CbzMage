using CbzMage.Shared.Extensions;
using CbzMage.Shared.IO;
using System.Xml;

namespace EpubConverter
{
    public class OpfReader
    {
        public List<string> PageList { get; private set; }
        public List<string> ImageList { get; private set; }

        private static XmlReaderSettings XmlReaderSettings => new()
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            Async = true,
        };

        public async Task ReadOpfAsync(string opfFile)
        {
            using var textReader = new StreamReader(AsyncStreams.AsyncFileReadStream(opfFile));
            var xmlStr = await textReader.ReadToEndAsync();

            var strReader = new StringReader(xmlStr);
            using var xmlReader = XmlReader.Create(strReader, XmlReaderSettings);

            var isManifest = false;
            var isSpine = false;

            var pageIdXhtmlMap = new Dictionary<string, string>();
            var pageList = new List<string>();

            var imageList = new List<string>();

            while (await xmlReader.ReadAsync())
            {
                if (xmlReader.Name.EqualsIgnoreCase("manifest"))
                {
                    isManifest = xmlReader.NodeType != XmlNodeType.EndElement;
                }
                else if (xmlReader.Name.EqualsIgnoreCase("spine"))
                {
                    isSpine = xmlReader.NodeType != XmlNodeType.EndElement;
                }
                else if (isManifest)
                {
                    var mediaType = xmlReader.GetAttribute("media-type");
                    if (mediaType != null)
                    {
                        var href = xmlReader.GetAttribute("href");

                        if (mediaType.StartsWithIgnoreCase("image/"))
                        {
                            if (href != null)
                            {
                                imageList.Add(href);
                            }
                        }
                        else if (mediaType.EqualsIgnoreCase("application/xhtml+xml"))
                        {
                            var id = xmlReader.GetAttribute("id");

                            if (href != null && id != null)
                            {
                                pageIdXhtmlMap.Add(id, href);
                            }
                        }
                    }
                }
                else if (isSpine)
                {
                    var pageId = xmlReader.GetAttribute("idref");
                    var xhtmlHref = pageIdXhtmlMap[pageId!];    // throws if null

                    pageList.Add(xhtmlHref);
                }
            }

            PageList = pageList;
            ImageList = imageList;
        }
    }
}
