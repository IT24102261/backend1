using System.Buffers.Binary;
using System.IO.Compression;

namespace FixFlow.Infrastructure.Data;

internal static class PortraitPng
{
    public static byte[] ForName(string name)
    {
        var hash = (uint)StringComparer.OrdinalIgnoreCase.GetHashCode(name);
        var r = (byte)(70 + hash % 140);
        var g = (byte)(70 + (hash >> 8) % 140);
        var b = (byte)(70 + (hash >> 16) % 140);
        return RgbSquare(96, r, g, b);
    }

    public static byte[] RgbSquare(int size, byte r, byte g, byte b)
    {
        var raw = new byte[(size * 3 + 1) * size];
        var i = 0;
        for (var y = 0; y < size; y++)
        {
            raw[i++] = 0;
            for (var x = 0; x < size; x++)
            {
                raw[i++] = r;
                raw[i++] = g;
                raw[i++] = b;
            }
        }

        using var zlib = new MemoryStream();
        zlib.WriteByte(0x78);
        zlib.WriteByte(0x01);
        using (var deflate = new DeflateStream(zlib, CompressionLevel.NoCompression, leaveOpen: true))
        {
            deflate.Write(raw);
        }

        WriteUInt32Big(zlib, Adler32(raw));

        using var png = new MemoryStream();
        png.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        WriteChunk(png, "IHDR"u8, Ihdr(size, size));
        WriteChunk(png, "IDAT"u8, zlib.ToArray());
        WriteChunk(png, "IEND"u8, []);
        return png.ToArray();
    }

    private static byte[] Ihdr(int width, int height)
    {
        var data = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(4, 4), height);
        data[8] = 8;
        data[9] = 2;
        return data;
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(type);
        stream.Write(data);
        var crcInput = new byte[type.Length + data.Length];
        type.CopyTo(crcInput);
        data.CopyTo(crcInput.AsSpan(type.Length));
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(crcInput));
        stream.Write(crc);
    }

    private static void WriteUInt32Big(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static uint Adler32(ReadOnlySpan<byte> data)
    {
        uint a = 1;
        uint b = 0;
        foreach (var value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }

    private static readonly uint[] CrcTable = CreateCrcTable();

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) == 1 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
