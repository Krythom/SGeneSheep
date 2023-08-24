using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace SGeneSheep
{
    internal class Sheep
    {
        public Color color;
        public int species;
        public bool awake;
        public int x;
        public int y;

        public Sheep(Color color, int species, bool awake) 
        {
            this.color = color;
            this.species = species;
            this.awake = awake;
        }

        public Sheep DeepCopy()
        {
            Sheep copy = new(color, species, awake);
            return copy;
        }
    }
}
