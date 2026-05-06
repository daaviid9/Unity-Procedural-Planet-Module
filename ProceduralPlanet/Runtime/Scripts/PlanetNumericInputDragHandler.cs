using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProceduralPlanet
{
    public class PlanetNumericInputDragHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public TMP_InputField targetInput;
        public float dragSensitivity = 0.02f;
        public bool wholeNumbers;
        public bool clampValue;
        public float minValue;
        public float maxValue = 1f;
        public bool notifyWhileDragging = true;

        [Header("Cursor")]
        public Texture2D horizontalDragCursor;
        public Vector2 cursorHotspot = new Vector2(16f, 16f);
        public CursorMode cursorMode = CursorMode.Auto;

        private float startValue;
        private float dragOffset;
        private bool pointerOver;
        private bool dragging;

        private void OnDisable()
        {
            ResetCursor();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerOver = true;
            ApplyCursor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOver = false;
            if (!dragging) ResetCursor();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            ApplyCursor();
            startValue = Parse(targetInput != null ? targetInput.text : null, 0f);
            dragOffset = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetInput == null) return;

            dragOffset += eventData.delta.x * dragSensitivity;
            float value = startValue + dragOffset;
            if (wholeNumbers) value = Mathf.Round(value);
            if (clampValue) value = Mathf.Clamp(value, minValue, maxValue);

            targetInput.SetTextWithoutNotify(Format(value));
            if (notifyWhileDragging) targetInput.onEndEdit.Invoke(targetInput.text);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (targetInput == null) return;

            targetInput.onEndEdit.Invoke(targetInput.text);
            dragging = false;
            if (!pointerOver) ResetCursor();
        }

        private void ApplyCursor()
        {
            if (horizontalDragCursor != null)
            {
                Cursor.SetCursor(horizontalDragCursor, cursorHotspot, cursorMode);
            }
        }

        private void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            pointerOver = false;
            dragging = false;
        }

        private string Format(float value)
        {
            return wholeNumbers
                ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static float Parse(string value, float fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;

            string normalized = value.Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
        }
    }
}
