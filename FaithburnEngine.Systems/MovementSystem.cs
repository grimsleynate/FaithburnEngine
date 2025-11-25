using DefaultEcs;
using DefaultEcs.System;
using FaithburnEngine.Components;
using Microsoft.Xna.Framework;

namespace FaithburnEngine.Systems
{
    /// <summary>
    /// Updates Position based on Velocity. Pure system: no shared mutations.
    /// </summary>
    public sealed class MovementSystem : AEntitySetSystem<float>
    {
        public MovementSystem(DefaultEcs.World world)
            : base(world.GetEntities().With<Position>().With<Velocity>().AsSet())
        {
        }

        // In 0.17.x you override this signature
        protected override void Update(float dt, ReadOnlySpan<Entity> entities)
        {
            foreach (ref readonly var entity in entities)
            {
                ref var pos = ref entity.Get<Position>();
                ref var vel = ref entity.Get<Velocity>();
                pos.Value += vel.Value * dt;
            }
        }
    }
}
