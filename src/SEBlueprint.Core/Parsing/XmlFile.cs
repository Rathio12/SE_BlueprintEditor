using System.IO.Compression;
using System.Xml.Linq;

namespace SEBlueprint.Core.Parsing;

/// <summary>Loads game XML files, which may be plain text or gzip-compressed (the game writes both).</summary>
public static class XmlFile
{
    public static XDocument Load(Stream stream)
    {
        var buffered = stream.CanSeek ? stream : Copy(stream);
        var start = buffered.Position;
        int b1 = buffered.ReadByte(), b2 = buffered.ReadByte();
        buffered.Position = start;
        if (b1 == 0x1F && b2 == 0x8B)
        {
            using var gz = new GZipStream(buffered, CompressionMode.Decompress, leaveOpen: true);
            return XDocument.Load(gz);
        }
        return XDocument.Load(buffered);
    }

    static MemoryStream Copy(Stream s)
    {
        var ms = new MemoryStream();
        s.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
