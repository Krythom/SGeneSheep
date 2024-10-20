using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace SGeneSheep
{
    internal class Mutator
    {
        static int[] dis = new[] { -1, -1, -1, 0, 1, 1, 1, 0 };
        static int[] djs = new[] { -1, 0, 1, 1, 1, 0, -1, -1 };
        public static float mutationStrength;
        static Random rand = new();
        public static HSL uniformCol = new HSL(rand.Next(360), 0.7f, 0.4f);

        //At an offset of 4 makes spiral structures, as offset gets closer to 0 spirals become less distinct 
        public static ColorSpace Spiral(Sheep[,] world, Sheep sheep, int winner, int offset)
        {
            ColorSpace newCol;
            int worldX = world.GetLength(0);
            int worldY = world.GetLength(1);
            bool prev = world[Mod(sheep.x + dis[^1], worldX), Mod(sheep.y + djs[^1], worldY)].species == winner;
            int start = -1;
            int end = -1;

            for (int index = 0; index < 8; ++index)
            {
                bool curr = world[Mod(sheep.x + dis[index], worldX), Mod(sheep.y + djs[index], worldY)].species == winner;
                if (curr)
                {
                    if (!prev)
                    {
                        //found first winner
                        start = index;
                        if (end >= 0)
                        {
                            break;
                        }
                    }
                }
                else if (prev)
                {
                    //past last winner
                    end = index;
                    if (start >= 0)
                    {
                        break;
                    }
                }
                prev = curr;
            }

            int length = Mod(end - start, 8);
            int midpointIndex = start + length / 2;
            if (Mod(length, 2) == 0)//even # of winners
            {
                midpointIndex -= rand.Next(0, 2);
            }
            if (length >= 3)
            {
                midpointIndex += rand.Next(-1, 2);
            }
            midpointIndex += offset;
            midpointIndex = Mod(midpointIndex, 8);

            if (world[Mod(sheep.x + dis[midpointIndex], worldX), Mod(sheep.y + djs[midpointIndex], worldY)].species == winner)
            {
                newCol = world[Mod(sheep.x + dis[midpointIndex], worldX), Mod(sheep.y + djs[midpointIndex], worldY)].color;
            }
            else
            {
                newCol = world[Mod(sheep.x + dis[start], worldX), Mod(sheep.y + djs[start], worldY)].color;
            }

            newCol.Mutate(mutationStrength, rand);
            return newCol;
        }

        public static void IncrementUniform()
        {
            uniformCol.Mutate(mutationStrength, rand);
        }

        public static HSV Rainbow(HSV col)
        {
            col.h = (col.h + mutationStrength) % 360;
            return col;
        }

        public static ColorSpace Preservation(Sheep[,] world, Sheep sheep, int winner)
        {
            ColorSpace newCol;
            List<Sheep> neighbors = GetNeighbours(world, sheep, true);
            double lowest = double.MaxValue;
            int lowestIndex = 0;
            int current = 0;

            foreach (Sheep s in neighbors)
            {
                double sum = s.color.GetDiff(sheep.color);
                if (sum < lowest && s.species == winner)
                {
                    lowest = sum;
                    lowestIndex = current;
                }
                current++;
            }

            newCol = neighbors[lowestIndex].color;
            newCol.Mutate(mutationStrength, rand);
            return newCol;
        }

        public static ColorSpace Mirror(Sheep[,] world, Sheep sheep, int winner)
        {
            ColorSpace newCol;
            int mirrorX;
            int mirrorY;
            mirrorX = world.GetLength(0) - sheep.x - 1;
            mirrorY = world.GetLength(1) - sheep.y - 1;

            if (world[mirrorX, mirrorY].species == winner)
            {
                newCol = world[mirrorX, mirrorY].color;
            }
            else
            {
                newCol = sheep.color;
                List<Sheep> neighbors = GetNeighbours(world, world[mirrorX, mirrorY], true);
                foreach (Sheep neighbor in neighbors)
                {
                    if (neighbor.species == winner)
                    {
                        newCol = neighbor.color;
                        break;
                    }
                }
            }

            newCol.Mutate(mutationStrength, rand);
            return newCol;
        }

        private static List<Sheep> GetNeighbours(Sheep[,] world, Sheep s, bool shuffled)
        {
            int worldX = world.GetLength(0);
            int worldY = world.GetLength(1);
            List<Sheep> sheep = new()
            {
                world[Mod(s.x + -1, worldX), Mod(s.y + -1, worldY)],
                world[Mod(s.x + 0, worldX) , Mod(s.y + -1, worldY)],
                world[Mod(s.x + 1, worldX) , Mod(s.y + -1, worldY)],
                world[Mod(s.x + -1, worldX), Mod(s.y , worldY)],
                world[Mod(s.x + 1, worldX) , Mod(s.y, worldY)],
                world[Mod(s.x + -1, worldX) , Mod(s.y + 1, worldY)],
                world[Mod(s.x + 0, worldX) , Mod(s.y + 1, worldY)],
                world[Mod(s.x + 1, worldX) , Mod(s.y + 1, worldY)],

            };

            if (shuffled)
            {
                int n = sheep.Count;
                while (n > 1)
                {
                    n--;
                    int k = rand.Next(n + 1);
                    Sheep temp = sheep[k];
                    sheep[k] = sheep[n];
                    sheep[n] = temp;
                }
            }
            return sheep;
        }

        private static int Mod(int x, int m)
        {
            return (Math.Abs(x * m) + x) % m;
        }
    }
}
