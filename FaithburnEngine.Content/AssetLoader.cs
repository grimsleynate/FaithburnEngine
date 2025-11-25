using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace FaithburnEngine.Content
{
    /// <summary>
    /// Async asset loader; reads raw bytes on background threads,
    /// then publishes GPU resources on the main thread.
    /// </summary>
    public sealed class AssetLoader
    {
        private readonly ConcurrentQueue<StagedTexture> _queue = new();
        private readonly ConcurrentDictionary<string, Texture2D> _textures = new();

        private readonly string _contentRoot;

        public AssetLoader(string contentRoot)
        {
            _contentRoot = contentRoot;
        }

        /// <summary>
        /// Loads textures onto the GPU synchronously.
        /// </summary>
        public void LoadTexture(GraphicsDevice gd, string relativePath)
        {
            var fullPath = Path.Combine(_contentRoot, relativePath);
            if(!File.Exists(fullPath))
                throw new FileNotFoundException($"Texture not found: {fullPath}");

            using var stream = File.OpenRead(fullPath);
            var tex = Texture2D.FromStream(gd, stream);
            _textures[relativePath] = tex;
        }

        /// <summary>
        /// Begin loading a texture from disk asynchronously.
        /// </summary>
        public void LoadTextureAsync(string relativePath)
        {
            var fullPath = Path.Combine(_contentRoot, relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Texture not found: {fullPath}");

            Task.Run(() =>
            {
                var bytes = File.ReadAllBytes(fullPath);
                _queue.Enqueue(new StagedTexture(relativePath, bytes));
            });
        }

        /// <summary>
        /// Publish staged textures onto the GPU. Call from main thread.
        /// </summary>
        public void Publish(GraphicsDevice gd)
        {
            while (_queue.TryDequeue(out var staged))
            {
                using var ms = new MemoryStream(staged.Bytes);
                var tex = Texture2D.FromStream(gd, ms);
                _textures[staged.Id] = tex;
            }
        }

        /// <summary>
        /// Get a texture if it has been published
        /// </summary>
        public Texture2D? GetTexture(string relativePath)
        {
            return _textures.TryGetValue(relativePath, out var tex) ? tex : null;
        }

        /// <summary>
        /// Dispose all cached textures.
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in _textures)
                kvp.Value.Dispose();
            _textures.Clear();
        }

        private readonly record struct StagedTexture(string Id, byte[] Bytes);
    }
}