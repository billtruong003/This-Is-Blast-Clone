namespace JellyGunner
{
    public static class TierConfig
    {
        public static int GetHP(EnemyTier tier) => tier switch
        {
            EnemyTier.Tiny => 1,
            EnemyTier.Standard => 20,
            EnemyTier.Medium => 60,
            EnemyTier.Tank => 120,
            _ => 1
        };

        public static int GetGridSize(EnemyTier tier) => tier switch
        {
            EnemyTier.Tiny => 1,
            EnemyTier.Standard => 1,
            EnemyTier.Medium => 2,
            EnemyTier.Tank => 3,
            _ => 1
        };

        public static int GetAmmo(BlasterType type) => type switch
        {
            BlasterType.Pistol => 20,
            BlasterType.Sniper => 60,
            BlasterType.Gatling => 120,
            _ => 20
        };

        public static float GetShotInterval(BlasterType type) => type switch
        {
            BlasterType.Pistol => 0.15f,
            BlasterType.Sniper => 0.08f,
            BlasterType.Gatling => 0.04f,
            _ => 0.15f
        };
    }
}
