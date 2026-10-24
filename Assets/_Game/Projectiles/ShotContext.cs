using System;
using System.Collections.Generic;
namespace TankGame.Projectiles
{
    // One action can own many projectiles. Children retain the same identity and attribution.
    public sealed class ShotContext
    {
        private static int nextId;
        private readonly HashSet<Projectile> members = new HashSet<Projectile>();
        public int Id { get; } = ++nextId;
        public int OwnerPlayerIndex { get; }
        public string VisualId { get; set; } = "weapon-rocket";
        public int ActiveCount => members.Count;
        public bool IsCancelled { get; private set; }
        private float minX = -40f, maxX = 40f, minY = -12f, maxY = 40f;
        public void SetBounds(float left, float right, float bottom, float top) { minX=left; maxX=right; minY=bottom; maxY=top; }
        public bool Contains(UnityEngine.Vector2 point) => point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        public ShotContext(int owner) { OwnerPlayerIndex = owner; }
        internal void Add(Projectile projectile)
        {
            if (IsCancelled) throw new InvalidOperationException("Cannot add a projectile to a cancelled shot.");
            members.Add(projectile);
        }
        internal void Remove(Projectile projectile) => members.Remove(projectile);
        public void Cancel()
        {
            if (IsCancelled) return;
            IsCancelled = true;
            var snapshot = new List<Projectile>(members);
            foreach (var projectile in snapshot) if (projectile != null) projectile.CancelWithoutExplosion();
            members.Clear();
        }
    }
}
