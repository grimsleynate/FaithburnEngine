using System;
using System.Collections.Generic;

namespace FaithburnEngine.Systems.HeldAnimations
{
    public sealed class HeldAnimationRegistry
    {
        private readonly Dictionary<string, IHeldItemAnimator> _map = new(StringComparer.OrdinalIgnoreCase);

        public void Register(string key, IHeldItemAnimator animator)
        {
            _map[key] = animator;
        }

        public bool TryGet(string key, out IHeldItemAnimator animator)
        {
            return _map.TryGetValue(key, out animator!);
        }
    }
}
