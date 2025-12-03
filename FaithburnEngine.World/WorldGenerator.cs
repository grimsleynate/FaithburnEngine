using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FaithburnEngine.Content;

namespace FaithburnEngine.World
{
    /// <summary>
    /// Generates world terrain using noise and biome rules.
    /// 
    /// TENET #2 (Terraria-like):
    /// Procedural generation with biomes, surface/underground layers.
    /// 
    /// TENET #4 (ECS First):
    /// Generator is stateless. Inherits from no base class.
    /// Modders create custom generators by composition, not inheritance.
    /// </summary>
    public sealed class WorldGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _surfaceLevel;
        private readonly Random _rng;

        /// <summary>
        /// Creates a new world generator.
        /// width/height in tiles (not pixels).
        /// surfaceLevel = Y coordinate where ground starts.
        /// </summary>
        public WorldGenerator(int widthInTiles, int heightInTiles, int surfaceLevel, int? seed = null)
        {
            _width = widthInTiles;
            _height = heightInTiles;
            _surfaceLevel = surfaceLevel;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>
        /// Generates a flat world filled with grass/dirt tiles.
        /// PoC version: simple layering (surface grass, below is dirt).
        /// Future: Add Perlin noise for terrain variation, caves, etc.
        /// </summary>
        public void FillWorld(WorldGrid world)
        {
            // Fill sky (above surface) with air
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _surfaceLevel; y++)
                {
                    // Air tiles are implicit (not stored in dict)
                }
            }

            // Surface layer: grass
            for (int x = 0; x < _width; x++)
            {
                world.SetBlock(new Point(x, _surfaceLevel), "grass_dirt");
            }

            // Underground: dirt
            for (int x = 0; x < _width; x++)
            {
                for (int y = _surfaceLevel + 1; y < _height; y++)
                {
                    world.SetBlock(new Point(x, y), "grass_dirt");
                }
            }

            // Recalculate all variants now that all tiles exist
            world.RecalculateAllVariants();
        }
    }
}