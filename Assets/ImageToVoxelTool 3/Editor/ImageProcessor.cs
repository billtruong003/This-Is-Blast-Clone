using UnityEngine;

namespace ImageToVoxel
{
    public static class ImageProcessor
    {
        private const float LuminanceR = 0.2126f;
        private const float LuminanceG = 0.7152f;
        private const float LuminanceB = 0.0722f;

        public static int[,] Process(Texture2D source, RectInt cropPixelRect, int outputWidth, int outputHeight, int totalRanges)
        {
            var pixels = SampleCroppedRegion(source, cropPixelRect, outputWidth, outputHeight);
            var result = new int[outputWidth, outputHeight];

            for (int y = 0; y < outputHeight; y++)
                for (int x = 0; x < outputWidth; x++)
                    result[x, y] = Quantize(ComputeLuminance(pixels[y * outputWidth + x]), totalRanges);

            return result;
        }

        public static RectInt ComputeCropPixelRect(Texture2D source, Rect normalizedCrop)
        {
            int x = Mathf.RoundToInt(normalizedCrop.x * source.width);
            int y = Mathf.RoundToInt(normalizedCrop.y * source.height);
            int w = Mathf.RoundToInt(normalizedCrop.width * source.width);
            int h = Mathf.RoundToInt(normalizedCrop.height * source.height);

            x = Mathf.Clamp(x, 0, source.width - 1);
            y = Mathf.Clamp(y, 0, source.height - 1);
            w = Mathf.Clamp(w, 1, source.width - x);
            h = Mathf.Clamp(h, 1, source.height - y);

            return new RectInt(x, y, w, h);
        }

        public static bool IsReadable(Texture2D texture)
        {
            try
            {
                texture.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Color[] SampleCroppedRegion(Texture2D source, RectInt cropRect, int targetWidth, int targetHeight)
        {
            var result = TryGpuCropAndResize(source, cropRect, targetWidth, targetHeight);
            if (result != null)
                return result;

            return CpuCropAndResize(source, cropRect, targetWidth, targetHeight);
        }

        private static Color[] TryGpuCropAndResize(Texture2D source, RectInt cropRect, int targetWidth, int targetHeight)
        {
            RenderTexture outputRT = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                var scale = new Vector2(
                    (float)cropRect.width / source.width,
                    (float)cropRect.height / source.height
                );
                var offset = new Vector2(
                    (float)cropRect.x / source.width,
                    (float)cropRect.y / source.height
                );

                outputRT = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
                outputRT.filterMode = FilterMode.Bilinear;

                Graphics.Blit(source, outputRT, scale, offset);

                RenderTexture.active = outputRT;
                var readback = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
                readback.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                readback.Apply();

                var pixels = readback.GetPixels();
                Object.DestroyImmediate(readback);

                if (IsAllBlack(pixels))
                    return null;

                return pixels;
            }
            catch
            {
                return null;
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (outputRT != null)
                    RenderTexture.ReleaseTemporary(outputRT);
            }
        }

        private static Color[] CpuCropAndResize(Texture2D source, RectInt cropRect, int targetWidth, int targetHeight)
        {
            var pixels = new Color[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    float u = targetWidth > 1 ? (float)x / (targetWidth - 1) : 0.5f;
                    float v = targetHeight > 1 ? (float)y / (targetHeight - 1) : 0.5f;

                    float srcX = cropRect.x + u * (cropRect.width - 1);
                    float srcY = cropRect.y + v * (cropRect.height - 1);

                    pixels[y * targetWidth + x] = SampleBilinear(source, srcX, srcY);
                }
            }

            return pixels;
        }

        private static Color SampleBilinear(Texture2D source, float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, source.width - 1);
            int y1 = Mathf.Min(y0 + 1, source.height - 1);
            x0 = Mathf.Clamp(x0, 0, source.width - 1);
            y0 = Mathf.Clamp(y0, 0, source.height - 1);

            float fx = x - Mathf.FloorToInt(x);
            float fy = y - Mathf.FloorToInt(y);

            Color c00 = source.GetPixel(x0, y0);
            Color c10 = source.GetPixel(x1, y0);
            Color c01 = source.GetPixel(x0, y1);
            Color c11 = source.GetPixel(x1, y1);

            Color bottom = Color.Lerp(c00, c10, fx);
            Color top = Color.Lerp(c01, c11, fx);
            return Color.Lerp(bottom, top, fy);
        }

        private static bool IsAllBlack(Color[] pixels)
        {
            int sampleCount = Mathf.Min(pixels.Length, 64);
            int step = Mathf.Max(1, pixels.Length / sampleCount);

            for (int i = 0; i < pixels.Length; i += step)
            {
                if (pixels[i].r > 0.001f || pixels[i].g > 0.001f || pixels[i].b > 0.001f)
                    return false;
            }

            return true;
        }

        private static float ComputeLuminance(Color pixel)
        {
            return pixel.r * LuminanceR + pixel.g * LuminanceG + pixel.b * LuminanceB;
        }

        private static int Quantize(float brightness, int totalRanges)
        {
            int index = Mathf.FloorToInt(brightness * totalRanges);
            return Mathf.Clamp(index, 0, totalRanges - 1);
        }
    }
}
