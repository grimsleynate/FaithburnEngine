using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Content
{
    public sealed class Atlas
    {
        public Texture2D? Texture { get; private set; }

        public Dictionary<string, Rectangle> Regions { get; } = new();

        public string Name { get; }

        public Atlas(string name)
        {
            Name = name;
        }

        public void BindTexture(Texture2D texture)
        {
            Texture = texture;
        }

        public bool TryGetRegion(string id, out Rectangle region) => Regions.TryGetValue(id, out region);

        public Rectangle GetRegionOrThrow(string id) 
        {
            if (!TryGetRegion(id, out var region))
                throw new KeyNotFoundException($"REgion '{id}' not found in atlas '{Name}'.");
            return region;
        }
    }
}