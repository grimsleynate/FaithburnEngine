# Modding Guide (Faithburn Engine)

This document outlines how to package and register mods.

## Mod Manifests

Place JSON manifests under `Content/Mods/`. A manifest may contain:

```json
{
  "items": [
    { "id": "mod_pickaxe", "type": "Tool", "toolKind": "Pickaxe", "stackMax": 1, "stats": { "damage": 2, "harvestPower": 25, "cooldown": 0.5 }, "spriteRef": "Assets/Tools/ModPickaxe" }
  ],
  "blocks": [
    { "id": "mod_dirt", "solid": true, "hardness": 10, "dropItemId": "mod_dirt_item", "spriteRef": "Assets/Tiles/ModDirt" }
  ],
  "harvestRules": [
    { "targetBlockId": "mod_dirt", "toolRequired": "Pickaxe", "minHarvestPower": 1, "yields": [ { "itemId": "mod_dirt_item", "minCount": 1, "maxCount": 3, "chance": 1.0 } ] }
  ]
}
```

The `ContentLoader` scans `Content/Mods/*` for manifests and merges the content into primary registries. Conflicts on ids will log a warning and the **last writer wins**.

## Runtime Registration API

For dynamic mods, use the registry API:

```csharp
var registry = contentLoader.GetRegistry();
registry.RegisterItemDef(new ItemDef { Id = "mod_pickaxe", /* ... */ });
registry.RegisterBlockDef(new BlockDef { Id = "mod_dirt", /* ... */ });
registry.RegisterHarvestRule(new HarvestRule { /* ... */ });
```

Registered content will be merged during `LoadAll()`.

## Custom Block Behaviors

Blocks can implement `IHarvestable` to customize post-harvest behavior:

```csharp
public sealed class Block_ModShrine : BlockDef, IHarvestable
{
    public override void OnHarvested(WorldGrid world, Microsoft.Xna.Framework.Point tile, FaithburnEngine.Core.PlayerContext player)
    {
        // spawn particle, drop special loot, etc.
    }
}
```

> Note: default `BlockDef` implements `IHarvestable` with a no-op. Mods can derive and override behavior or use composition.

## Packaging Guidelines

- Put sprites under `Content/Assets/` with subfolders by type (`Tiles`, `Tools`, `Sprites`).
- Keep ids unique (use a mod prefix like `myMod_` to prevent conflicts).
- Test manifests locally; check logs for conflict messages.

## Future Extensions

- Per-mod assemblies and script hooks.
- Biomes/NPC schemas and spawn rules.
- Chunked world implementation for large maps.
