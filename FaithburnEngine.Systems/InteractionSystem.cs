using DefaultEcs.System;
using FaithburnEngine.Core;
using FaithburnEngine.Rendering;
using FaithburnEngine.World;
using Microsoft.Xna.Framework;

namespace FaithburnEngine.Systems
{
    public sealed class InteractionSystem : ISystem<float>
    {
        private readonly Content.ContentLoader _content;
        private readonly InventorySystem _inventorySystem;
        private readonly IWorldGrid _world;
        private readonly Camera2D _camera;
        private readonly PlayerContext _player;

        public bool IsEnabled { get; set; } = true;

        public InteractionSystem(Content.ContentLoader content, InventorySystem invSys, IWorldGrid world, Camera2D camera, PlayerContext player)
        {
            _content = content;
            _inventorySystem = invSys;
            _world = world;
            _camera = camera;
            _player = player;
        }

        public void Update(float dt)
        {
        }

        public void Dispose()
        {
        }
    }
}