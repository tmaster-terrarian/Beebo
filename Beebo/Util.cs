using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo;

public static class Util
{
    public static bool TexturesRoughlyMatch(Texture2D tex1, Texture2D tex2, float matchThreshold, float tolerance)
    {
        int bufferSize = tex1.Width * tex1.Height;
        if(bufferSize != tex2.Width * tex2.Height)
        {
            return false;
        }

        Color[] tex1data = new Color[bufferSize];
        tex1.GetData(tex1data);

        Color[] tex2data = new Color[bufferSize];
        tex2.GetData(tex2data);

        int matched = 0;

        for(int i = 0; i < bufferSize; i++)
        {
            if(tex1data[i] == tex2data[i])
            {
                matched++;
                continue;
            }

            if(tolerance <= 0) continue;

            var c1 = tex1data[i].ToVector3();
            var c2 = tex2data[i].ToVector3();

            var diff = (c1 + ((c2 - c1) / 2)).LengthSquared() - c1.LengthSquared();
            if(diff < (tolerance * tolerance))
            {
                matched++;
            }
        }

        return (float)matched / bufferSize > matchThreshold;
    }
}
