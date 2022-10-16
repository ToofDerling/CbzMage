using PdfConverter.ManagedBuffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImagesCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly ZipArchive _compressor;

        private readonly IDictionary<string, Stream> _inputMap;

        //TODO: Write MagicImage data directly into the compressor

        public ImagesCompressorJob(ZipArchive compressor, IDictionary<string, Stream> inputMap)
        {
            _compressor = compressor;

            _inputMap = inputMap;
        }

        public IEnumerable<string> Execute()
        {
            foreach (var page in _inputMap)
            {
                var entry = _compressor.CreateEntry(page.Key, CompressionLevel.Fastest);
                using (var archiveStream = entry.Open())
                {
                    var dataStream = (ManagedMemoryStream)page.Value;
                    
                    // Doesn't work...
                    //var buffer = dataStream.GetBuffer();
                    //archiveStream.Write(buffer, 0, (int)dataStream.Length);
                
                    dataStream.CopyTo(archiveStream);   
                    dataStream.Release();
                }
            }

            return _inputMap.Keys;
        }
    }
}
