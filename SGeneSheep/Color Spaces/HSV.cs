using System;
using Microsoft.Xna.Framework;

namespace SGeneSheep
{
    internal class HSV : ColorSpace
    {
        public float h;
        public float s;
        public float v;

        public HSV(float hue, float saturation, float value)
        {
        h = hue;
        s = saturation;
        v = value;
        }

        public override void Mutate(float strength, Random rand)
        {
            h += (2 * rand.NextSingle() * strength) - strength;
            s += ((2 * rand.NextSingle() * strength) - strength) / 360;
            v += ((2 * rand.NextSingle() * strength) - strength) / 360;

            h = (h + 360) % 360;
            s = Math.Clamp(s, 0, 1);
            v = Math.Clamp(v, 0, 1);
        }

        public override double GetDiff(ColorSpace other)
        {
            throw new NotImplementedException();
        }

        public override double GetDistance(ColorSpace other)
        {
            throw new NotImplementedException();
        }

        public override Color ToColor()
        {
            float chroma = s * v;
            float H = h / 60;
            float X = chroma * (1 - Math.Abs((H % 2) - 1));
            float m = v - chroma;

            Vector3 newCol;

            if (H <= 1)
            {
                newCol = new Vector3(chroma, X, 0);
            }
            else if (H <= 2)
            {
                newCol = new Vector3(X, chroma, 0);
            }
            else if (H <= 3)
            {
                newCol = new Vector3(0, chroma, X);
            }
            else if (H <= 4)
            {
                newCol = new Vector3(0, X, chroma);
            }
            else if (H <= 5)
            {
                newCol = new Vector3(X, 0, chroma);
            }
            else
            {
                newCol = new Vector3(chroma, 0, X);
            }

            newCol = new Vector3(newCol.X + m, newCol.Y + m, newCol.Z + m);
            newCol *= 255;
            return new Color((int)newCol.X, (int)newCol.Y, (int)newCol.Z);
        }

        public override ColorSpace DeepCopy()
        {
            return new HSV(h,s,v);
        }
    }
}
