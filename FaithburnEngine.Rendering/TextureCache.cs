using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.IO;

namespace FaithburnEngine.Rendering
{
    /// <summary>
    /// Simple runtime texture cache. Prefer AssetRegistry for mod-aware loading.
    /// </summary>
    public static class TextureCache
    {
        private static readonly ConcurrentDictionary<string, Texture2D> _cache = new();

        public static Texture2D GetOrLoad(GraphicsDevice gd, string relativePath)
        {
            return _cache.GetOrAdd(relativePath, key =>
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, "Content", key);
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Texture not found: {fullPath}");

                using var stream = File.OpenRead(fullPath);
                return Texture2D.FromStream(gd, stream);
            });
        }

        public static void Clear()
        {
            foreach (var kv in _cache)
                kv.Value.Dispose();
            _cache.Clear();
        }
    }
}
