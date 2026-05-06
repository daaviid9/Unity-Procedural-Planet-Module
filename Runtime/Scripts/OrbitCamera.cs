using UnityEngine;
using UnityEngine.InputSystem;

namespace ProceduralPlanet
{
    /// <summary>
    /// A premium orbital camera controller suitable for viewing planets.
    /// Supports rotation, zooming, damping, and auto-rotation.
    /// Refactored to use the Unity Input System.
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Targeting")]
        public Transform target;
        public Vector3 targetOffset = Vector3.zero;

        [Header("Distance & Zoom")]
        public float distance = 60f;
        public float minDistance = 27f;
        public float maxDistance = 300f;
        public float zoomSensitivity = 5f;
        
        [Header("UI Zoom Blocking")]
        public RectTransform zoomBlockRect;

        [Header("Rotation")]
        public float xSpeed = 0.5f;
        public float ySpeed = 0.5f;
        public float yMinLimit = -80f;
        public float yMaxLimit = 80f;

        [Header("Smoothness")]
        public float damping = 50.0f;
        public bool smoothZoom = true;
        public float zoomDamping = 10f;

        [Header("Auto Rotation")]
        public bool autoRotate = false;
        public float autoRotateSpeed = 5f;
        public float idleTimeBeforeAutoRotate = 3f;

        private float x = 0.0f;
        private float y = 0.0f;
        private float currentDistance;
        private float desiredDistance;
        private float lastInputTime;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;

            currentDistance = distance;
            desiredDistance = distance;

            // Try to find a planet if no target is assigned
            if (target == null)
            {
                Planet planet = FindFirstObjectByType<Planet>();
                if (planet != null) target = planet.transform;
            }

            if (target == null)
            {
                Debug.LogWarning("OrbitCamera: No target assigned and no Planet found in scene. Using origin.");
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            bool inputActive = false;
            var mouse = Mouse.current;

            if (mouse != null)
            {
                // 1. Rotation (Right Mouse Button)
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    x += delta.x * xSpeed * 0.1f;
                    y -= delta.y * ySpeed * 0.1f;
                    inputActive = true;
                }

                // 2. Zooming (Scroll)
                float scroll = mouse.scroll.ReadValue().y;
                bool blockZoom = false;
                if (zoomBlockRect != null)
                {
                    blockZoom = RectTransformUtility.RectangleContainsScreenPoint(
                        zoomBlockRect,
                        mouse.position.ReadValue(),
                        null);
                }
                else
                {
                    // Fallback: if no specific rect is assigned, keep zoom always available.
                    blockZoom = false;
                }

                if (!blockZoom && Mathf.Abs(scroll) > 0.01f)
                {
                    // Normalize scroll for different hardware/OS (usually 120 or small values)
                    float scrollAmount = (scroll > 0) ? 1 : -1;
                    desiredDistance -= scrollAmount * zoomSensitivity;
                    desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
                    inputActive = true;
                }
            }

            if (inputActive)
            {
                lastInputTime = Time.time;
            }

            // 3. Auto-Rotation Logic
            if (autoRotate && Time.time - lastInputTime > idleTimeBeforeAutoRotate)
            {
                x += autoRotateSpeed * Time.deltaTime;
            }

            // Apply Limits
            y = ClampAngle(y, yMinLimit, yMaxLimit);

            // Interpolation
            Quaternion rotation = Quaternion.Euler(y, x, 0);

            if (smoothZoom)
            {
                currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDamping);
            }
            else
            {
                currentDistance = desiredDistance;
            }

            // Calculate Position
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
            Vector4 pivotPos = target.position + targetOffset;
            Vector3 position = (rotation * negDistance) + (Vector3)pivotPos;

            // Apply to Camera
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * damping);
            transform.position = position;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}

