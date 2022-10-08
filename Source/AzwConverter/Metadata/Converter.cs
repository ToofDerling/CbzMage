using System.Buffers.Binary;

namespace AzwConverter.Metadata
{
    public static class Converter
    {
        public static short ToInt16(byte[] bytes)
        {
            var res = BitConverter.ToInt16(bytes);
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        //public static int ToInt32(byte[] bytes)
        //{
        //    return BitConverter.ToInt32(CheckBytes(bytes), 0);
        //}

        //public static long ToInt64(byte[] bytes)
        //{
        //    return BitConverter.ToInt64(CheckBytes(bytes), 0);
        //}

        public static ushort ToUInt16(byte[] bytes)
        {
            var res = BitConverter.ToUInt16(bytes);
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public static uint ToUInt32(byte[] bytes)
        {
            var res = BitConverter.ToUInt32(bytes);
            return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        //public static ulong ToUInt64(byte[] bytes)
        //{
        //    return BitConverter.ToUInt64(CheckBytes(bytes), 0);
        //}
    }
}
