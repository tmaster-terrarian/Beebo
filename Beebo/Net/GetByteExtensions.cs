using System;
using System.Buffers.Binary;
using System.IO;

namespace Beebo.Net;

public static class GetByteExtensions
{
    public static byte[] GetBytes(this int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        return buffer.ToArray();
    }

    public static byte[] GetBytes(this uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        return buffer.ToArray();
    }

    public static byte[] GetBytes(this long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        return buffer.ToArray();
    }

    public static byte[] GetBytes(this ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        return buffer.ToArray();
    }

    public static byte[] GetBytes(this short value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        return buffer.ToArray();
    }

    public static byte[] GetBytes(this ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        return buffer.ToArray();
    }

    public static byte[] GetBytes(this string value, System.Text.Encoding encoding = null)
    {
        byte[] result = null;

        using (var stream = new MemoryStream())
        {
            using var writer = new BinaryWriter(stream, encoding ?? System.Text.Encoding.UTF8);
            writer.Write(value);
            result = stream.GetBuffer();
        }

        return result;
    }
}
