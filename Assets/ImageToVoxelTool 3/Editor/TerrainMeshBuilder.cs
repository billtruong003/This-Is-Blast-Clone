using UnityEngine;
using UnityEditor;

namespace ImageToVoxel
{
    public static class TerrainMeshBuilder
    {
        public static Mesh Build(int[,] data, int totalRanges, float cellSize, float maxDepth, bool invertDepth)
        {
            int width = data.GetLength(0);
            int height = data.GetLength(1);
            int vertexCount = (width + 1) * (height + 1);

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var colors = new Color[vertexCount];
            var triangles = new int[width * height * 6];

            for (int y = 0; y <= height; y++)
            {
                for (int x = 0; x <= width; x++)
                {
                    int vertIndex = y * (width + 1) + x;

                    float sampleX = Mathf.Clamp(x - 1, 0, width - 1);
                    float sampleY = Mathf.Clamp(y - 1, 0, height - 1);
                    float heightValue = SampleSmooth(data, x, y, width, height, totalRanges);

                    if (invertDepth)
                        heightValue = 1f - heightValue;

                    float yPos = heightValue * maxDepth;

                    vertices[vertIndex] = new Vector3(x * cellSize, yPos, y * cellSize);
                    uvs[vertIndex] = new Vector2((float)x / width, (float)y / height);
                    colors[vertIndex] = Color.Lerp(Color.black, Color.white, heightValue);
                }
            }

            int triIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int bl = y * (width + 1) + x;
                    int br = bl + 1;
                    int tl = bl + (width + 1);
                    int tr = tl + 1;

                    triangles[triIndex++] = bl;
                    triangles[triIndex++] = tl;
                    triangles[triIndex++] = tr;
                    triangles[triIndex++] = bl;
                    triangles[triIndex++] = tr;
                    triangles[triIndex++] = br;
                }
            }

            var mesh = new Mesh
            {
                name = "TerrainMesh",
                indexFormat = vertexCount > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16,
                vertices = vertices,
                triangles = triangles,
                uv = uvs,
                colors = colors
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        public static GameObject CreateTerrainObject(Mesh mesh, Material material)
        {
            var terrainObj = new GameObject("__TerrainPreview__")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var meshFilter = terrainObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = terrainObj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true;

            return terrainObj;
        }

        public static Mesh SaveMeshAsset(Mesh mesh, string defaultName = "TerrainMesh")
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Terrain Mesh",
                defaultName,
                "asset",
                "Choose a location to save the terrain mesh."
            );

            if (string.IsNullOrEmpty(path))
                return mesh;

            var assetMesh = Object.Instantiate(mesh);
            assetMesh.name = System.IO.Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(assetMesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(assetMesh);
            return assetMesh;
        }

        private static float SampleSmooth(int[,] data, int vx, int vy, int dataW, int dataH, int totalRanges)
        {
            float total = 0;
            int count = 0;

            for (int dy = -1; dy <= 0; dy++)
            {
                for (int dx = -1; dx <= 0; dx++)
                {
                    int sx = Mathf.Clamp(vx + dx, 0, dataW - 1);
                    int sy = Mathf.Clamp(vy + dy, 0, dataH - 1);

                    float normalized = totalRanges > 1
                        ? data[sx, sy] / (float)(totalRanges - 1)
                        : 0;

                    total += normalized;
                    count++;
                }
            }

            return count > 0 ? total / count : 0;
        }
    }
}
