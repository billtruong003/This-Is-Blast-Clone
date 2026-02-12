using UnityEngine;

namespace JellyGunner
{
    public class Blaster : MonoBehaviour
    {
        private BlasterDefinition _definition;
        private BlockColor _color;
        private BlasterState _state;
        private int _currentAmmo;
        private float _shotTimer;
        private int _targetIndex = -1;
        private int _slotIndex = -1;
        private EnemyGridManager _grid;

        private Vector3 _flyStart;
        private Vector3 _flyEnd;
        private float _flyProgress;
        private float _flyDuration;

        private Transform _legLeft;
        private Transform _legRight;
        private float _runTimer;
        private Vector3 _runDirection;

        public BlasterState State => _state;
        public BlockColor Color => _color;
        public int CurrentAmmo => _currentAmmo;
        public bool IsEmpty => _currentAmmo <= 0;
        public BlasterDefinition Definition => _definition;
        public int SlotIndex { get => _slotIndex; set => _slotIndex = value; }

        public void Setup(BlasterDefinition def, BlockColor color, int ammo, EnemyGridManager grid)
        {
            _definition = def;
            _color = color;
            _currentAmmo = ammo;
            _grid = grid;
            _state = BlasterState.InSupply;
        }

        public void FlyToSlot(Vector3 slotPosition, float duration)
        {
            _state = BlasterState.FlyingToTray;
            _flyStart = transform.position;
            _flyEnd = slotPosition;
            _flyProgress = 0f;
            _flyDuration = duration;
        }

        public void ApplyMergeAmmo(int totalAmmo)
        {
            _currentAmmo = totalAmmo;
        }

        public void BeginMergeInto(Vector3 targetPosition, float duration)
        {
            _state = BlasterState.MergingIn;
            _flyStart = transform.position;
            _flyEnd = targetPosition;
            _flyProgress = 0f;
            _flyDuration = duration;
        }

        private void Update()
        {
            switch (_state)
            {
                case BlasterState.FlyingToTray:
                    UpdateFly(true);
                    break;
                case BlasterState.MergingIn:
                    UpdateMergeFly();
                    break;
                case BlasterState.Active:
                    UpdateActive();
                    break;
                case BlasterState.RunningAway:
                    UpdateRunAway();
                    break;
            }
        }

        private void UpdateFly(bool activateOnComplete)
        {
            _flyProgress += Time.deltaTime / _flyDuration;
            transform.position = Vector3.LerpUnclamped(_flyStart, _flyEnd, EaseOutBack(_flyProgress));

            if (_flyProgress < 1f) return;

            transform.position = _flyEnd;

            if (activateOnComplete)
            {
                _state = BlasterState.Active;
                _shotTimer = 0f;
                AcquireTarget();
            }
        }

        private void UpdateMergeFly()
        {
            _flyProgress += Time.deltaTime / _flyDuration;

            float t = Mathf.Clamp01(_flyProgress);
            Vector3 mid = (_flyStart + _flyEnd) * 0.5f + Vector3.up * 1.5f;
            Vector3 a = Vector3.Lerp(_flyStart, mid, t);
            Vector3 b = Vector3.Lerp(mid, _flyEnd, t);
            transform.position = Vector3.Lerp(a, b, t);

            float shrink = 1f - t;
            transform.localScale = Vector3.one * (_definition.modelScale * shrink);

            if (_flyProgress >= 1f)
                Destroy(gameObject);
        }

        private void UpdateActive()
        {
            if (_currentAmmo <= 0)
            {
                BeginRunAway();
                return;
            }

            if (_targetIndex < 0 || !_grid.IsAlive(_targetIndex))
                AcquireTarget();

            if (_targetIndex < 0) return;

            RotateTowardsTarget();

            _shotTimer -= Time.deltaTime;
            if (_shotTimer <= 0f)
            {
                Fire();
                _shotTimer = _definition.ShotInterval;
            }
        }

        private void AcquireTarget()
        {
            _targetIndex = _grid.FindBottomMostByColor(_color, transform.position);
        }

        private void RotateTowardsTarget()
        {
            Vector3 dir = _grid.GetPosition(_targetIndex) - transform.position;
            if (dir.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 12f);
        }

        private void Fire()
        {
            _currentAmmo--;

            bool killed = _grid.TryDamage(_targetIndex, 1);

            transform.Rotate(-_definition.recoilAngle, 0f, 0f, Space.Self);

            if (killed)
                _targetIndex = -1;
        }

        private void BeginRunAway()
        {
            _state = BlasterState.RunningAway;
            _runTimer = 0f;
            _runDirection = (Random.value > 0.5f ? Vector3.right : Vector3.left) * 8f;

            SpawnCartoonLegs();

            GameEvents.Publish(new GameEvents.BlasterDepleted { SlotIndex = _slotIndex });
        }

        private void SpawnCartoonLegs()
        {
            _legLeft = CreateLeg(new Vector3(-0.2f, -0.4f, 0f));
            _legRight = CreateLeg(new Vector3(0.2f, -0.4f, 0f));
        }

        private Transform CreateLeg(Vector3 localPos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(0.12f, 0.25f, 0.12f);

            if (go.TryGetComponent<Collider>(out var col))
                Destroy(col);

            if (go.TryGetComponent<Renderer>(out var rend))
                rend.material.color = UnityEngine.Color.white;

            return go.transform;
        }

        private void UpdateRunAway()
        {
            _runTimer += Time.deltaTime;
            transform.position += _runDirection * Time.deltaTime;
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.one * (_definition.modelScale * 0.6f),
                Time.deltaTime * 3f
            );

            if (_legLeft && _legRight)
            {
                float swing = Mathf.Sin(_runTimer * 20f) * 30f;
                _legLeft.localRotation = Quaternion.Euler(swing, 0f, 0f);
                _legRight.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            }

            if (_runTimer > 2f)
                Destroy(gameObject);
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
