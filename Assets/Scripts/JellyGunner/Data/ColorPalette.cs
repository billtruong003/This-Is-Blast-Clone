using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "JellyGunner/Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        [Title("Block Colors")]
        public Color red = new Color(0.95f, 0.25f, 0.3f, 1f);
        public Color blue = new Color(0.2f, 0.5f, 0.95f, 1f);
        public Color green = new Color(0.25f, 0.9f, 0.4f, 1f);
        public Color yellow = new Color(1f, 0.85f, 0.2f, 1f);

        [Title("Effect Colors")]
        public Color hammerHighlight = new Color(1f, 1f, 1f, 0.6f);
        public Color mergeFlash = new Color(1f, 0.95f, 0.7f, 1f);
        public Color deadlockWarning = new Color(1f, 0.1f, 0.1f, 0.8f);
        public Color nearDeadlockTint = new Color(1f, 0.6f, 0.2f, 0.5f);

        public Color GetColor(BlockColor blockColor) => blockColor switch
        {
            BlockColor.Red => red,
            BlockColor.Blue => blue,
            BlockColor.Green => green,
            BlockColor.Yellow => yellow,
            _ => Color.white
        };

        public Vector4 GetColorVector(BlockColor blockColor) => (Vector4)GetColor(blockColor);

        public Vector4 GetHighlightedColorVector(BlockColor blockColor)
        {
            var baseColor = GetColor(blockColor);
            return (Vector4)Color.Lerp(baseColor, hammerHighlight, 0.4f);
        }
    }
}
