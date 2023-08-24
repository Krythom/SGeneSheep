using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SGeneSheep
{
    public class GeneSheep : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        readonly Random rand = new();
        Texture2D square;
        Sheep[,] world;
        List<Sheep> species = new();
        int worldSize;
        int cellSize;
        int numSpecies;
        HashSet<Point> checkSet = new();
        const int colorVariation = 1;

        public GeneSheep()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = false;
        }

        public struct SheepChange
        {
            public Point loc;
            public int id;
            public Color color;

            public override int GetHashCode()
            {
                return loc.GetHashCode();
            }
        }

        protected override void Initialize()
        {
            worldSize = 600;
            cellSize = 1;
            numSpecies = 10;
            world = new Sheep[worldSize, worldSize];
            InitWorld();

            _graphics.PreferredBackBufferHeight = worldSize * cellSize;
            _graphics.PreferredBackBufferWidth = worldSize * cellSize;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            square = Content.Load<Texture2D>("whiteSquare");
        }

        protected override void Update(GameTime gameTime)
        {
            Iterate();
            double ms = gameTime.ElapsedGameTime.TotalMilliseconds;
            Debug.WriteLine("fps: " + (1000/ms) + "    (" + ms + "ms)");
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();

            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    Color squareColor = world[x,y].color;
                    Rectangle squarePos = new(new Point((x * cellSize), (y * cellSize)), new Point(cellSize, cellSize));
                    _spriteBatch.Draw(square, squarePos, squareColor);
                }
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void InitWorld()
        {
            for (int i = 0; i < numSpecies; i++)
            {
                species.Add(new Sheep(new Color(rand.Next(256), rand.Next(256), rand.Next(256)), i));
            }
            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    world[x, y] = species[rand.Next(numSpecies)].DeepCopy();
                    world[x, y].x = x;
                    world[x, y].y = y;
                    checkSet.Add(new Point(x, y));
                }
            }
        }

        private void Iterate()
        {
            Sheep[,] newWorld = new Sheep[worldSize, worldSize];

            List<SheepChange> changed = new() { Capacity = (int)Math.Pow(worldSize, 2) };
            HashSet<Point> toSleep = new();
            HashSet<Point> toWake = new();

            Parallel.ForEach(checkSet, loc =>
            {
                int x = loc.X;
                int y = loc.Y;
                Sheep s = world[x, y];
                int winningSpecies = GetWinner(s, out bool sleep);
                if (sleep)
                {
                    lock (toSleep)
                    {
                        toSleep.Add(loc);
                    }
                }
                else if (winningSpecies != s.species)
                {
                    changed.Add(new() { color = GetNewColor(s, winningSpecies), id = winningSpecies, loc = loc });
                    lock (toWake)
                    {
                        toWake.UnionWith(GetNeighbours(s).Select(x => { return x.GetPoint(); }));
                    }
                }
            });

            foreach (SheepChange c in changed)
            {
                Sheep s = world[c.loc.X, c.loc.Y];
                s.species = c.id;
                s.color = c.color;
            }
            checkSet.UnionWith(toWake);
            checkSet.ExceptWith(toSleep);
        }

        private List<Sheep> GetNeighbours(Sheep s)
        {
            List<Sheep> sheep = new()
            {
                world[Mod(s.x + -1, worldSize), Mod(s.y + -1, worldSize)],
                world[Mod(s.x + 0, worldSize), Mod(s.y + -1, worldSize)],
                world[Mod(s.x + 1, worldSize), Mod(s.y + -1, worldSize)],
                world[Mod(s.x + -1, worldSize), Mod(s.y , worldSize)],
                world[Mod(s.x + 1, worldSize), Mod(s.y, worldSize)],
                world[Mod(s.x + -1, worldSize), Mod(s.y + 1, worldSize)],
                world[Mod(s.x + 0, worldSize), Mod(s.y + 1, worldSize)],
                world[Mod(s.x + 1, worldSize), Mod(s.y + 1, worldSize)],

            };
            return sheep;
        }

        private int GetWinner(Sheep sheep, out bool toSleep)
        {
            int[] neighborSpecies = new int[numSpecies];
            toSleep = false;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int getX = Mod(x + sheep.x, worldSize);
                    int getY = Mod(y + sheep.y, worldSize);
                    if (!(getX == sheep.x && getY == sheep.y))
            {
                        neighborSpecies[world[getX, getY].species]++;
                    }
                }
            }

            int winningSpecies = -1;
            int highest = -1;

            for (int i = 0; i < numSpecies; i++)
            {
                if (neighborSpecies[i] > highest)
                {
                    winningSpecies = i;
                    highest = neighborSpecies[i];
                }
                else if (neighborSpecies[i] == highest && rand.NextDouble() > 0.5)
                {
                    winningSpecies = i;
                    highest = neighborSpecies[i];
                }
            }

            if (neighborSpecies[sheep.species] == 8)
            {
                toSleep = true;
            }

            return winningSpecies;
        }

        private Color GetNewColor(Sheep sheep, int winner)
        {
            List<Sheep> winners = new();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int getX = Mod(x + sheep.x, worldSize);
                    int getY = Mod(y + sheep.y, worldSize);
                    if (world[getX, getY].species == winner)
                    {
                        winners.Add(world[getX, getY]);
                    }
                }
            }
            Color newCol = winners[0].color;
            newCol.R = (byte)(Math.Clamp(newCol.R + rand.Next(-colorVariation, colorVariation + 1), 0, 255));
            newCol.G = (byte)(Math.Clamp(newCol.G + rand.Next(-colorVariation, colorVariation + 1), 0, 255));
            newCol.B = (byte)(Math.Clamp(newCol.B + rand.Next(-colorVariation, colorVariation + 1), 0, 255));
            return newCol;
        }

        private static int Mod(int x, int m)
        {
            return (Math.Abs(x * m) + x) % m;
        }
    }
}