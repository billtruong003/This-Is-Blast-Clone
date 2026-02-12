namespace JellyGunner
{
    public enum BlockColor : byte
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Count = 4
    }

    public enum EnemyTier : byte
    {
        Tiny = 0,
        Standard = 1,
        Medium = 2,
        Tank = 3
    }

    public enum BlasterType : byte
    {
        Pistol = 0,
        Sniper = 1,
        Gatling = 2
    }

    public enum GameState : byte
    {
        Loading,
        Playing,
        Paused,
        NearDeadlock,
        Deadlock,
        Victory
    }

    public enum BlasterState : byte
    {
        InSupply,
        FlyingToTray,
        Active,
        Empty,
        MergingIn,
        RunningAway
    }
}
