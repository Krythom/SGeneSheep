using Microsoft.Xna.Framework;
using System;

namespace SGeneSheep
{
    public abstract class ColorSpace
    {
        public abstract void Mutate(float strength, Random rand);

        public abstract Color ToColor();

        public abstract double GetDistance(ColorSpace other);

        public abstract double GetDiff(ColorSpace other);

        public abstract ColorSpace DeepCopy();
    }
}
