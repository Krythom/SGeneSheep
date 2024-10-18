using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SColor = System.Drawing.Color;
using Bitmap = System.Drawing.Bitmap;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;

namespace SGeneSheep
{
    public class GeneSheep : Game
    {
        private readonly List<SheepChange> _changed;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        readonly Random rand = new();
        private readonly RandomEx _randBools = new();
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
        const int ColorVariation = 1;
        const int MaxIterations = -1;
        const bool BatchMode = false;
        const bool FromImage = false;

        private const int _speedup = 1;

        private int[] neighborSpecies;

        private Texture2D _tex;
        private Color[] _backingColors;
        private Memory2D<Color> _colors;

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
            _graphics.PreferredBackBufferHeight = 1000;
            _graphics.PreferredBackBufferWidth = 1000;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.ApplyChanges();
            InactiveSleepTime = new TimeSpan(0);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
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
            Debug.WriteLine("fps: " + (1000 / ms) + " (" + ms + "ms)" + " iterations: " + iterations + "/" +
                            MaxIterations + " active " + checkSet.Count);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();
            _tex.SetData(_backingColors);
            _spriteBatch.Draw(_tex, new Rectangle(0,0,1000,1000), Color.White);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void InitWorld()
        {
            neighborSpecies = new int[numSpecies];
            _backingColors = new Color[worldX * worldY];
            _colors = new Memory2D<Color>(_backingColors, worldX, worldY);
            _tex = new Texture2D(GraphicsDevice, worldX, worldY);

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
                            int steps = (int) (GetDistance(sample, col) / Tolerance);
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
                        _colors.Span[x, y] = world[x, y].color;
                        checkSet.Add(new Point(x, y));
                    }
                }
            }
        }

        HashSet<Point> toSleep = new();
        HashSet<Point> toWake = new();

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Iterate()
        {
            var colors = _colors.Span;
            toSleep.Clear();
            toWake.Clear();

            foreach (var loc in checkSet)
            {
                (int x, int y) = loc;
                Sheep s = world[x, y];
                int winningSpecies = GetWinner(s, out bool sleep);

                if (sleep)
                {
                    toSleep.Add(loc);
                }
                else if (winningSpecies != s.species)
                {
                    _changed.Add(new() { color = Mutator.Uniform(), id = winningSpecies, loc = loc });

                    foreach (var (dx, dy) in dirs)
                    {
                        var xp = Mod(s.x + dx, worldX);
                        var yp = Mod(s.y + dy, worldY);
                        
                        toWake.Add(world[xp, yp].GetPoint());
                    }
                }
            }

            foreach (SheepChange c in _changed)
            {
                Sheep s = world[c.loc.X, c.loc.Y];
                s.species = c.id;
                s.color = c.color;
                colors[c.loc.X, c.loc.Y] = s.color;
            }

            if (_changed is [] || iterations == MaxIterations)
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

        private static readonly (int, int)[] dirs =
        [
            (-1, -1),
            (0, -1),
            (1, -1),
            (-1, 0),
            (1, 0),
            (-1, 1),
            (0, 1),
            (1, 1)
        ];

        public class RandomEx : Random
        {
            private uint _boolBits;

            public RandomEx() : base()
            {
            }

            public RandomEx(int seed) : base(seed)
            {
            }

            public bool NextBoolean()
            {
                _boolBits >>= 1;
                if (_boolBits <= 1) _boolBits = (uint) ~this.Next();
                return (_boolBits & 1) == 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private int GetWinner(Sheep sheep, out bool toSleep)
        {
            Span<int> span = neighborSpecies.AsSpan();
            span.Clear();

            toSleep = false;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var xp = Mod(sheep.x + x, worldX);
                    var yp = Mod(sheep.y + y, worldY);

                    if (!(xp == sheep.x && yp == sheep.y))
                    {
                        span[world[xp, yp].species]++;
                    }
                }
            }

            int winningSpecies = -1;
            int highest = -1;

            for (int i = 0; i < numSpecies; i++)
            {
                if (span[i] > highest || (span[i] == highest && _randBools.NextBoolean()))
                {
                    winningSpecies = i;
                    highest = span[i];
                }
            }

            if (span[sheep.species] == 8)
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

                img.Save(@"ScreenCap_n" + numSpecies + "_v" + ColorVariation + "_i" + iterations + ".png",
                    System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static SColor ConvertColor(Color col)
        {
            SColor sCol = SColor.FromArgb(255, col.R, col.G, col.B);
            return sCol;
        }

        private static double GetDistance(Color sample, Color col)
        {
            return Math.Pow(
                Math.Pow(sample.R - col.R, 2) + Math.Pow(sample.G - col.G, 2) + Math.Pow(sample.B - col.B, 2), 0.5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static int Mod(int x, int m)
        {
            return (Math.Abs(x * m) + x) % m;
        }
    }
}