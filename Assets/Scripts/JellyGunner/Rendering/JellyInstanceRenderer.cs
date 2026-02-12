using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    [DefaultExecutionOrder(-50)]
    public class JellyInstanceRenderer : MonoBehaviour
    {
        [Serializable]
        public class RenderBatch
        {
            [ReadOnly] public string name;
            [Required] public Mesh mesh;
            [Required] public Material material;
            [ReadOnly] public float boundRadius;

            [HideInInspector] public JellyInstanceData[] cpuData;
            [HideInInspector] public ComputeBuffer sourceBuffer;
            [HideInInspector] public ComputeBuffer visibleBuffer;
            [HideInInspector] public ComputeBuffer argsBuffer;
            [HideInInspector] public MaterialPropertyBlock props;
            [HideInInspector] public int capacity;
            [HideInInspector] public int activeCount;
            [HideInInspector] public bool isDirty;

            public void Allocate(int maxCount)
            {
                Release();
                capacity = maxCount;
                activeCount = 0;
                cpuData = new JellyInstanceData[maxCount];

                sourceBuffer = new ComputeBuffer(maxCount, JellyInstanceData.Stride);
                visibleBuffer = new ComputeBuffer(maxCount, JellyInstanceData.Stride, ComputeBufferType.Append);
                argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

                uint[] args =
                {
                    mesh.GetIndexCount(0), 0,
                    mesh.GetIndexStart(0), mesh.GetBaseVertex(0), 0
                };
                argsBuffer.SetData(args);

                props = new MaterialPropertyBlock();
                props.SetBuffer(ShaderProps.VisibleBuffer, visibleBuffer);
                isDirty = true;
            }

            public void UploadToGPU()
            {
                if (!isDirty || activeCount == 0) return;
                sourceBuffer.SetData(cpuData, 0, 0, activeCount);
                isDirty = false;
            }

            public void Release()
            {
                sourceBuffer?.Release();
                visibleBuffer?.Release();
                argsBuffer?.Release();
                sourceBuffer = null;
                visibleBuffer = null;
                argsBuffer = null;
                cpuData = null;
            }
        }

        [Title("GPU Pipeline")]
        [SerializeField, Required] private ComputeShader _cullingShader;
        [SerializeField, Required] private GameConfig _config;

        private Camera _mainCamera;
        private int _kernelID;
        private readonly Plane[] _cameraPlanes = new Plane[6];
        private readonly Vector4[] _frustumPlanesV4 = new Vector4[6];
        private Bounds _worldBounds;
        private bool _initialized;

        private RenderBatch _enemyBatch;
        private RenderBatch _projectileBatch;

        public RenderBatch EnemyBatch => _enemyBatch;
        public RenderBatch ProjectileBatch => _projectileBatch;

        private static class ShaderProps
        {
            public static readonly int VisibleBuffer = Shader.PropertyToID("_VisibleBuffer");
            public static readonly int SourceBuffer = Shader.PropertyToID("_SourceBuffer");
            public static readonly int CameraPlanes = Shader.PropertyToID("_CameraPlanes");
            public static readonly int CameraPosition = Shader.PropertyToID("_CameraPosition");
            public static readonly int MaxDistanceSq = Shader.PropertyToID("_MaxDistanceSq");
            public static readonly int Count = Shader.PropertyToID("_Count");
            public static readonly int BoundRadius = Shader.PropertyToID("_BoundRadius");
        }

        public void Initialize(Mesh enemyMesh, Material enemyMat, int maxEnemies,
                               Mesh projectileMesh, Material projectileMat, int maxProjectiles)
        {
            _mainCamera = Camera.main;
            _kernelID = _cullingShader.FindKernel("CSMain");
            _worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

            _enemyBatch = new RenderBatch
            {
                name = "Enemies",
                mesh = enemyMesh,
                material = enemyMat,
                boundRadius = enemyMesh.bounds.extents.magnitude
            };
            _enemyBatch.Allocate(maxEnemies);

            _projectileBatch = new RenderBatch
            {
                name = "Projectiles",
                mesh = projectileMesh,
                material = projectileMat,
                boundRadius = projectileMesh.bounds.extents.magnitude
            };
            _projectileBatch.Allocate(maxProjectiles);

            _initialized = true;
        }

        public void MarkDirty(RenderBatch batch) => batch.isDirty = true;

        public void Render()
        {
            if (!_initialized) return;
            if (!_mainCamera)
            {
                _mainCamera = Camera.main;
                if (!_mainCamera) return;
            }

            UpdateFrustumPlanes();
            SetGlobalCullingUniforms();
            DispatchAndDraw(_enemyBatch);
            DispatchAndDraw(_projectileBatch);
        }

        private void UpdateFrustumPlanes()
        {
            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraPlanes);
            for (int i = 0; i < 6; i++)
            {
                var n = _cameraPlanes[i].normal;
                _frustumPlanesV4[i] = new Vector4(n.x, n.y, n.z, _cameraPlanes[i].distance);
            }
        }

        private void SetGlobalCullingUniforms()
        {
            _cullingShader.SetVectorArray(ShaderProps.CameraPlanes, _frustumPlanesV4);
            _cullingShader.SetVector(ShaderProps.CameraPosition, _mainCamera.transform.position);
            _cullingShader.SetFloat(ShaderProps.MaxDistanceSq, _config.cullDistance * _config.cullDistance);
        }

        private void DispatchAndDraw(RenderBatch batch)
        {
            if (batch?.sourceBuffer == null || batch.activeCount == 0) return;

            batch.UploadToGPU();
            batch.visibleBuffer.SetCounterValue(0);

            _cullingShader.SetInt(ShaderProps.Count, batch.activeCount);
            _cullingShader.SetFloat(ShaderProps.BoundRadius, batch.boundRadius + _config.boundsPadding);
            _cullingShader.SetBuffer(_kernelID, ShaderProps.SourceBuffer, batch.sourceBuffer);
            _cullingShader.SetBuffer(_kernelID, ShaderProps.VisibleBuffer, batch.visibleBuffer);

            _cullingShader.Dispatch(_kernelID, Mathf.CeilToInt(batch.activeCount / 256f), 1, 1);
            ComputeBuffer.CopyCount(batch.visibleBuffer, batch.argsBuffer, sizeof(uint));

            Graphics.DrawMeshInstancedIndirect(
                batch.mesh, 0, batch.material,
                _worldBounds, batch.argsBuffer, 0, batch.props
            );
        }

        private void OnDestroy()
        {
            _enemyBatch?.Release();
            _projectileBatch?.Release();
            _initialized = false;
        }
    }
}
