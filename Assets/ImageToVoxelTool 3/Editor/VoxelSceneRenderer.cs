using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ImageToVoxel
{
    public class VoxelSceneRenderer
    {
        private const string ShaderName = "Hidden/ImageToVoxel/VoxelVertexColor";
        private const string FallbackShaderName = "Particles/Standard Unlit";

        private GameObject displayObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material material;

        private bool isDirty;
        private float visualScale = 1f;
        private float heightMultiplier = 0.5f;
        private bool showGrid = true;
        private int[,] currentData;
        private int currentRanges;

        public float VisualScale
        {
            get => visualScale;
            set
            {
                if (Mathf.Approximately(visualScale, value)) return;
                visualScale = value;
                isDirty = true;
                RebuildIfDirty();
            }
        }

        public float HeightMultiplier
        {
            get => heightMultiplier;
            set
            {
                if (Mathf.Approximately(heightMultiplier, value)) return;
                heightMultiplier = value;
                isDirty = true;
                RebuildIfDirty();
            }
        }

        public bool ShowGrid
        {
            get => showGrid;
            set => showGrid = value;
        }

        public bool HasData => currentData != null;

        public void SetData(int[,] data, int totalRanges)
        {
            currentData = data;
            currentRanges = totalRanges;
            isDirty = true;
            RebuildIfDirty();
        }

        public void Clear()
        {
            currentData = null;
            currentRanges = 0;
            DestroyDisplayObject();
        }

        public void DrawOverlay(SceneView sceneView)
        {
            if (!showGrid || currentData == null) return;

            int width = currentData.GetLength(0);
            int height = currentData.GetLength(1);
            float halfScale = visualScale * 0.5f;

            Handles.color = new Color(0.3f, 0.6f, 1f, 0.15f);

            for (int x = 0; x <= width; x++)
            {
                float xPos = x * visualScale - halfScale;
                Handles.DrawLine(
                    new Vector3(xPos, 0, -halfScale),
                    new Vector3(xPos, 0, height * visualScale - halfScale));
            }

            for (int y = 0; y <= height; y++)
            {
                float zPos = y * visualScale - halfScale;
                Handles.DrawLine(
                    new Vector3(-halfScale, 0, zPos),
                    new Vector3(width * visualScale - halfScale, 0, zPos));
            }
        }

        public void Dispose()
        {
            DestroyDisplayObject();
            DestroyMaterial();
        }

        private void RebuildIfDirty()
        {
            if (!isDirty || currentData == null) return;
            isDirty = false;

            EnsureDisplayObject();

            if (meshFilter.sharedMesh != null)
                Object.DestroyImmediate(meshFilter.sharedMesh);

            meshFilter.sharedMesh = BuildCombinedMesh();
        }

        private void EnsureDisplayObject()
        {
            if (displayObject != null) return;

            EnsureMaterial();

            displayObject = new GameObject("__VoxelPreview__")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            displayObject.transform.position = Vector3.zero;
            displayObject.transform.rotation = Quaternion.identity;
            displayObject.transform.localScale = Vector3.one;

            meshFilter = displayObject.AddComponent<MeshFilter>();
            meshRenderer = displayObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private Mesh BuildCombinedMesh()
        {
            int dataWidth = currentData.GetLength(0);
            int dataHeight = currentData.GetLength(1);
            int totalCubes = dataWidth * dataHeight;

            var vertices = new List<Vector3>(totalCubes * 24);
            var normals = new List<Vector3>(totalCubes * 24);
            var colors = new List<Color>(totalCubes * 24);
            var indices = new List<int>(totalCubes * 36);

            int vertexOffset = 0;

            for (int y = 0; y < dataHeight; y++)
            {
                for (int x = 0; x < dataWidth; x++)
                {
                    int rangeValue = currentData[x, y];
                    float normalizedBrightness = currentRanges > 1
                        ? rangeValue / (float)(currentRanges - 1)
                        : 1f;

                    float voxelHeight = Mathf.Max((rangeValue + 1) * heightMultiplier * visualScale, visualScale * 0.05f);
                    float halfH = voxelHeight * 0.5f;
                    float halfS = visualScale * 0.46f;

                    var center = new Vector3(x * visualScale, halfH, y * visualScale);
                    var color = Color.Lerp(new Color(0.05f, 0.05f, 0.08f), Color.white, normalizedBrightness);

                    AddCube(vertices, normals, colors, indices, center, halfS, halfH, color, vertexOffset);
                    vertexOffset += 24;
                }
            }

            var mesh = new Mesh
            {
                name = "VoxelCombined",
                indexFormat = totalCubes * 24 > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void AddCube(
            List<Vector3> verts, List<Vector3> norms, List<Color> cols, List<int> tris,
            Vector3 center, float halfW, float halfH, Color color, int baseIndex)
        {
            float l = center.x - halfW, r = center.x + halfW;
            float b = center.y - halfH, t = center.y + halfH;
            float bk = center.z - halfW, fr = center.z + halfW;

            var v = new Vector3[]
            {
                new Vector3(l, b, bk), new Vector3(r, b, bk), new Vector3(r, t, bk), new Vector3(l, t, bk),
                new Vector3(l, b, fr), new Vector3(r, b, fr), new Vector3(r, t, fr), new Vector3(l, t, fr),
                new Vector3(l, t, bk), new Vector3(r, t, bk), new Vector3(r, t, fr), new Vector3(l, t, fr),
                new Vector3(l, b, bk), new Vector3(r, b, bk), new Vector3(r, b, fr), new Vector3(l, b, fr),
                new Vector3(l, b, fr), new Vector3(l, b, bk), new Vector3(l, t, bk), new Vector3(l, t, fr),
                new Vector3(r, b, bk), new Vector3(r, b, fr), new Vector3(r, t, fr), new Vector3(r, t, bk),
            };

            var n = new Vector3[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            };

            for (int i = 0; i < 24; i++)
            {
                verts.Add(v[i]);
                norms.Add(n[i]);
                cols.Add(color);
            }

            int[] faceIndices = { 0,2,1, 0,3,2, 4,5,6, 4,6,7, 8,10,9, 8,11,10, 12,13,14, 12,14,15, 16,18,17, 16,19,18, 20,21,22, 20,22,23 };
            for (int i = 0; i < faceIndices.Length; i++)
                tris.Add(baseIndex + faceIndices[i]);
        }

        private void EnsureMaterial()
        {
            if (material != null) return;

            var shader = Shader.Find(ShaderName);

            if (shader == null || !shader.isSupported)
                shader = Shader.Find(FallbackShaderName);

            if (shader == null || !shader.isSupported)
                shader = Shader.Find("Sprites/Default");

            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void DestroyDisplayObject()
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
                Object.DestroyImmediate(meshFilter.sharedMesh);

            if (displayObject != null)
                Object.DestroyImmediate(displayObject);

            displayObject = null;
            meshFilter = null;
            meshRenderer = null;
        }

        private void DestroyMaterial()
        {
            if (material != null)
                Object.DestroyImmediate(material);
            material = null;
        }
    }
}
