# Copilot Instructions for FaithburnEngine

## Priorities
1. Moddable. Has an extensive API that allows users to create custom mods of all shapes and sizes.
2. Built like Terraria / Starbound / Necesse. We are building a 2d side-scrolling game that emphasizes crafting, exploring, and battle. Very much in the same vein as Terraria and Starbound but modern. Use them as reference, but don't copy-paste, as much as possible. 
3. Multi-threaded and efficient. I need the code to be as efficient as possible as we'll possibly need to hold thousands of assets like Terraria.
4. Entity Component System first. This will allow us to craft many unique item and tile types.
5. Multiplayer up to X people on private servers or over wi-fi. We will not be hosting any servers ourselves. Think like Terraria.

## Art Workflow
- **Canvas sizes:** Tiles are 32x32 pixels. Bipedal characters should be about 2 tiles wide by 3 tiles high, or 64x96 pixels.
- **Item and Weapon Sizes:** Item and Weapon sizes will vary by pixel size, all item inventory icons will be 24x24 pixels. Weapon pixel size will dictate the size of the weapon's hitbox.
- **General settings:** sprites will be in png format. Animations will come through as spritesheets.

## Gameplay & Mechanics
- **Faithburn mechanic:** Cross-universe poisoning effect caused by Other Gods (from Earth). Separate items and boons are available in a Pure version (from Gods of your universe). They don't poison but also don't offer as strong of an effect.
- **Multiple classes:** Softly enforced through item and weapon synergies. A total of 4-6 currently undecided classes will be present. Think common trope classes like rogue mage and warrior, but also game-unique classes we will decide.
- **Combat:** Weapon / enemy sprites and hitboxes are separate entities. This allows smooth animations with solid math-based constant hitboxes. The size of the sprite will educate the size of the hitbox in pixels, but should otherwise be decoupled for smooth combat and animations.
- 
## Lore Integration
- **Myth sources:** Any Myth of Earth origin. Norse, Egyptian, Celtic, Polynesian, Mesopotamian, Greek are all great references, but we also want references from Voodoo and West Africa and Catholocism and other less known mythos. Reference authentic myths.

## Coding Standards
- **File naming:** `Item_<Name>.cs`, `Biome_<Name>.json`, `NPC_<Name>.cs`.
- **Comments:** Add exhaustive comments explaining why decisions were made.
- **Modularity:** Keep assets reusable and scalable. Avoid hardcoding lore into mechanics.

## Repo Organization
- `FaithburnEngine.Components` -> Will hold all of our components in our ECS design system.
- `FaithburnEngine.Content` -> Will hold all of our assets to be imported into CoreGame. These include our png files, fonts, UI images, and models. The Model folder must contain folders that hold a model definition .cs named <Model>Def.cs and a json file holding those items named <Model>.json. An example would be a blocks folder holding BlockDef.cs and blocks.json This also holds our loaders (AssetLoader.cs, Atlas.cs, and ContentLoader.cs)
- `FaithburnEngine.Core` -> Will hold all of our core mechanic definitions. Right now it holds Inventory.cs (a definition for our Inventory system) and InventorySlot (defines logic for a single InventorySlot).
- `FaithBurnEngine.CoreGame` -> The actual Game app, which holds our Program.cs and Faithburn.cs that call everything else.
- `FaithBurnEngine.ModAPI` -> Will hold the code for our Mod API.
- `FaithBurnEngine.Networking` -> Any code that relates to multiplayer and network syncing.
- `FaithBurnEngine.Rendering` -> Any code for rendering to screen. For reference we currently have our Camera2D class that defines our camera, HotbarRenderer that has helper methods for rendering the hotbar, SpriteRenderer that has helper methods for rendering sprites to screen, TextureCache that gets or loads textures or clears the cache.
- `FaithBurnEngine.Systems` -> Will hold the systems used by entities in our ECS system.
- `FaithBurnEngine.Tools` -> Will hold helper methods for running the code, but the methods might not necessarily output anything valuable to the user.
- `FaithBurnEngine.ModAPI` -> Will hold our code for our UI. Right now it holds 4 inventory classes.
- `FaithBurnEngine.ModAPI` -> Will hold the code related to world and tile generation.

## Checklist
- Build tile and tile creation to test world generation.
- Import required tile image files so we see them on screen.
