using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FaithburnEngine.Content;

namespace FaithburnEngine.World
{
    /// <summary>
    /// Generates world terrain using noise and biome rules.
    /// Supports both WorldGrid (dictionary-backed) and ChunkedWorldGrid.
    /// </summary>
    public sealed class WorldGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _surfaceLevel;
        private readonly int _seed;

        public WorldGenerator(int widthInTiles, int heightInTiles, int surfaceLevel, int? seed = null)
        {
            _width = widthInTiles;
            _height = heightInTiles;
            _surfaceLevel = surfaceLevel;
            _seed = seed ?? Environment.TickCount;
        }

        /// <summary>
        /// Fill a dictionary-backed WorldGrid.
        /// </summary>
        public void FillWorld(WorldGrid world)
        {
            var perlin = new PerlinNoise(_seed);
            GenerateTerrain((x, y, blockId) => world.SetBlock(new Point(x, y), blockId), perlin);
            world.RecalculateAllVariants();
        }

        /// <summary>
        /// Fill a ChunkedWorldGrid.
        /// </summary>
        public void FillWorld(ChunkedWorldGrid world)
        {
            var perlin = new PerlinNoise(_seed);
            GenerateTerrain((x, y, blockId) => world.SetBlock(new Point(x, y), blockId), perlin);
            world.RecalculateAllVariants();
        }

        private void GenerateTerrain(Action<int, int, string> setBlock, PerlinNoise perlin)
        {
            int octaves = 6;
            float baseFrequency = 1f / 120f;
            float lacunarity = 2f;
            float persistence = 0.5f;
            float heightAmplitude = 30f;

            float cliffFreq = baseFrequency * 6f;
            float cliffThreshold = 0.35f;
            float cliffMultiplier = 4.0f;
            int cliffQuantizeStep = 2;

            for (int x = 0; x < _width; x++)
            {
                float nx = x * baseFrequency;
                float noise = FBM(perlin, nx, 0f, octaves, lacunarity, persistence);

                float offsetF = noise * heightAmplitude;

                float cliffNoise = perlin.Noise(x * cliffFreq, 42f);
                if (cliffNoise > cliffThreshold)
                {
                    float extra = (cliffNoise - cliffThreshold) / (1f - cliffThreshold);
                    float amplify = 1f + extra * cliffMultiplier;
                    offsetF *= amplify;

                    if (cliffQuantizeStep > 1)
                    {
                        offsetF = (float)Math.Round(offsetF / cliffQuantizeStep) * cliffQuantizeStep;
                    }
                }

                int offset = (int)Math.Round(offsetF);
                int surfaceY = Math.Clamp(_surfaceLevel + offset, 1, _height - 2);

                setBlock(x, surfaceY, "grass_dirt");

                for (int y = surfaceY + 1; y < _height; y++)
                {
                    setBlock(x, y, "grass_dirt");
                }
            }
        }

        private static float FBM(PerlinNoise perlin, float x, float y, int octaves, float lacunarity, float persistence)
        {
            float amp = 1f;
            float freq = 1f;
            float sum = 0f;
            float max = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += perlin.Noise(x * freq, y * freq) * amp;
                max += amp;
                amp *= persistence;
                freq *= lacunarity;
            }

            return sum / max;
        }

        private sealed class PerlinNoise
        {
            private readonly int[] _perm = new int[512];

            public PerlinNoise(int seed)
            {
                var rnd = new Random(seed);
                var p = new int[256];
                for (int i = 0; i < 256; i++) p[i] = i;
                for (int i = 255; i >= 0; i--)
                {
                    int swap = rnd.Next(i + 1);
                    int tmp = p[i];
                    p[i] = p[swap];
                    p[swap] = tmp;
                }
                for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
            }

            private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
            private static float Lerp(float a, float b, float t) => a + t * (b - a);
            private static float Grad(int hash, float x, float y)
            {
                int h = hash & 7;
                float u = h < 4 ? x : y;
                float v = h < 4 ? y : x;
                return (((h & 1) == 0) ? u : -u) + (((h & 2) == 0) ? v : -v);
            }

            public float Noise(float x, float y)
            {
                int xi = (int)Math.Floor(x) & 255;
                int yi = (int)Math.Floor(y) & 255;
                float xf = x - (float)Math.Floor(x);
                float yf = y - (float)Math.Floor(y);

                float u = Fade(xf);
                float v = Fade(yf);

                int aa = _perm[xi] + yi;
                int ab = _perm[xi] + yi + 1;
                int ba = _perm[xi + 1] + yi;
                int bb = _perm[xi + 1] + yi + 1;

                float x1 = Lerp(Grad(_perm[aa], xf, yf), Grad(_perm[ba], xf - 1, yf), u);
                float x2 = Lerp(Grad(_perm[ab], xf, yf - 1), Grad(_perm[bb], xf - 1, yf - 1), u);

                return Lerp(x1, x2, v);
            }
        }
    }
}