using System.IO.Compression;

namespace SEBlueprint.Core.Imaging;

public static class Png
{
    static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(int width, int height, byte[] rgba)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var header = new byte[13];
        WriteBigEndian(header, 0, (uint)width);
        WriteBigEndian(header, 4, (uint)height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(output, "IHDR", header);

        var stride = width * 4;
        var raw = new byte[(stride + 1) * height];
        for (var y = 0; y < height; y++)
            Buffer.BlockCopy(rgba, y * stride, raw, y * (stride + 1) + 1, stride);
        using (var compressed = new MemoryStream())
        {
            using (var z = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
                z.Write(raw);
            WriteChunk(output, "IDAT", compressed.ToArray());
        }
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    static void WriteChunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4];
        WriteBigEndian(len, 0, (uint)data.Length);
        s.Write(len);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes);
        s.Write(data);
        var crc = 0xFFFFFFFFu;
        foreach (var b in typeBytes) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        var crcBytes = new byte[4];
        WriteBigEndian(crcBytes, 0, crc ^ 0xFFFFFFFFu);
        s.Write(crcBytes);
    }

    static void WriteBigEndian(byte[] b, int offset, uint v)
    {
        b[offset] = (byte)(v >> 24);
        b[offset + 1] = (byte)(v >> 16);
        b[offset + 2] = (byte)(v >> 8);
        b[offset + 3] = (byte)v;
    }

    static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}

public static class IconFiles
{
    public static string FileName(string iconPath) =>
        System.Text.RegularExpressions.Regex.Replace(
            Path.ChangeExtension(iconPath.Replace('\\', '/'), ".png").ToLowerInvariant(), "[^a-z0-9._-]+", "_");
}
