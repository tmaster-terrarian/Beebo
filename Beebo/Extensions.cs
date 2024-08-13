using System.Collections.Generic;
using Beebo.GameContent;
using Jelly;

namespace Beebo;

public static class Extensions
{
    public static string ToStringUTF8(this IEnumerable<byte> value)
    {
        return System.Text.Encoding.UTF8.GetString([..value]);
    }

    public static string ToStringASCII(this IEnumerable<byte> value)
    {
        return System.Text.Encoding.ASCII.GetString([..value]);
    }
}
