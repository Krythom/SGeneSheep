using Microsoft.Xna.Framework;
using System;

namespace SGeneSheep
{
    internal class HSL : ColorSpace
    {
        public float h;
        public float s;
        public float l;

        public HSL(float hue, float saturation, float luminosity) 
        {
            h = hue;
            s = saturation;
            l = luminosity;
        }

        public override void Mutate(float strength, Random rand)
        {
            h += (2 * rand.NextSingle() * strength) - strength;
            s += ((2 * rand.NextSingle() * strength) - strength)/360;
            l += ((2 * rand.NextSingle() * strength) - strength)/360;

            h = (h + 360) % 360;
            s = Math.Clamp(s, 0, 1);
            l = Math.Clamp(l, 0, 1);
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
            float chroma = (1 - Math.Abs((2 * l) - 1)) * s;
            float H = h / 60;
            float q = chroma * (1 - Math.Abs((H % 2) - 1));
            float m = l - chroma / 2;

            Vector3 newCol;

            if (H <= 1)
            {
                newCol = new Vector3(chroma, q, 0);
            }
            else if (H <= 2)
            {
                newCol = new Vector3(q, chroma, 0);
            }
            else if (H <= 3)
            {
                newCol = new Vector3(0, chroma, q);
            }
            else if (H <= 4)
            {
                newCol = new Vector3(0, q, chroma);
            }
            else if (H <= 5)
            {
                newCol = new Vector3(q, 0, chroma);
            }
            else
            {
                newCol = new Vector3(chroma, 0, q);
            }

            newCol = new Vector3(newCol.X + m, newCol.Y + m, newCol.Z + m);
            newCol *= 255;
            return new Color((int)newCol.X, (int)newCol.Y, (int)newCol.Z);
        }

        public override ColorSpace DeepCopy()
        {
            return new HSL(h, s, l);
        }
    }
}
