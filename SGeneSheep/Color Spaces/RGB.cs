using Microsoft.Xna.Framework;
using System;

namespace SGeneSheep
{
    internal class RGB : ColorSpace
    {
        public float r;
        public float g;
        public float b;

        public RGB(float red, float green, float blue) 
        { 
            r = red;
            g = green;
            b = blue;
        }

        public override void Mutate(float strength, Random rand)
        {
            r += (2 * rand.NextSingle() * strength) - strength;
            g += (2 * rand.NextSingle() * strength) - strength;
            b += (2 * rand.NextSingle() * strength) - strength;

            Math.Clamp(r, 0, 255);
            Math.Clamp(g, 0, 255);
            Math.Clamp(b, 0, 255);
        }

        public override double GetDiff(ColorSpace other)
        {
            RGB sub = (RGB) other;
            return Math.Abs(r - sub.r) + Math.Abs(g - sub.g) + Math.Abs(b - sub.b);
        }

        public override double GetDistance(ColorSpace other)
        {
            throw new NotImplementedException();
        }

        public override Color ToColor()
        {
            return new Color((int)r, (int)g, (int)b);
        }

        public override ColorSpace DeepCopy()
        {
            return new RGB(r, g, b);
        }
    }
}
