using FaithburnEngine.Content.Models;
using System.Diagnostics;
using System.Text.Json;

namespace FaithburnEngine.Content
{
    public sealed class ContentLoader
    {
        private readonly string _contentRoot;

        public string ContentRoot => _contentRoot;

        public IReadOnlyList<ItemDef> Items { get; private set; } = new List<ItemDef>();
        public IReadOnlyList<BlockDef> Blocks { get; private set; } = new List<BlockDef>();
        public IReadOnlyList<HarvestRule> HarvestRules { get; private set; } = new List<HarvestRule>();
        public Dictionary<string, EnemyAISettings> EnemyAI { get; private set; } = new();

        public ContentLoader(string contentRoot)
        {
            _contentRoot = contentRoot;
        }

        public void LoadAll()
        {
            Debug.WriteLine($"ContentLoader.LoadAll root={_contentRoot}");
            TryLogDirectory(_contentRoot);

            Items = LoadJsonList<ItemDef>("items/items.json");
            Blocks = LoadJsonList<BlockDef>("blocks/blocks.json");
            HarvestRules = LoadJsonList<HarvestRule>("harvest_rules/harvest_rules.json");
            EnemyAI = LoadJsonDict<Dictionary<string, EnemyAISettings>>("enemy_ai_settings/enemy_ai_settings.json");

            Debug.WriteLine($"ContentLoader finished: Items={Items.Count}, Blocks={Blocks.Count}, HarvestRules={HarvestRules.Count}, EnemyAI={EnemyAI?.Count ?? 0}");
        }

        private IReadOnlyList<T> LoadJsonList<T>(string filename)
        {
            var path = Path.Combine(_contentRoot, filename);
            Debug.WriteLine($"LoadJsonList<{typeof(T).Name}> checking {path}");
            if (!File.Exists(path))
            {
                Debug.WriteLine($"ContentLoader: file not found: {path}");
                TryLogDirectory(Path.GetDirectoryName(path) ?? _contentRoot);
                return new List<T>();
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<T>();
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"ContentLoader: JSON deserialization error for {path}: {ex}");
                return new List<T>();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"ContentLoader: unexpected error reading {path}: {ex}");
                return new List<T>();
            }
        }

        private T LoadJsonDict<T>(string filename)
        {
            var path = Path.Combine(_contentRoot, filename);
            Debug.WriteLine($"LoadJsonDict<{typeof(T).Name}> checking {path}");
            if (!File.Exists(path))
            {
                Debug.WriteLine($"ContentLoader: file not found: {path}");
                TryLogDirectory(Path.GetDirectoryName(path) ?? _contentRoot);
                return default!;
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"ContentLoader: JSON deserialization error for {path}: {ex}");
                return default!;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"ContentLoader: unexpected error reading {path}: {ex}");
                return default!;
            }
        }

        private void TryLogDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            try
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Select(f => f.Replace(_contentRoot + Path.DirectorySeparatorChar, ""));
                    Debug.WriteLine($"Content files under {_contentRoot}:");
                    foreach (var f in files) Debug.WriteLine($" - {f}");
                }
                else
                {
                    Debug.WriteLine($"Directory does not exist: {dir}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error enumerating directory {dir}: {ex}");
            }
        }
    }
}