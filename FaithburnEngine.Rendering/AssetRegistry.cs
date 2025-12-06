using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace FaithburnEngine.Rendering
{
    // Simple mod-aware asset resolver. First-mod-wins by search order.
    public sealed class AssetRegistry
    {
        private readonly GraphicsDevice _gd;
        private readonly List<string> _searchRoots = new();
        private readonly Dictionary<string, string> _keyToRelPath = new();
        private readonly Dictionary<string, Texture2D> _textureCache = new();

        public AssetRegistry(GraphicsDevice gd, IEnumerable<string> searchRoots)
        {
            _gd = gd;
            _searchRoots.AddRange(searchRoots);
        }

        // Register logical key to relative path (under a search root).
        public void Register(string key, string relativePath) => _keyToRelPath[key] = relativePath;

        // First-mod-wins: iterate search roots in order and return first hit.
        public bool TryGetTexture(string key, out Texture2D texture)
        {
            texture = null!;
            if (!_keyToRelPath.TryGetValue(key, out var rel)) return false;
            return TryGetTextureByPath(rel, out texture);
        }

        // Allows loading by relative path directly (for content-driven items that store paths).
        public bool TryGetTextureByPath(string relativePath, out Texture2D texture)
        {
            texture = null!;
            var cacheKey = string.Join("|", _searchRoots) + "::PATH::" + relativePath;
            if (_textureCache.TryGetValue(cacheKey, out var cached))
            {
                texture = cached;
                return true;
            }

            foreach (var root in _searchRoots)
            {
                var full = Path.Combine(root, relativePath);
                if (File.Exists(full))
                {
                    using var fs = File.OpenRead(full);
                    texture = Texture2D.FromStream(_gd, fs);
                    _textureCache[cacheKey] = texture;
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            foreach (var kv in _textureCache)
            {
                kv.Value.Dispose();
            }
            _textureCache.Clear();
        }
    }
}
