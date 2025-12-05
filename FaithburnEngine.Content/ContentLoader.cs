using FaithburnEngine.Content.Models;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FaithburnEngine.Content
{
    public sealed class ContentLoader
    {
        private readonly string _contentRoot;
        private readonly List<ItemDef> _pendingItems = new();
        private readonly List<BlockDef> _pendingBlocks = new();
        private readonly List<HarvestRule> _pendingHarvestRules = new();

        public string ContentRoot => _contentRoot;

        public IReadOnlyList<ItemDef> Items { get; private set; } = new List<ItemDef>();
        public IReadOnlyList<BlockDef> Blocks { get; private set; } = new List<BlockDef>();
        public IReadOnlyList<HarvestRule> HarvestRules { get; private set; } = new List<HarvestRule>();
        public Dictionary<string, EnemyAISettings> EnemyAI { get; private set; } = new();

        // WHY Immutable: publish content lookups via immutable dictionaries to allow
        // concurrent reads safely across systems without locks. Content is loaded at boot
        // and treated as read-only data thereafter.
        public ImmutableDictionary<string, ItemDef> ItemsById { get; private set; }
            = ImmutableDictionary<string, ItemDef>.Empty;
        public ImmutableDictionary<string, BlockDef> BlocksById { get; private set; }
            = ImmutableDictionary<string, BlockDef>.Empty;

        public ItemDef? GetItem(string id) => ItemsById.TryGetValue(id, out var def) ? def : null;
        public BlockDef? GetBlock(string id) => BlocksById.TryGetValue(id, out var def) ? def : null;

        public ContentLoader(string contentRoot)
        {
            _contentRoot = contentRoot;
        }

        public void LoadAll()
        {
            Debug.WriteLine($"ContentLoader.LoadAll root={_contentRoot}");
            TryLogDirectory(_contentRoot);

            // WHY JSON-driven loaders:
            // - Designers can author content without recompilation.
            // - Mod-friendly: content packs can drop new JSON into known folders.
            // - Versionable: schemas evolve while code stays stable.
            Items = LoadJsonList<ItemDef>("Items/Items.json");
            Blocks = LoadJsonList<BlockDef>("Blocks/Blocks.json");
            HarvestRules = LoadJsonList<HarvestRule>("Harvest_Rules/Harvest_Rules.json");
            EnemyAI = LoadJsonDict<Dictionary<string, EnemyAISettings>>("Enemy_Ai_Settings/Enemy_Ai_Settings.json");

            // Scan mods folder for manifests and merge
            ScanModsAndMerge(Path.Combine(_contentRoot, "Mods"));

            // Merge pending registry content (from mods via registry API)
            if (_pendingItems.Count > 0) Items = Items.Concat(_pendingItems).ToList();
            if (_pendingBlocks.Count > 0) Blocks = Blocks.Concat(_pendingBlocks).ToList();
            if (_pendingHarvestRules.Count > 0) HarvestRules = HarvestRules.Concat(_pendingHarvestRules).ToList();

            // Build indices with id conflict policy: last writer wins, log conflicts
            ItemsById = BuildIndex(Items, i => i.Id, "ItemDef");
            BlocksById = BuildIndex(Blocks, b => b.Id, "BlockDef");

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
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var json = File.ReadAllText(path);
                var rules = JsonSerializer.Deserialize<List<HarvestRule>>(json, options);

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

        private void ScanModsAndMerge(string modsRoot)
        {
            try
            {
                if (!Directory.Exists(modsRoot)) return;
                var manifests = Directory.GetFiles(modsRoot, "*.json", SearchOption.AllDirectories);
                foreach (var manifest in manifests)
                {
                    try
                    {
                        var json = File.ReadAllText(manifest);
                        var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("items", out var itemsEl))
                        {
                            var modsItems = JsonSerializer.Deserialize<List<ItemDef>>(itemsEl.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (modsItems != null) _pendingItems.AddRange(modsItems);
                        }
                        if (doc.RootElement.TryGetProperty("blocks", out var blocksEl))
                        {
                            var modsBlocks = JsonSerializer.Deserialize<List<BlockDef>>(blocksEl.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (modsBlocks != null) _pendingBlocks.AddRange(modsBlocks);
                        }
                        if (doc.RootElement.TryGetProperty("harvestRules", out var hrEl))
                        {
                            var modsHr = JsonSerializer.Deserialize<List<HarvestRule>>(hrEl.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (modsHr != null) _pendingHarvestRules.AddRange(modsHr);
                        }
                        Debug.WriteLine($"Loaded mod manifest: {manifest}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error parsing mod manifest {manifest}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScanMods error: {ex}");
            }
        }

        private ImmutableDictionary<string, T> BuildIndex<T>(IEnumerable<T> src, Func<T, string> keySel, string kind)
        {
            var dict = new Dictionary<string, T>();
            foreach (var it in src)
            {
                var id = keySel(it);
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (dict.ContainsKey(id))
                {
                    Debug.WriteLine($"{kind} id conflict: {id} — last writer wins.");
                }
                dict[id] = it;
            }
            return dict.ToImmutableDictionary();
        }

        // Simple registry implementation
        public IContentRegistry GetRegistry()
        {
            return new RegistryImpl(this);
        }

        public interface IContentRegistry
        {
            void RegisterItemDef(ItemDef item);
            void RegisterBlockDef(BlockDef block);
            void RegisterHarvestRule(HarvestRule rule);
        }

        private sealed class RegistryImpl : IContentRegistry
        {
            private readonly ContentLoader _owner;
            public RegistryImpl(ContentLoader owner) { _owner = owner; }
            public void RegisterItemDef(ItemDef item) => _owner._pendingItems.Add(item);
            public void RegisterBlockDef(BlockDef block) => _owner._pendingBlocks.Add(block);
            public void RegisterHarvestRule(HarvestRule rule) => _owner._pendingHarvestRules.Add(rule);
        }
    }
}