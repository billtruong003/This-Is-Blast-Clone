using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "JellyGunner/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Title("Grid")]
        [Range(0.8f, 2f)] public float cellSize = 1.2f;
        [Range(0.01f, 0.5f)] public float gridAdvanceSpeed = 0.05f;

        [Title("Tray")]
        [Range(0.2f, 0.8f)] public float blasterFlyDuration = 0.35f;
        [Range(0.15f, 0.5f)] public float mergeFlyDuration = 0.25f;

        [Title("Combat")]
        [Range(1f, 50f)] public float projectileSpeed = 25f;
        [Range(0.1f, 2f)] public float projectileScale = 0.3f;

        [Title("Jelly Visual")]
        [Range(0.1f, 1f)] public float deformDecayRate = 0.85f;
        [Range(0.05f, 0.5f)] public float idleBreathAmplitude = 0.08f;
        [Range(1f, 5f)] public float idleBreathSpeed = 2f;
        [Range(0.1f, 1f)] public float deathShrinkDuration = 0.35f;
        [Range(0.1f, 1f)] public float deathSpinSpeed = 0.2f;

        [Title("Culling")]
        [Range(50f, 500f)] public float cullDistance = 200f;
        [Range(0.5f, 5f)] public float boundsPadding = 1.5f;

        [Title("Supply")]
        [Range(8, 64)] public int supplyBufferCapacity = 32;

        [Title("Hammer")]
        [Range(0.3f, 1f)] public float hammerSwingDuration = 0.5f;
        [Range(0.1f, 0.5f)] public float hammerImpactDelay = 0.25f;

        [Title("Deadlock Warning")]
        [Range(0.3f, 1f)] public float warningFlashInterval = 0.5f;
    }
}
