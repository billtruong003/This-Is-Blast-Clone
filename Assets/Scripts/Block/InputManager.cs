using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Define layers that interact with raycast (e.g. Default, Items)")]
    [SerializeField] private LayerMask _interactionMask = -1;

    private Camera _mainCamera;
    private readonly RaycastHit[] _hits = new RaycastHit[10];

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        // Prevent clicking through UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // Use QueryTriggerInteraction.Collide to ensure triggers are hit
        int hitCount = Physics.RaycastNonAlloc(ray, _hits, Mathf.Infinity, _interactionMask, QueryTriggerInteraction.Collide);

        if (hitCount > 0)
        {
            SupplyItem bestTarget = null;
            float minDistance = float.MaxValue;

            // Iterate manually to avoid LINQ allocation
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];
                if (hit.distance < minDistance)
                {
                    // Check component on the object or its parent
                    if (hit.collider.TryGetComponent<SupplyItem>(out var item))
                    {
                        bestTarget = item;
                        minDistance = hit.distance;
                    }
                    else
                    {
                        // Fallback: check in parent if collider is a child object
                        var parentItem = hit.collider.GetComponentInParent<SupplyItem>();
                        if (parentItem != null)
                        {
                            bestTarget = parentItem;
                            minDistance = hit.distance;
                        }
                    }
                }
            }

            if (bestTarget != null)
            {
                bestTarget.OnClick();
            }
        }
    }
}