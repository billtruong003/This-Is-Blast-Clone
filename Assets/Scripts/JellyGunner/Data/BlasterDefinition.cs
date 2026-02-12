using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    [CreateAssetMenu(fileName = "BlasterDef", menuName = "JellyGunner/Blaster Definition")]
    public class BlasterDefinition : ScriptableObject
    {
        [Title("Identity")]
        public BlasterType type;
        [Required] public Mesh mesh;

        [Title("Stats"), ReadOnly]
        public int MaxAmmo => TierConfig.GetAmmo(type);

        [ReadOnly]
        public float ShotInterval => TierConfig.GetShotInterval(type);

        [Title("Visual")]
        [Range(0.3f, 2f)] public float modelScale = 1f;
        [Range(0f, 15f)] public float recoilAngle = 5f;
    }
}
