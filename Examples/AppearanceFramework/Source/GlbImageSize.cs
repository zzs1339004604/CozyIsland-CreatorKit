using System;
using System.Buffers.Binary;
using System.IO;

namespace CozyMods.Appearance
{
    internal static class GlbImageSize
    {
        public static (int Width, int Height) Read(ReadOnlySpan<byte> bytes, string type)
        {
            if (type == "image/png" && bytes.Length >= 33 &&
                BinaryPrimitives.ReadUInt64BigEndian(bytes) == 0x89504e470d0a1a0a &&
                BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8)) == 13 &&
                BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(12)) == 0x49484452)
                return (BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(16)),
                    BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(20)));

            if (type == "image/jpeg" && bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8)
                return ReadJpeg(bytes);

            throw new InvalidDataException("GLB appearance: use embedded PNG or JPEG images.");
        }

        private static (int Width, int Height) ReadJpeg(ReadOnlySpan<byte> bytes)
        {
            int offset = 2;
            (int Width, int Height) size = default;
            while (offset < bytes.Length && bytes[offset++] == 0xff)
            {
                while (offset < bytes.Length && bytes[offset] == 0xff) offset++;
                if (offset >= bytes.Length - 2) break;
                int marker = bytes[offset++];
                if (marker == 0xda && size.Width > 0 && size.Height > 0) return size;

                int length = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset));
                if (length < 2 || length > bytes.Length - offset) break;
                if ((marker >= 0xc0 && marker <= 0xc3) || (marker >= 0xc5 && marker <= 0xc7) ||
                    (marker >= 0xc9 && marker <= 0xcb) || (marker >= 0xcd && marker <= 0xcf))
                {
                    if (size.Width != 0 || length < 8 || bytes[offset + 2] != 8) break;
                    size = (BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 5)),
                        BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 3)));
                }
                offset += length;
            }
            throw new InvalidDataException("GLB appearance: invalid JPEG frame header.");
        }
    }
}
