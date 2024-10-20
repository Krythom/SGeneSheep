using Microsoft.Xna.Framework;
using System;

namespace SGeneSheep
{
    internal class CMYK : ColorSpace
    {
        public float c;
        public float m;
        public float y;
        public float k;

        public CMYK(float cyan, float magenta, float yellow, float black)
        {
            c = cyan;
            m = magenta;
            y = yellow;
            k = black;
        }

        public override void Mutate(float strength, Random rand)
        {
            c += ((2 * rand.NextSingle() * strength) - strength)/255;
            m += ((2 * rand.NextSingle() * strength) - strength)/255;
            y += ((2 * rand.NextSingle() * strength) - strength)/255;
            k += ((2 * rand.NextSingle() * strength) - strength)/255;

            Math.Clamp(c, 0, 1);
            Math.Clamp(m, 0, 1);
            Math.Clamp(y, 0, 1);
            Math.Clamp(k, 0, 1);
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
            float r = 255 * (1f - c) * (1f - k);
            float g = 255 * (1f - m) * (1f - k);
            float b = 255 * (1f - y) * (1f - k);

            return new Color((int)r, (int)g, (int)b);
        }

        public override ColorSpace DeepCopy()
        {
            return new CMYK(c, m, y, k);
        }
    }
}
