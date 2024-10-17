using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SColor = System.Drawing.Color;
using Bitmap = System.Drawing.Bitmap;

namespace SGeneSheep
{
    public class GeneSheep : Game
    {
        private readonly List<SheepChange> _changed;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        readonly Random rand = new();
        Texture2D square;
        Sheep[,] world;
        List<Sheep> species = new();


        bool completed = false;
        bool saved = false;
        int iterations = 0;
        HashSet<Point> checkSet = new();

        int worldX = 500;
        int worldY = 500;
        int numSpecies = 10;
        const double Tolerance = 10;
        const int CellSize = 2;
        const int ColorVariation = 1;
        const int MaxIterations = -1;
        const bool BatchMode = false;
        const bool ShowBorders = false;
        const bool FromImage = false;

        private const int _speedup = 50;

        public GeneSheep()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = false;
            _changed = new() { Capacity = worldX * worldY };
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
            InitWorld();
            _graphics.PreferredBackBufferHeight = worldY * CellSize;
            _graphics.PreferredBackBufferWidth = worldX * CellSize;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            InactiveSleepTime = new TimeSpan(0);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            square = Content.Load<Texture2D>("whiteSquare");
        }

        protected override void Update(GameTime gameTime)
        {
            if (completed)
            {
                if (!saved)
                {
                    SaveImage();
                    saved = true;
                }
                if (BatchMode)
                {
                    completed = false;
                    saved = false;
                    Initialize();
                    iterations = 0;
                }
            }
            else
            {
                Iterate();
                iterations++;
                if (_speedup == 0 || iterations % _speedup != 0)
                {
                    SuppressDraw();
                }
            }

            double ms = gameTime.ElapsedGameTime.TotalMilliseconds;
            Debug.WriteLine("fps: " + (1000/ms) + " (" + ms + "ms)" + " iterations: " + iterations + "/" + MaxIterations + " active " + checkSet.Count);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            
            _spriteBatch.Begin();

            for (int x = 0; x < worldX; x++)
            {
                for (int y = 0; y < worldY; y++)
                {
                    if (checkSet.Contains(new Point(x, y)) && ShowBorders)
                    {
                        Rectangle squarePos = new(new Point((x * CellSize), (y * CellSize)), new Point(CellSize, CellSize));
                        _spriteBatch.Draw(square, squarePos, new Color(255,255,255));
                    }
                    else
                    {
                        Rectangle squarePos = new(new Point((x * CellSize), (y * CellSize)), new Point(CellSize, CellSize));
                        _spriteBatch.Draw(square, squarePos, world[x, y].color);
                    }
                }
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void InitWorld()
        {
            if (FromImage)
            {
                if (!Directory.Exists(@"Images"))
                {
                    Directory.CreateDirectory(@"Images");
                }
                try
                {
                    Bitmap img = new(@"Images\input.png");
                    Color sample = new(img.GetPixel(0, 0).R, img.GetPixel(0, 0).G, img.GetPixel(0, 0).B);
                    worldX = img.Width;
                    worldY = img.Height;
                    world = new Sheep[worldX, worldY];

                    for (int x = 0; x < worldX; x++)
                    {
                        for (int y = 0; y < worldY; y++)
                        {
                            Color col = new(img.GetPixel(x, y).R, img.GetPixel(x, y).G, img.GetPixel(x, y).B);
                            int steps = (int)(GetDistance(sample, col) / Tolerance);
                            if (steps >= numSpecies)
                            {
                                numSpecies = steps + 1;
                            }
                            world[x, y] = new Sheep(col, steps);
                            world[x, y].x = x;
                            world[x, y].y = y;
                            checkSet.Add(new Point(x, y));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            else
            {
                world = new Sheep[worldX, worldY];
                species.Clear();
                for (int i = 0; i < numSpecies; i++)
                {
                    species.Add(new Sheep(Mutator.uniformCol, i));
                }
                for (int x = 0; x < worldX; x++)
                {
                    for (int y = 0; y < worldY; y++)
                    {
                        world[x, y] = species[rand.Next(numSpecies)].DeepCopy();
                        world[x, y].x = x;
                        world[x, y].y = y;
                        checkSet.Add(new Point(x, y));
                    }
                }
            }
        }

        private void Iterate()
        {
            Sheep[,] newWorld = new Sheep[worldX, worldY];

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
                    _changed.Add(new() { color = Mutator.Uniform(), id = winningSpecies, loc = loc });
                    lock (toWake)
                    {
                        toWake.UnionWith(GetNeighbours(s).Select(x => { return x.GetPoint(); }));
                    }
                }
            });

            foreach (SheepChange c in _changed)
            {
                Sheep s = world[c.loc.X, c.loc.Y];
                s.species = c.id;
                s.color = c.color;
            }
            if (_changed.Count == 0 || iterations == MaxIterations)
            {
                completed = true;
            }

            checkSet.UnionWith(toWake);
            checkSet.ExceptWith(toSleep);
            _changed.Clear();

            Mutator.uniformCol = new Color(Mutator.uniformCol.R + rand.Next(-ColorVariation, ColorVariation + 1),
                                           Mutator.uniformCol.G + rand.Next(-ColorVariation, ColorVariation + 1),
                                           Mutator.uniformCol.B + rand.Next(-ColorVariation, ColorVariation + 1));
        }

        private List<Sheep> GetNeighbours(Sheep s)
        {
            List<Sheep> sheep = new()
            {
                world[Mod(s.x + -1, worldX), Mod(s.y + -1, worldY)],
                world[Mod(s.x + 0, worldX), Mod(s.y + -1, worldY)],
                world[Mod(s.x + 1, worldX), Mod(s.y + -1, worldY)],
                world[Mod(s.x + -1, worldX), Mod(s.y, worldY)],
                world[Mod(s.x + 1, worldX), Mod(s.y, worldY)],
                world[Mod(s.x + -1, worldX), Mod(s.y + 1, worldY)],
                world[Mod(s.x + 0, worldX), Mod(s.y + 1, worldY)],
                world[Mod(s.x + 1, worldX), Mod(s.y + 1, worldY)],

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
                    int getX = Mod(x + sheep.x, worldX);
                    int getY = Mod(y + sheep.y, worldY);
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

        private void SaveImage()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Bitmap img = new Bitmap(worldX, worldY);

                for (int x = 0; x < worldX; x++)
                {
                    for (int y = 0; y < worldY; y++)
                    {
                        img.SetPixel(x, y, ConvertColor(world[x, y].color));
                    }
                }

                img.Save(@"ScreenCap_n" + numSpecies + "_v" + ColorVariation + "_i" + iterations + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static SColor ConvertColor(Color col)
        {
            SColor sCol = SColor.FromArgb(255, col.R, col.G, col.B);
            return sCol;
        }

        private static double GetDistance(Color sample, Color col)
        {
            return Math.Pow(Math.Pow(sample.R - col.R, 2) + Math.Pow(sample.G - col.G, 2) + Math.Pow(sample.B - col.B, 2), 0.5);
        }

        private static int Mod(int x, int m)
        {
            return (Math.Abs(x * m) + x) % m;
        }
    }
}