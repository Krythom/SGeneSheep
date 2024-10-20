using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SColor = System.Drawing.Color;
using Bitmap = System.Drawing.Bitmap;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SGeneSheep
{
    public class GeneSheep : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        readonly Random rand = new();
        private readonly RandomEx _randBools = new();
        Sheep[,] world;
        List<Sheep> i = new();


        bool completed = false;
        bool saved = false;
        int iterations = 0;
        HashSet<(int, int)> checkSet = new();

        int worldX = 500;
        int worldY = 500;
        int numSpecies = 10;
        const double Tolerance = 10;
        const float ColorVariation = 10f;
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
        }

        public struct SheepChange
        {
            public (int, int) loc;
            public int id;
            public ColorSpace color;

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
            Mutator.mutationStrength = ColorVariation;
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
                    RGB sample = new(img.GetPixel(0, 0).R, img.GetPixel(0, 0).G, img.GetPixel(0, 0).B);
                    worldX = img.Width;
                    worldY = img.Height;
                    world = new Sheep[worldX, worldY];

                    for (int x = 0; x < worldX; x++)
                    {
                        for (int y = 0; y < worldY; y++)
                        {
                            RGB col = new(img.GetPixel(x, y).R, img.GetPixel(x, y).G, img.GetPixel(x, y).B);
                            int steps = (int) (sample.GetDiff(col) / Tolerance);
                            if (steps >= numSpecies)
                            {
                                numSpecies = steps + 1;
                            }

                            world[x, y] = new Sheep(col, steps);
                            world[x, y].x = x;
                            world[x, y].y = y;
                            checkSet.Add((x, y));
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
                i.Clear();
                for (int i = 0; i < numSpecies; i++)
                {
                    this.i.Add(new Sheep(Mutator.uniformCol, i));
                }

                for (int x = 0; x < worldX; x++)
                {
                    for (int y = 0; y < worldY; y++)
                    {
                        world[x, y] = i[rand.Next(i.Count)].DeepCopy();
                        world[x, y].x = x;
                        world[x, y].y = y;
                        _colors.Span[x,y] = world[x,y].color.ToColor();
                        checkSet.Add((x, y));
                    }
                }

                checkSet = checkSet.OrderBy(x => rand.Next()).ToHashSet();
            }
        }

        ConcurrentDictionary<(int,int), Byte> toSleep = new();
        ConcurrentDictionary<(int,int), Byte> toWake = new();

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Iterate()
        {
            int changed = 0;
            toSleep.Clear();
            toWake.Clear();

            foreach (var loc in checkSet)
            {
                (int x, int y) = loc;
                Sheep s = world[x, y];
                int winningSpecies = GetWinner(s, out bool sleep);

                if (sleep)
                {
                    toSleep[(x, y)] = 0;
                }
                else if (winningSpecies != s.species)
                {
                    changed++;
                    s.species = winningSpecies;
                    s.color = Mutator.uniformCol;
                    _colors.Span[x, y] = s.color.ToColor();


                    foreach (var (dx, dy) in dirs)
                    {
                        var xp = Mod(s.x + dx, worldX);
                        var yp = Mod(s.y + dy, worldY);

                        toWake[(xp, yp)] = 0;
                    }
                }
            }

            if (changed == 0 || iterations == MaxIterations)
            {
                completed = true;
            }

            foreach (var loc in toSleep.Keys)
            {
                checkSet.Remove((loc.Item1, loc.Item2));
            }
            foreach (var loc in toWake.Keys)
            {
                checkSet.Add((loc.Item1, loc.Item2));
            }
            Mutator.IncrementUniform();
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
            int winningSpecies = -1;
            int highest = -1;
            int[] span = new int[numSpecies];
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
            unsafe
            {
                fixed (void* ptr = _backingColors)
                {
                    var img = Image.WrapMemory<Rgba32>(
                        ptr,
                        _backingColors.AsSpan().AsBytes().Length,
                        worldX,
                        worldY
                    );

                    string date = DateTime.Now.ToString("s").Replace("T", " ").Replace(":", "-");

                    img.Save(
                        $"{date}_i{iterations}.png",
                        new PngEncoder()
                    );
                }
            }
        }

        private static SColor ConvertColor(Color col)
        {
            SColor sCol = SColor.FromArgb(255, col.R, col.G, col.B);
            return sCol;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static int Mod(int x, int m)
        {
            return (Math.Abs(x * m) + x) % m;
        }
    }
}