using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Universal.DragDrop
{
    /// <summary>
    /// Utility class for converting positions between different coordinate spaces.
    /// This is the key piece that enables cross-space drag and drop.
    /// </summary>
    public static class SpaceConverter
    {
        private static readonly List<RaycastResult> s_UIRaycastResults = new List<RaycastResult>();

        // ─── Screen → World Conversions ─────────────────────────────

        /// <summary>
        /// Convert screen position to world position based on the target space.
        /// </summary>
        public static Vector3 ScreenToWorld(Vector2 screenPos, DragSpace targetSpace, Camera cam, DragContext context)
        {
            switch (targetSpace)
            {
                case DragSpace.World3D:
                    return ScreenToWorld3D(screenPos, cam, context);
                case DragSpace.World2D:
                    return ScreenToWorld2D(screenPos, cam);
                case DragSpace.UI:
                    return screenPos; // UI uses screen coordinates directly
                default:
                    return screenPos;
            }
        }

        /// <summary>
        /// Convert screen position to 3D world position using raycast or drag plane.
        /// </summary>
        public static Vector3 ScreenToWorld3D(Vector2 screenPos, Camera cam, DragContext context)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Ray ray = cam.ScreenPointToRay(screenPos);

            // First try raycast against colliders
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                context.Hit3D = hit;
                return hit.point;
            }

            // Fallback to drag plane
            if (context.DragPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            // Last fallback: project onto a default plane at start height
            Plane defaultPlane = new Plane(Vector3.up, context.StartWorldPosition);
            if (defaultPlane.Raycast(ray, out float dist))
            {
                return ray.GetPoint(dist);
            }

            return context.StartWorldPosition;
        }

        /// <summary>
        /// Convert screen position to 2D world position.
        /// </summary>
        public static Vector3 ScreenToWorld2D(Vector2 screenPos, Camera cam)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            worldPos.z = 0f;
            return worldPos;
        }

        /// <summary>
        /// Convert screen position to UI local position within a RectTransform.
        /// </summary>
        public static Vector2 ScreenToUILocal(Vector2 screenPos, RectTransform parent, Camera uiCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, uiCamera, out Vector2 localPos);
            return localPos;
        }

        // ─── World → Screen Conversions ─────────────────────────────

        /// <summary>
        /// Convert a world position to screen position.
        /// </summary>
        public static Vector2 WorldToScreen(Vector3 worldPos, Camera cam)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return Vector2.zero;

            return cam.WorldToScreenPoint(worldPos);
        }

        // ─── Cross-Space Conversions ────────────────────────────────

        /// <summary>
        /// Convert position from one drag space to another.
        /// This is the core function enabling UI↔3D, UI↔2D, etc.
        /// </summary>
        public static Vector3 Convert(Vector3 position, DragSpace from, DragSpace to, Camera cam, DragContext context)
        {
            if (from == to) return position;

            // Convert source position to screen space first
            Vector2 screenPos;
            switch (from)
            {
                case DragSpace.UI:
                    screenPos = position; // UI positions are already in screen space
                    break;
                case DragSpace.World3D:
                case DragSpace.World2D:
                    screenPos = WorldToScreen(position, cam);
                    break;
                default:
                    screenPos = position;
                    break;
            }

            // Then convert from screen space to target space
            return ScreenToWorld(screenPos, to, cam, context);
        }

        // ─── Raycast Utilities ──────────────────────────────────────

        /// <summary>
        /// Perform a 3D raycast from screen position, optionally with layer mask.
        /// </summary>
        public static bool Raycast3D(Vector2 screenPos, Camera cam, out RaycastHit hit, float maxDistance = 1000f, int layerMask = -1)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) { hit = default; return false; }

            Ray ray = cam.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out hit, maxDistance, layerMask);
        }

        /// <summary>
        /// Perform a 2D raycast from screen position.
        /// </summary>
        public static RaycastHit2D Raycast2D(Vector2 screenPos, Camera cam, int layerMask = -1)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return default;

            Vector2 worldPos = cam.ScreenToWorldPoint(screenPos);
            return Physics2D.Raycast(worldPos, Vector2.zero, 0f, layerMask);
        }

        /// <summary>
        /// Perform a UI raycast from screen position using EventSystem.
        /// </summary>
        public static bool RaycastUI(Vector2 screenPos, out List<RaycastResult> results)
        {
            results = s_UIRaycastResults;
            results.Clear();

            if (EventSystem.current == null)
                return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        /// <summary>
        /// Find the first IDropZone under the screen position, searching across all spaces.
        /// </summary>
        public static IDropZone FindDropZoneAtScreenPosition(Vector2 screenPos, Camera cam, DragContext context, string channel)
        {
            // 1. Check UI drop zones first (they're on top)
            if (RaycastUI(screenPos, out var uiResults))
            {
                foreach (var result in uiResults)
                {
                    var dropZone = result.gameObject.GetComponent<IDropZone>();
                    if (dropZone != null && dropZone.IsDropEnabled &&
                        (string.IsNullOrEmpty(channel) || dropZone.Channel == channel))
                    {
                        return dropZone;
                    }
                }
            }

            // 2. Check 3D drop zones
            if (Raycast3D(screenPos, cam, out RaycastHit hit3D))
            {
                var dropZone = hit3D.collider.GetComponent<IDropZone>();
                if (dropZone != null && dropZone.IsDropEnabled &&
                    (string.IsNullOrEmpty(channel) || dropZone.Channel == channel))
                {
                    context.Hit3D = hit3D;
                    return dropZone;
                }
            }

            // 3. Check 2D drop zones
            RaycastHit2D hit2D = Raycast2D(screenPos, cam);
            if (hit2D.collider != null)
            {
                var dropZone = hit2D.collider.GetComponent<IDropZone>();
                if (dropZone != null && dropZone.IsDropEnabled &&
                    (string.IsNullOrEmpty(channel) || dropZone.Channel == channel))
                {
                    context.Hit2D = hit2D;
                    return dropZone;
                }
            }

            return null;
        }

        // ─── Plane Utilities ────────────────────────────────────────

        /// <summary>
        /// Create a drag plane at a world position facing the camera.
        /// </summary>
        public static Plane CreateDragPlane(Vector3 position, Camera cam)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return new Plane(Vector3.up, position);

            return new Plane(-cam.transform.forward, position);
        }

        /// <summary>
        /// Create a horizontal drag plane at a specific height.
        /// </summary>
        public static Plane CreateHorizontalPlane(float height = 0f)
        {
            return new Plane(Vector3.up, new Vector3(0, height, 0));
        }
    }
}
