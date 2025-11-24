using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Rendering
{
    /// <summary>
    /// Simple runtime texture cache. Loads textures from Content folder via FromStream.
    /// Replace with MGCB/Atlas manager later.
    /// </summary>
    public static class TextureCache
    {
        private static readonly Dictionary<string, Texture2D> _cache = new();

        public static Texture2D GetOrLoad(GraphicsDevice gd, string relativePath)
        {
            if (_cache.TryGetValue(relativePath, out var tex))
                return tex;

            var fullPath = Path.Combine(System.AppContext.BaseDirectory, "Content", relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Texture not found: {fullPath}");

            using var stream = File.OpenRead(fullPath);
            tex = Texture2D.FromStream(gd, stream);
            _cache[relativePath] = tex;
            return tex;
        }

        public static void Clear()
        {
            foreach (var kv in _cache)
                kv.Value.Dispose();
            _cache.Clear();
        }
    }
}