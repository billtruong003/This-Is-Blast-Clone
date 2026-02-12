using UnityEngine;

namespace JellyGunner
{
    public struct EnemyBlock
    {
        public int GridX;
        public int GridY;
        public BlockColor Color;
        public EnemyTier Tier;
        public int MaxHP;
        public int CurrentHP;
        public float DeformImpact;
        public float DeathProgress;
        public float HighlightPulse;
        public bool IsAlive;
        public bool IsDying;
        public Vector3 WorldPosition;
        public Vector3 TargetWorldPosition;
        public float FallVelocity;

        public float HPNormalized => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
        public int GridSize => TierConfig.GetGridSize(Tier);

        public static EnemyBlock Create(int x, int y, BlockColor color, EnemyTier tier, Vector3 worldPos)
        {
            int hp = TierConfig.GetHP(tier);
            return new EnemyBlock
            {
                GridX = x,
                GridY = y,
                Color = color,
                Tier = tier,
                MaxHP = hp,
                CurrentHP = hp,
                DeformImpact = 0f,
                DeathProgress = 0f,
                HighlightPulse = 0f,
                IsAlive = true,
                IsDying = false,
                WorldPosition = worldPos,
                TargetWorldPosition = worldPos,
                FallVelocity = 0f
            };
        }
    }
}
