using UnityEngine;
using UnityEditor;

namespace ImageToVoxel
{
    public class CropSceneHandler
    {
        private const float PreviewMaxSize = 20f;
        private const float HandleRadius = 0.3f;
        private const float DimAlpha = 0.6f;

        private Rect normalizedCrop = new Rect(0, 0, 1, 1);
        private Texture2D sourceImage;
        private Material previewMaterial;

        private float previewWidth;
        private float previewHeight;
        private Vector3 previewOrigin = Vector3.zero;

        private bool aspectLocked;
        private float lockedAspectRatio = 1f;

        private enum DragTarget { None, Center, TopLeft, TopRight, BottomLeft, BottomRight }
        private DragTarget activeDrag = DragTarget.None;
        private Vector3 dragStartMouseWorld;
        private Rect dragStartCrop;

        public Rect NormalizedCrop => normalizedCrop;
        public bool HasImage => sourceImage != null;
        public bool IsDragging => activeDrag != DragTarget.None;

        public RectInt GetPixelCrop()
        {
            if (sourceImage == null)
                return new RectInt(0, 0, 1, 1);
            return ImageProcessor.ComputeCropPixelRect(sourceImage, normalizedCrop);
        }

        public void SetImage(Texture2D image)
        {
            sourceImage = image;
            normalizedCrop = new Rect(0, 0, 1, 1);

            if (image != null)
            {
                float aspect = (float)image.width / image.height;
                if (aspect > 1f)
                {
                    previewWidth = PreviewMaxSize;
                    previewHeight = PreviewMaxSize / aspect;
                }
                else
                {
                    previewHeight = PreviewMaxSize;
                    previewWidth = PreviewMaxSize * aspect;
                }
            }

            DestroyPreviewMaterial();
        }

        public void SetAspectRatio(float ratio, bool locked)
        {
            aspectLocked = locked;
            lockedAspectRatio = ratio;

            if (locked && sourceImage != null)
                EnforceAspectRatio();
        }

        public void ResetCrop()
        {
            normalizedCrop = new Rect(0, 0, 1, 1);
            if (aspectLocked)
                EnforceAspectRatio();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (sourceImage == null) return;

            EnsurePreviewMaterial();
            DrawImagePreview();
            DrawDimOverlay();
            DrawCropBorder();
            DrawHandles();
            HandleInput();
        }

        public void Dispose()
        {
            DestroyPreviewMaterial();
        }

        private void EnsurePreviewMaterial()
        {
            if (previewMaterial != null && previewMaterial.mainTexture == sourceImage) return;

            DestroyPreviewMaterial();
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Texture");
            previewMaterial = new Material(shader)
            {
                mainTexture = sourceImage
            };
        }

        private void DrawImagePreview()
        {
            Handles.color = new Color(1, 1, 1, 0.8f);

            var p0 = previewOrigin;
            var p1 = previewOrigin + new Vector3(previewWidth, 0, 0);
            var p2 = previewOrigin + new Vector3(previewWidth, 0, previewHeight);
            var p3 = previewOrigin + new Vector3(0, 0, previewHeight);

            Handles.DrawSolidRectangleWithOutline(new[] { p0, p1, p2, p3 }, new Color(0.5f, 0.5f, 0.5f, 0.3f), Color.clear);

            if (previewMaterial != null && Event.current.type == EventType.Repaint)
            {
                previewMaterial.SetPass(0);
                GL.PushMatrix();
                GL.Begin(GL.QUADS);
                GL.TexCoord2(0, 0); GL.Vertex(p0 + Vector3.up * 0.001f);
                GL.TexCoord2(1, 0); GL.Vertex(p1 + Vector3.up * 0.001f);
                GL.TexCoord2(1, 1); GL.Vertex(p2 + Vector3.up * 0.001f);
                GL.TexCoord2(0, 1); GL.Vertex(p3 + Vector3.up * 0.001f);
                GL.End();
                GL.PopMatrix();
            }
        }

        private void DrawDimOverlay()
        {
            var dimColor = new Color(0, 0, 0, DimAlpha);
            float yOffset = 0.002f;

            float cx = normalizedCrop.x * previewWidth + previewOrigin.x;
            float cz = normalizedCrop.y * previewHeight + previewOrigin.z;
            float cw = normalizedCrop.width * previewWidth;
            float ch = normalizedCrop.height * previewHeight;

            float left = previewOrigin.x;
            float right = previewOrigin.x + previewWidth;
            float bottom = previewOrigin.z;
            float top = previewOrigin.z + previewHeight;

            DrawFilledRect(left, bottom, cx - left, top - bottom, yOffset, dimColor);
            DrawFilledRect(cx + cw, bottom, right - (cx + cw), top - bottom, yOffset, dimColor);
            DrawFilledRect(cx, bottom, cw, cz - bottom, yOffset, dimColor);
            DrawFilledRect(cx, cz + ch, cw, top - (cz + ch), yOffset, dimColor);
        }

        private void DrawFilledRect(float x, float z, float w, float h, float y, Color color)
        {
            if (w <= 0 || h <= 0) return;

            var verts = new[]
            {
                new Vector3(x, y, z),
                new Vector3(x + w, y, z),
                new Vector3(x + w, y, z + h),
                new Vector3(x, y, z + h)
            };
            Handles.DrawSolidRectangleWithOutline(verts, color, Color.clear);
        }

        private void DrawCropBorder()
        {
            var corners = GetCropWorldCorners(0.003f);
            Handles.color = new Color(1f, 0.85f, 0.2f, 1f);
            Handles.DrawLine(corners[0], corners[1], 2f);
            Handles.DrawLine(corners[1], corners[2], 2f);
            Handles.DrawLine(corners[2], corners[3], 2f);
            Handles.DrawLine(corners[3], corners[0], 2f);

            float thirdW = (corners[1].x - corners[0].x) / 3f;
            float thirdH = (corners[3].z - corners[0].z) / 3f;

            Handles.color = new Color(1f, 0.85f, 0.2f, 0.3f);
            for (int i = 1; i <= 2; i++)
            {
                Handles.DrawLine(
                    corners[0] + new Vector3(thirdW * i, 0, 0),
                    corners[3] + new Vector3(thirdW * i, 0, 0));
                Handles.DrawLine(
                    corners[0] + new Vector3(0, 0, thirdH * i),
                    corners[1] + new Vector3(0, 0, thirdH * i));
            }

            string ratioText = ComputeDisplayAspectRatio();
            var center = (corners[0] + corners[2]) * 0.5f;
            Handles.Label(center + Vector3.up * 0.5f, ratioText, new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.2f) },
                fontSize = 14,
                fontStyle = FontStyle.Bold
            });
        }

        private void DrawHandles()
        {
            var corners = GetCropWorldCorners(0.005f);

            Handles.color = new Color(1f, 0.85f, 0.2f, 1f);
            for (int i = 0; i < 4; i++)
            {
                float size = HandleUtility.GetHandleSize(corners[i]) * 0.06f;
                Handles.DotHandleCap(0, corners[i], Quaternion.identity, size, EventType.Repaint);
            }

            var centerPos = (corners[0] + corners[2]) * 0.5f;
            float centerSize = HandleUtility.GetHandleSize(centerPos) * 0.08f;
            Handles.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Handles.DotHandleCap(0, centerPos, Quaternion.identity, centerSize, EventType.Repaint);
        }

        private void HandleInput()
        {
            var evt = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            switch (evt.type)
            {
                case EventType.MouseDown when evt.button == 0:
                    var target = FindDragTarget(evt.mousePosition);
                    if (target != DragTarget.None)
                    {
                        activeDrag = target;
                        dragStartMouseWorld = GetMouseWorldXZ(evt.mousePosition);
                        dragStartCrop = normalizedCrop;
                        GUIUtility.hotControl = controlId;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag when activeDrag != DragTarget.None && evt.button == 0:
                    var currentWorld = GetMouseWorldXZ(evt.mousePosition);
                    var delta = currentWorld - dragStartMouseWorld;
                    ApplyDrag(delta);
                    evt.Use();
                    SceneView.RepaintAll();
                    break;

                case EventType.MouseUp when activeDrag != DragTarget.None && evt.button == 0:
                    activeDrag = DragTarget.None;
                    GUIUtility.hotControl = 0;
                    evt.Use();
                    break;
            }
        }

        private DragTarget FindDragTarget(Vector2 mousePos)
        {
            var corners = GetCropWorldCorners(0.005f);
            float threshold = HandleUtility.GetHandleSize(corners[0]) * 0.15f;

            var mouseWorld = GetMouseWorldXZ(mousePos);

            if (Vector3.Distance(mouseWorld, corners[0]) < threshold) return DragTarget.BottomLeft;
            if (Vector3.Distance(mouseWorld, corners[1]) < threshold) return DragTarget.BottomRight;
            if (Vector3.Distance(mouseWorld, corners[2]) < threshold) return DragTarget.TopRight;
            if (Vector3.Distance(mouseWorld, corners[3]) < threshold) return DragTarget.TopLeft;

            float cx = normalizedCrop.x * previewWidth + previewOrigin.x;
            float cz = normalizedCrop.y * previewHeight + previewOrigin.z;
            float cw = normalizedCrop.width * previewWidth;
            float ch = normalizedCrop.height * previewHeight;

            if (mouseWorld.x >= cx && mouseWorld.x <= cx + cw &&
                mouseWorld.z >= cz && mouseWorld.z <= cz + ch)
                return DragTarget.Center;

            return DragTarget.None;
        }

        private void ApplyDrag(Vector3 worldDelta)
        {
            float dx = worldDelta.x / previewWidth;
            float dz = worldDelta.z / previewHeight;

            switch (activeDrag)
            {
                case DragTarget.Center:
                    ApplyMoveDrag(dx, dz);
                    break;
                case DragTarget.BottomLeft:
                    ApplyCornerDrag(dx, dz, anchorRight: true, anchorTop: true);
                    break;
                case DragTarget.BottomRight:
                    ApplyCornerDrag(dx, dz, anchorRight: false, anchorTop: true);
                    break;
                case DragTarget.TopRight:
                    ApplyCornerDrag(dx, dz, anchorRight: false, anchorTop: false);
                    break;
                case DragTarget.TopLeft:
                    ApplyCornerDrag(dx, dz, anchorRight: true, anchorTop: false);
                    break;
            }
        }

        private void ApplyMoveDrag(float dx, float dz)
        {
            float newX = Mathf.Clamp(dragStartCrop.x + dx, 0, 1 - dragStartCrop.width);
            float newY = Mathf.Clamp(dragStartCrop.y + dz, 0, 1 - dragStartCrop.height);
            normalizedCrop = new Rect(newX, newY, dragStartCrop.width, dragStartCrop.height);
        }

        private void ApplyCornerDrag(float dx, float dz, bool anchorRight, bool anchorTop)
        {
            float anchorX = anchorRight ? dragStartCrop.xMax : dragStartCrop.x;
            float anchorZ = anchorTop ? dragStartCrop.yMax : dragStartCrop.y;

            float movingX = anchorRight ? dragStartCrop.x + dx : dragStartCrop.xMax + dx;
            float movingZ = anchorTop ? dragStartCrop.y + dz : dragStartCrop.yMax + dz;

            movingX = Mathf.Clamp(movingX, 0f, 1f);
            movingZ = Mathf.Clamp(movingZ, 0f, 1f);

            float minX = Mathf.Min(anchorX, movingX);
            float maxX = Mathf.Max(anchorX, movingX);
            float minZ = Mathf.Min(anchorZ, movingZ);
            float maxZ = Mathf.Max(anchorZ, movingZ);

            float newW = Mathf.Max(maxX - minX, 0.02f);
            float newH = Mathf.Max(maxZ - minZ, 0.02f);

            if (aspectLocked && sourceImage != null)
            {
                float imageAspect = (float)sourceImage.width / sourceImage.height;
                float cropAspect = (newW * imageAspect) / (newH);
                float targetRatio = lockedAspectRatio;

                if (cropAspect > targetRatio)
                    newW = newH * targetRatio / imageAspect;
                else
                    newH = newW * imageAspect / targetRatio;

                if (anchorRight)
                    minX = anchorX - newW;
                if (anchorTop)
                    minZ = anchorZ - newH;
            }

            minX = Mathf.Clamp(minX, 0f, 1f - newW);
            minZ = Mathf.Clamp(minZ, 0f, 1f - newH);

            normalizedCrop = new Rect(minX, minZ, newW, newH);
        }

        private void EnforceAspectRatio()
        {
            if (sourceImage == null) return;

            float imageAspect = (float)sourceImage.width / sourceImage.height;
            float currentCropAspect = (normalizedCrop.width * imageAspect) / normalizedCrop.height;

            float w = normalizedCrop.width;
            float h = normalizedCrop.height;

            if (currentCropAspect > lockedAspectRatio)
                w = h * lockedAspectRatio / imageAspect;
            else
                h = w * imageAspect / lockedAspectRatio;

            float cx = normalizedCrop.x + normalizedCrop.width * 0.5f;
            float cy = normalizedCrop.y + normalizedCrop.height * 0.5f;

            float x = Mathf.Clamp(cx - w * 0.5f, 0, 1 - w);
            float y = Mathf.Clamp(cy - h * 0.5f, 0, 1 - h);

            normalizedCrop = new Rect(x, y, w, h);
        }

        private Vector3[] GetCropWorldCorners(float yOffset)
        {
            float x = normalizedCrop.x * previewWidth + previewOrigin.x;
            float z = normalizedCrop.y * previewHeight + previewOrigin.z;
            float w = normalizedCrop.width * previewWidth;
            float h = normalizedCrop.height * previewHeight;

            return new[]
            {
                new Vector3(x, yOffset, z),
                new Vector3(x + w, yOffset, z),
                new Vector3(x + w, yOffset, z + h),
                new Vector3(x, yOffset, z + h)
            };
        }

        private Vector3 GetMouseWorldXZ(Vector2 mousePos)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (Mathf.Abs(ray.direction.y) < 0.001f)
                return Vector3.zero;

            float t = -ray.origin.y / ray.direction.y;
            return ray.origin + ray.direction * t;
        }

        private string ComputeDisplayAspectRatio()
        {
            if (sourceImage == null) return "";

            float pixelW = normalizedCrop.width * sourceImage.width;
            float pixelH = normalizedCrop.height * sourceImage.height;

            if (pixelH < 1) return "N/A";

            float ratio = pixelW / pixelH;

            (int, int)[] commonRatios = { (1, 1), (4, 3), (3, 2), (16, 9), (16, 10), (21, 9), (3, 4), (2, 3), (9, 16) };
            foreach (var (a, b) in commonRatios)
            {
                if (Mathf.Abs(ratio - (float)a / b) < 0.05f)
                    return $"{a}:{b}";
            }

            return $"{ratio:F2}:1";
        }

        private void DestroyPreviewMaterial()
        {
            if (previewMaterial != null)
            {
                Object.DestroyImmediate(previewMaterial);
                previewMaterial = null;
            }
        }
    }
}
