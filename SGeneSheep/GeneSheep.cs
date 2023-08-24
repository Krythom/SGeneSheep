using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        public GeneSheep()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = false;
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
            Debug.WriteLine("fps: " + (int)(1000/ms));
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
                species.Add(new Sheep(new Color(rand.Next(256), rand.Next(256), rand.Next(256)), i, true));
            }
            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    world[x, y] = species[rand.Next(numSpecies)].DeepCopy();
                    world[x, y].x = x;
                    world[x, y].y = y;
                }
            }
        }

        private void Iterate()
        {
            Sheep[,] newWorld = new Sheep[worldSize, worldSize];

            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    if (world[x, y].awake)
                    {
                        int winningSpecies = GetWinner(world[x, y]);
                        newWorld[x, y] = new(GetNewColor(world[x, y], winningSpecies), winningSpecies, world[x, y].awake)
                        {
                            x = x,
                            y = y
                        };

                        if (winningSpecies != world[x, y].species)
                        {
                            for (int xDiff = -1; xDiff <= 1; xDiff++)
                            {
                                for (int yDiff = -1; yDiff <= 1; yDiff++)
                                {
                                    int getX = Mod(x + xDiff, worldSize);
                                    int getY = Mod(y + yDiff, worldSize);
                                    world[getX, getY].awake = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        newWorld[x, y] = world[x, y];
                    }
                }
            }

            world = newWorld;
        }

        private int GetWinner(Sheep sheep)
        {
            int[] neighborSpecies = new int[numSpecies];

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
                sheep.awake = false;
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
            return newCol;
        }

        private static int Mod(int x, int m)
        {
            return (Math.Abs(x * m) + x) % m;
        }
    }
}