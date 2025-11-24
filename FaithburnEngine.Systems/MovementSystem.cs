using DefaultEcs;
using DefaultEcs.System;
using FaithburnEngine.Components;

namespace FaithburnEngine.Systems;
public class MovementSystem : AEntitySetSystem<float>
{
    public MovementSystem(DefaultEcs.World world) : base(world.GetEntities().With<Position>().With<Velocity>().AsSet()) { }

    protected override void Update(float deltaTime, in Entity entity)
    {
        ref var pos = ref entity.Get<Position>();
        ref var vel = ref entity.Get<Velocity>();
        pos.Value += vel.Value * deltaTime;
    }
}