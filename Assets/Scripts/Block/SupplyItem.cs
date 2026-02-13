using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;

[RequireComponent(typeof(SupplyVisual))]
public class SupplyItem : MonoBehaviour, IPoolable
{
    [Header("Animation Settings")]
    [SerializeField] private float _jumpHeight = 1.5f;
    [SerializeField] private float _jumpDuration = 0.4f;
    [SerializeField] private Vector3 _squashScale = new Vector3(1.2f, 0.8f, 1.2f);
    [SerializeField] private float _squashDuration = 0.1f;

    [Header("Combat Settings")]
    [SerializeField] private float _fireRate = 0.5f;

    public int ColumnIndex { get; private set; }
    public int RowIndex { get; private set; }
    public SupplyData Data { get; private set; }

    private SupplyVisual _visual;
    private Action<SupplyItem> _onClickCallback;
    private bool _isInTray = false;
    private float _nextFireTime;

    private void Awake()
    {
        _visual = GetComponent<SupplyVisual>();
    }

    private void Update()
    {
        if (_isInTray && Time.time >= _nextFireTime)
        {
            TryFire();
        }
    }

    public void Setup(SupplyData data, int col, int row, Action<SupplyItem> onClick)
    {
        Data = data;
        ColumnIndex = col;
        RowIndex = row;
        _onClickCallback = onClick;
        _isInTray = false;

        _visual.Initialize(data);
        _visual.SetStateInLayout();
    }

    public void OnClick()
    {
        _onClickCallback?.Invoke(this);
    }

    public void UpdateGridPosition(int newRow, Vector3 newWorldPos)
    {
        Vector3 startPos = transform.position;
        RowIndex = newRow;

        LeanTween.cancel(gameObject);

        LeanTween.value(gameObject, 0f, 1f, _jumpDuration)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnUpdate((float t) =>
            {
                Vector3 currentPos = Vector3.Lerp(startPos, newWorldPos, t);
                float linearY = Mathf.Lerp(startPos.y, newWorldPos.y, t);
                float arcY = _jumpHeight * 4 * t * (1 - t);
                currentPos.y = linearY + arcY;
                transform.position = currentPos;
            })
            .setOnComplete(() =>
            {
                transform.position = newWorldPos;
                LeanTween.scale(gameObject, _squashScale, _squashDuration)
                    .setLoopPingPong(1);
            });
    }

    public void MoveToTray(Vector3 targetPos, Action onComplete)
    {
        _visual.SetStateInTray();

        Vector3 startPos = transform.position;
        float highestY = Mathf.Max(startPos.y, targetPos.y);
        float peakY = highestY + _jumpHeight;

        Vector3 control1 = Vector3.Lerp(startPos, targetPos, 0.25f);
        control1.y = peakY;
        Vector3 control2 = Vector3.Lerp(startPos, targetPos, 0.75f);
        control2.y = peakY;

        LTBezierPath path = new LTBezierPath(new Vector3[] { startPos, control1, control2, targetPos });

        LeanTween.move(gameObject, path, _jumpDuration)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() => 
            {
                onComplete?.Invoke();
                _isInTray = true;
            });

        LeanTween.rotateAround(gameObject, Vector3.up, 360f, _jumpDuration);
    }

    private void TryFire()
    {
        if (Data.Amount <= 0) 
        {
            gameObject.Despawn();
            return;
        }

        if (BlockTargetManager.Instance == null) return;

        var target = BlockTargetManager.Instance.GetTarget(Data.ColorEnum);
        if (target != null)
        {
            FireProjectile(target);
            _nextFireTime = Time.time + _fireRate;
        }
    }

    private void FireProjectile(TargetBlock target)
    {
        target.TakeDamage(1);
        
        var newData = Data;
        newData.Amount--;
        Data = newData;

        _visual.Initialize(Data);
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        LeanTween.cancel(gameObject);
        _onClickCallback = null;
        _isInTray = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Debug Gizmos omitted for brevity
    }
#endif
}
