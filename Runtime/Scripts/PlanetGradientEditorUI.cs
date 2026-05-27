using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetGradientEditorUI : MonoBehaviour, IPointerDownHandler
    {
        [Header("References")]
        public RawImage gradientPreview;
        public RectTransform keysContainer;
        public GameObject keyPrefab;
        
        [Header("Settings Controls")]
        public GameObject settingsPanel;
        public Slider rSlider;
        public TextMeshProUGUI rText;
        public Slider gSlider;
        public TextMeshProUGUI gText;
        public Slider bSlider;
        public TextMeshProUGUI bText;
        public Slider aSlider;
        public TextMeshProUGUI aText;
        public TMP_InputField timeInput;
        public TMP_InputField hexInput;
        

        [Space]
        public UnityEvent<Gradient> onGradientChanged;

        private List<PlanetGradientKeyUI> keys = new List<PlanetGradientKeyUI>();
        private PlanetGradientKeyUI selectedKey;
        private Texture2D previewTexture;
        private bool isUpdatingUI = false;
        private bool keyPositionsDirty = false;
        private Coroutine delayedRefreshRoutine;

        private void Awake()
        {
            EnsurePreviewTexture();

            if (rSlider != null) rSlider.onValueChanged.AddListener(OnColorSliderChanged);
            if (gSlider != null) gSlider.onValueChanged.AddListener(OnColorSliderChanged);
            if (bSlider != null) bSlider.onValueChanged.AddListener(OnColorSliderChanged);
            if (aSlider != null) aSlider.onValueChanged.AddListener(OnColorSliderChanged);
            
            if (timeInput != null) timeInput.onEndEdit.AddListener(OnTimeInputChanged);
            if (hexInput != null) hexInput.onEndEdit.AddListener(OnHexInputChanged);

            UpdateGradientPreview();
        }

        private void OnEnable()
        {
            EnsurePreviewTexture();
            UpdateGradientPreview();
            RefreshAfterLayout();
        }

        private void LateUpdate()
        {
            if (!keyPositionsDirty) return;

            RefreshKeyPositions();
            keyPositionsDirty = false;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame && selectedKey != null)
            {
                RemoveSelectedKey();
            }
        }


        public void SetGradient(Gradient gradient)
        {
            EnsurePreviewTexture();
            ClearKeys();
            if (gradient == null) gradient = new Gradient();

            if (gradient.colorKeys != null && gradient.colorKeys.Length > 0)
            {
                foreach (var ck in gradient.colorKeys)
                {
                    Color fullColor = ck.color;
                    fullColor.a = gradient.Evaluate(ck.time).a;
                    CreateKeyUI(fullColor, ck.time);
                }
            }
            else
            {
                CreateKeyUI(Color.black, 0f);
                CreateKeyUI(Color.white, 1f);
            }

            if (keys.Count > 0) SelectKey(keys[0]);
            RefreshKeyPositions();
            MarkKeyPositionsDirty();
            UpdateGradientPreview();
        }

        public Gradient GetGradient()
        {
            Gradient g = new Gradient();
            if (keys.Count == 0)
            {
                return g;
            }
            
            int count = Mathf.Min(keys.Count, 8);
            GradientColorKey[] colorKeys = new GradientColorKey[count];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[count];

            keys.Sort((a, b) => a.Time.CompareTo(b.Time));

            for (int i = 0; i < count; i++)
            {
                colorKeys[i] = new GradientColorKey(keys[i].Color, keys[i].Time);
                alphaKeys[i] = new GradientAlphaKey(keys[i].Color.a, keys[i].Time);
            }

            g.SetKeys(colorKeys, alphaKeys);
            return g;
        }

        private void CreateKeyUI(Color color, float time)
        {
            GameObject go = Instantiate(keyPrefab, keysContainer);
            PlanetGradientKeyUI keyUI = go.GetComponent<PlanetGradientKeyUI>();
            keyUI.Initialize(this, color, time);
            keys.Add(keyUI);
            keyUI.RefreshPosition();
            MarkKeyPositionsDirty();
        }

        private void RefreshKeyPositions()
        {
            Canvas.ForceUpdateCanvases();
            RebuildLayoutChain(keysContainer != null ? keysContainer : transform as RectTransform);

            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] != null)
                {
                    keys[i].RefreshPosition();
                }
            }
        }

        private void RebuildLayoutChain(RectTransform start)
        {
            RectTransform current = start;
            while (current != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(current);
                current = current.parent as RectTransform;
            }

            Canvas.ForceUpdateCanvases();
        }

        private void MarkKeyPositionsDirty()
        {
            keyPositionsDirty = true;
        }

        public void RefreshAfterLayout()
        {
            MarkKeyPositionsDirty();

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (delayedRefreshRoutine != null)
            {
                StopCoroutine(delayedRefreshRoutine);
            }

            delayedRefreshRoutine = StartCoroutine(RefreshAfterLayoutRoutine());
        }

        private IEnumerator RefreshAfterLayoutRoutine()
        {
            yield return null;
            RefreshKeyPositions();
            UpdateGradientPreview();

            yield return new WaitForEndOfFrame();
            RefreshKeyPositions();
            delayedRefreshRoutine = null;
        }

        public void SelectKey(PlanetGradientKeyUI key)
        {
            if (selectedKey != null) selectedKey.SetSelected(false);
            selectedKey = key;
            if (selectedKey != null) selectedKey.SetSelected(true);

            RefreshSettingsUI();
        }

        private void RefreshSettingsUI()
        {
            if (settingsPanel != null) settingsPanel.SetActive(selectedKey != null);
            if (selectedKey == null) return;

            isUpdatingUI = true;
            if (rSlider != null) rSlider.value = selectedKey.Color.r;
            if (gSlider != null) gSlider.value = selectedKey.Color.g;
            if (bSlider != null) bSlider.value = selectedKey.Color.b;
            if (aSlider != null) aSlider.value = selectedKey.Color.a;
            
            if (rText != null) rText.text = $"R: {Mathf.RoundToInt(selectedKey.Color.r * 255f)}";
            if (gText != null) gText.text = $"G: {Mathf.RoundToInt(selectedKey.Color.g * 255f)}";
            if (bText != null) bText.text = $"B: {Mathf.RoundToInt(selectedKey.Color.b * 255f)}";
            if (aText != null) aText.text = $"A: {Mathf.RoundToInt(selectedKey.Color.a * 255f)}";
            
            if (timeInput != null) timeInput.text = (selectedKey.Time * 100f).ToString("0.0");
            if (hexInput != null) hexInput.text = "#" + ColorUtility.ToHtmlStringRGB(selectedKey.Color);
            isUpdatingUI = false;
        }

        private void OnColorSliderChanged(float _)
        {
            if (isUpdatingUI || selectedKey == null) return;
            
            Color c = new Color(
                rSlider != null ? rSlider.value : 1f,
                gSlider != null ? gSlider.value : 1f,
                bSlider != null ? bSlider.value : 1f,
                aSlider != null ? aSlider.value : 1f
            );
            
            if (rText != null && rSlider != null) rText.text = $"R: {Mathf.RoundToInt(rSlider.value * 255f)}";
            if (gText != null && gSlider != null) gText.text = $"G: {Mathf.RoundToInt(gSlider.value * 255f)}";
            if (bText != null && bSlider != null) bText.text = $"B: {Mathf.RoundToInt(bSlider.value * 255f)}";
            if (aText != null && aSlider != null) aText.text = $"A: {Mathf.RoundToInt(aSlider.value * 255f)}";
            
            if (hexInput != null) hexInput.text = "#" + ColorUtility.ToHtmlStringRGB(c);
            
            selectedKey.SetColor(c);
            onGradientChanged?.Invoke(GetGradient());
        }

        private void OnTimeInputChanged(string val)
        {
            if (isUpdatingUI || selectedKey == null) return;
            if (float.TryParse(val, out float pct))
            {
                selectedKey.SetTime(Mathf.Clamp01(pct / 100f));
                NotifyGradientChanged();
            }
        }

        private void OnHexInputChanged(string val)
        {
            if (isUpdatingUI || selectedKey == null) return;
            
            if (!val.StartsWith("#"))
            {
                val = "#" + val;
            }

            if (ColorUtility.TryParseHtmlString(val, out Color c))
            {
                c.a = selectedKey.Color.a; // Zachovanie pôvodnej priehľadnosti
                
                isUpdatingUI = true;
                if (rSlider != null) rSlider.value = c.r;
                if (gSlider != null) gSlider.value = c.g;
                if (bSlider != null) bSlider.value = c.b;
                
                if (rText != null) rText.text = $"R: {Mathf.RoundToInt(c.r * 255f)}";
                if (gText != null) gText.text = $"G: {Mathf.RoundToInt(c.g * 255f)}";
                if (bText != null) bText.text = $"B: {Mathf.RoundToInt(c.b * 255f)}";
                
                if (hexInput != null) hexInput.text = "#" + ColorUtility.ToHtmlStringRGB(c);
                
                isUpdatingUI = false;

                selectedKey.SetColor(c);
                NotifyGradientChanged();
            }
            else
            {
                // Revert to old valid hex if parsing failed
                if (hexInput != null) hexInput.text = "#" + ColorUtility.ToHtmlStringRGB(selectedKey.Color);
            }
        }

        public void NotifyGradientChanged()
        {
            onGradientChanged?.Invoke(GetGradient());
        }

        private void RemoveSelectedKey()
        {
            RemoveKey(selectedKey);
        }

        public void RemoveKey(PlanetGradientKeyUI key)
        {
            if (key == null || keys.Count <= 2) return;

            bool removedSelected = key == selectedKey;
            keys.Remove(key);
            Destroy(key.gameObject);
            if (removedSelected) selectedKey = null;

            SelectKey(keys[0]);
            UpdateGradientPreview();
            onGradientChanged?.Invoke(GetGradient());
        }

        public void ClearKeys()
        {
            foreach (var k in keys)
            {
                if (k != null) Destroy(k.gameObject);
            }
            keys.Clear();
            selectedKey = null;
        }

        public void UpdateGradientPreview()
        {
            EnsurePreviewTexture();
            if (previewTexture == null) return;

            Gradient g = GetGradient();
            for (int x = 0; x < previewTexture.width; x++)
            {
                float t = x / (float)(previewTexture.width - 1);
                Color c = g.Evaluate(t);
                c.a = 1f;
                previewTexture.SetPixel(x, 0, c);
            }
            previewTexture.Apply();
            
            if (selectedKey != null) RefreshSettingsUI();
        }

        private void EnsurePreviewTexture()
        {
            if (previewTexture == null)
            {
                previewTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
                previewTexture.wrapMode = TextureWrapMode.Clamp;
            }

            if (gradientPreview != null && gradientPreview.texture != previewTexture)
            {
                gradientPreview.texture = previewTexture;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (keysContainer == null || keys.Count >= 8) return;

            // Only create keys from direct clicks inside the key container.
            if (!RectTransformUtility.RectangleContainsScreenPoint(keysContainer, eventData.position, eventData.pressEventCamera)) 
                return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(keysContainer, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                float t = Mathf.Clamp01((localPoint.x - keysContainer.rect.xMin) / keysContainer.rect.width);
                Gradient g = GetGradient();
                CreateKeyUI(g.Evaluate(t), t);
                SelectKey(keys[keys.Count - 1]);
                UpdateGradientPreview();
                onGradientChanged?.Invoke(GetGradient());
            }
        }
        
        private void OnDestroy()
        {
            if (previewTexture != null) Destroy(previewTexture);
        }
    }
}
