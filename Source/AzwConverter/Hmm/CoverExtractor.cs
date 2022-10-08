using System.Net;
using System.Text;

public class CoverExtractor
{
    private readonly Stream _stream;
    private readonly BinaryReader _reader;

    private CoverExtractor(Stream mobiFile)
    {
        _stream = mobiFile;
        _reader = new BinaryReader(mobiFile);
    }

    public static Stream ExtractCover(Stream mobiFile)
    {
        if (!BitConverter.IsLittleEndian)
            throw new InvalidOperationException("You should rewrite this code if you intend to use it on a big-endian machine");

        return new CoverExtractor(mobiFile).Extract();
    }

    /// <summary>
    /// Extract the cover image from a MOBI stram
    /// </summary>
    /// <returns>null if there's no cover image</returns>
    private Stream Extract()
    {
        // A MOBI file is a Palm Database file containing MOBI-specific records.
        // A palm database file is a header, followed by a record list, followed by record data.

        // The first record (at index 0) contains the following concatenated data:
        //   - The PalmDOC header
        //   - The MOBI header
        //   - An optional EXTH header

        // Read the offset of the first record in the file
        var firstRecordOffset = ReadPdbRecordOffset(0);

        // Ignore the PalmDOC header, skip to the MOBI header at offset 16
        // and make sure it starts with the ASCII text "MOBI". If it does, we have a valid MOBI file.
        var mobiHeaderOffset = firstRecordOffset + 16;
        EnsureMagic(mobiHeaderOffset, "MOBI");

        // Read a couple useful values from the MOBI header
        var mobiHeaderLength = ReadUInt32(firstRecordOffset + 20);
        var firstImageRecordIndex = ReadUInt32(firstRecordOffset + 108);
        var exthFlags = ReadUInt32(firstRecordOffset + 128);

        // The "EXTH flags" bitfield indicates if there's an EXTH header.
        // If there's no EXTH header we won't be able to tell which image is the cover.
        if ((exthFlags & 0x40) == 0)
            return null; // There's no EXTH header

        // The EXTH header immediately follows the EXTH header, but as the MOBI header is of
        // variable length, we have to calculate the EXTH header offset.
        var exthOffset = mobiHeaderOffset + mobiHeaderLength;

        // Ensure the EXTH header starts with the ASCII string "EXTH" (just a validity check like before)
        EnsureMagic(exthOffset, "EXTH");

        // Read the "coveroffset" field value from the EXTH header
        var coverRecordOffset = ReadExthRecord(exthOffset, 201);

        // If the coveroffset field is not found, we assume there's no cover
        if (coverRecordOffset == null)
            return null; // No cover

        // Now, per the spec, the cover image is contained in a Palm Database record.
        // Image records are sequential in the MOBI file. These records start at index firstImageRecordIndex.
        // The cover image is one of these images. Its offset in the image records is coverRecordOffset.
        // With this info, we can deduce which record contains the cover image.
        var coverRecordIndex = firstImageRecordIndex + coverRecordOffset.Value;
        Console.WriteLine(coverRecordIndex);

        // Get the byte offset of the cover image
        var coverOffset = ReadPdbRecordOffset(coverRecordIndex);

        // Let's assume here the Palm Database records are indexed sequentially,
        // and are laid out sequentially as well, without any gap in between.
        // The PDB spec is not really explicit about that but it seems like a valid assumption.
        // Also, the MOBI spec says there's and end-of-file record, so the cover record won't be the last one.
        // Therefore, we can get the offset of the following record to deduce the image file size.
        var nextRecord = ReadPdbRecordOffset(coverRecordIndex + 1);

        Console.WriteLine(coverOffset);
        Console.WriteLine(nextRecord);

        return default;


        // Now we know the start and end offsets of the cover image in the MOBI file, extract the data.
        _stream.Position = coverOffset;
        return new MemoryStream(_reader.ReadBytes((int)(nextRecord - coverOffset)));
    }

    /// <summary>
    /// Gets the byte offset in the file of the PDB record at the given index.
    /// </summary>
    /// <param name="recordIndex">The record index</param>
    private long ReadPdbRecordOffset(uint recordIndex)
    {
        // A Palm Database file is a header followed by a record info list.
        // The header is 78 bytes, and each record info is 8 bytes.
        return ReadUInt32(78 + 8 * recordIndex);
    }

    /// <summary>
    /// Reads the given EXTH record value
    /// </summary>
    /// <param name="exthHeaderOffset">Offset of the EXTH header</param>
    /// <param name="exthRecordType">Type of the record to read</param>
    private uint? ReadExthRecord(long exthHeaderOffset,  int exthRecordType)
    {
        // The EXTH header contains records in no fixed order.
        // Each record has an type, a length and a value.

        // Read the total EXTH record count
        var exthRecordCount = ReadUInt32(exthHeaderOffset + 8);

        // Iterate over all the EXTH records until we find the one we're looking for
        for (var i = 0; i < exthRecordCount; ++i)
        {
            var recordType = ReadUInt32();

            // EXTH records have no fixed length, and the length provided in the record is for the whole record,
            // so we have to substract 8 bytes (the length of the "type" and "length" fields) in order to get the value length.
            var recordLength = ReadUInt32() - 8;

            // Check if we found the value record type we were looking for, and read its value
            if (recordType == exthRecordType)
                return ReadVariableLength(recordLength);

            // Throw the field value away and check the next field.
            // This is equivalent to _stream.Position += recordLength;
            _reader.ReadBytes((int)recordLength);
        }

        // We didn't find the field we were looking for
        return null;
    }

    /// <summary>
    /// Reads a byte, word or dword at the current position
    /// </summary>
    /// <param name="bytes">Number of bytes to read</param>
    private uint ReadVariableLength(uint bytes)
    {
        // EXTH record values do not have a fixed length, so just read whatever length is provided.

        switch (bytes)
        {
            case 1:
                return _reader.ReadByte();

            case 2:
                return ReadUInt16();

            case 4:
                return ReadUInt32();

            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Reads a dword at the given offset
    /// </summary>
    /// <param name="offset">Byte offset to read</param>
    private uint ReadUInt32(long offset)
    {
        _stream.Position = offset;
        return ReadUInt32();
    }

    /// <summary>
    /// Reads a word at the current offset
    /// </summary>
    private ushort ReadUInt16()
    {
        // Palm Database files are encoded in big-endian order, but BinaryReader reads everything in little-endian.
        return (ushort)IPAddress.HostToNetworkOrder(_reader.ReadInt16());
    }

    /// <summary>
    /// Reads a dword at the current offset
    /// </summary>
    private uint ReadUInt32()
    {
        return (uint)IPAddress.HostToNetworkOrder(_reader.ReadInt32());
    }

    /// <summary>
    /// Checks for a magic identifier at the given offset.
    /// </summary>
    /// <param name="offset">Byte offset to check</param>
    /// <param name="magicString">Expected ASCII string at the given offset</param>
    private void EnsureMagic(long offset, string magicString)
    {
        // This function is made like that just for the sake of readability.
        // In real code, you should just read an uint32 and compare that to the expected number.

        _stream.Position = offset;
        if (Encoding.ASCII.GetString(_reader.ReadBytes(magicString.Length)) != magicString)
            throw new InvalidOperationException("Invalid file format");
    }
}
