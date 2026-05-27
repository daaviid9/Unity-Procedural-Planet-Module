using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetGradientKeyUI : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public Image keyColorImage;
        public Image backgroundImage;
        public Button removeButton;
        public Color normalColor = Color.white;
        public Color selectedColor = new Color(0.3f, 0.8f, 1f);

        public Color Color { get; private set; }
        public float Time { get; private set; }

        private PlanetGradientEditorUI editor;
        private RectTransform rectTransform;
        private RectTransform parentRect;

        public void Initialize(PlanetGradientEditorUI editor, Color color, float time)
        {
            this.editor = editor;
            rectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;
            
            SetColor(color);
            SetTime(time);
            SetSelected(false);

            if (removeButton != null)
            {
                removeButton.onClick.AddListener(RemoveThisKey);
            }
        }

        public void SetColor(Color c)
        {
            Color = c;
            if (keyColorImage != null) keyColorImage.color = new Color(c.r, c.g, c.b, 1f); // Show full alpha in UI
            editor.UpdateGradientPreview();
        }

        public void SetTime(float t)
        {
            Time = Mathf.Clamp01(t);
            UpdatePosition();
            editor.UpdateGradientPreview();
        }

        public void SetSelected(bool selected)
        {
            if (backgroundImage != null) backgroundImage.color = selected ? selectedColor : normalColor;
            if (removeButton != null) removeButton.gameObject.SetActive(selected);
            if (selected && rectTransform != null) rectTransform.SetAsLastSibling();
        }

        private void RemoveThisKey()
        {
            editor.RemoveKey(this);
        }

        public void RefreshPosition()
        {
            if (parentRect == null || rectTransform == null) return;
            
            float width = parentRect.rect.width;
            if (width <= 0f) return;

            float newX = Time * width;
            
            rectTransform.anchoredPosition = new Vector2(newX, rectTransform.anchoredPosition.y);
        }

        private void UpdatePosition()
        {
            RefreshPosition();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshPosition();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (parentRect == null) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                float normalizedX = Mathf.Clamp01((localPoint.x - parentRect.rect.xMin) / parentRect.rect.width);
                SetTime(normalizedX);
                editor.SelectKey(this);
                editor.NotifyGradientChanged();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            editor.SelectKey(this);
        }
    }
}
