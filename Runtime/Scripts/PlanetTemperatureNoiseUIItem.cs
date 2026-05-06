using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

namespace ProceduralPlanet
{
    public class PlanetTemperatureNoiseUIItem : MonoBehaviour
    {
        [Header("Header")]
        public Button foldoutButton;
        public TextMeshProUGUI foldoutArrowText;
        public GameObject detailsRoot;
        public LayoutElement layoutElement;
        public float collapsedHeight = 44f;

        [Header("Biome Blend")]
        public Toggle debugModeToggle;
        public Slider blendAmountSlider;
        public TextMeshProUGUI blendAmountText;

        [Header("Simple Settings")]
        public GameObject simpleGroup;
        public TMP_InputField simpleStrengthInput;
        public Slider simpleNumLayersSlider;
        public TextMeshProUGUI simpleNumLayersText;
        public TMP_InputField simpleBaseRoughnessInput;
        public TMP_InputField simpleRoughnessInput;
        public TMP_InputField simplePersistenceInput;
        public TMP_InputField simpleMinValueInput;
        public TMP_InputField simpleCentreXInput;
        public TMP_InputField simpleCentreYInput;
        public TMP_InputField simpleCentreZInput;

        private PlanetColorSettingsUIController owner;
        private bool expanded;

        public void Initialize(PlanetColorSettingsUIController controller, NoiseSettings noiseSettings, bool startExpanded)
        {
            owner = controller;
            expanded = startExpanded;
            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();

            if (noiseSettings == null) noiseSettings = new NoiseSettings();
            var simple = noiseSettings.simpleNoiseSettings ?? new NoiseSettings.SimpleNoiseSettings();

            FillGlobalFields();
            FillSimpleFields(simple);
            ApplyExpandedState();
            BindEvents();
        }

        public NoiseSettings BuildNoiseSettings()
        {
            return new NoiseSettings
            {
                filterType = NoiseSettings.FilterType.Simple,
                simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings
                {
                    strength = ParseFloat(simpleStrengthInput, 1f),
                    numLayers = Mathf.Clamp(GetNumLayersValue(), 1, 8),
                    baseRoughness = ParseFloat(simpleBaseRoughnessInput, 1f),
                    roughness = ParseFloat(simpleRoughnessInput, 2f),
                    persistence = ParseFloat(simplePersistenceInput, 0.5f),
                    minValue = ParseFloat(simpleMinValueInput, 0f),
                    centre = new Vector3(
                        ParseFloat(simpleCentreXInput, 0f),
                        ParseFloat(simpleCentreYInput, 0f),
                        ParseFloat(simpleCentreZInput, 0f))
                }
            };
        }

        private void BindEvents()
        {
            if (debugModeToggle != null) debugModeToggle.onValueChanged.AddListener(OnDebugModeChanged);
            if (blendAmountSlider != null) blendAmountSlider.onValueChanged.AddListener(OnBlendAmountChanged);

            BindInput(simpleStrengthInput);
            BindInput(simpleBaseRoughnessInput);
            BindInput(simpleRoughnessInput);
            BindInput(simplePersistenceInput);
            BindInput(simpleMinValueInput);
            BindInput(simpleCentreXInput);
            BindInput(simpleCentreYInput);
            BindInput(simpleCentreZInput);
            
            if (simpleNumLayersSlider != null) simpleNumLayersSlider.onValueChanged.AddListener(OnNumLayersSliderChanged);
            if (foldoutButton != null) foldoutButton.onClick.AddListener(ToggleExpanded);
        }

        private void BindInput(TMP_InputField input)
        {
            if (input != null) input.onEndEdit.AddListener(_ => OnValueChanged());
        }

        private void OnValueChanged()
        {
            owner?.ApplyTemperatureFromUi();
        }

        private void OnDebugModeChanged(bool value)
        {
            if (owner == null || owner.planet == null || owner.planet.colorSettings == null) return;

            owner.planet.colorSettings.debugMode = value;
            owner.ApplyTemperatureGlobalsFromUi();
        }

        private void OnBlendAmountChanged(float value)
        {
            UpdateBlendAmountText(value);
            if (owner == null || owner.planet == null || owner.planet.colorSettings == null) return;

            owner.planet.colorSettings.biomeSettings.blendAmount = value;
            owner.ApplyTemperatureGlobalsFromUi();
        }

        private void FillGlobalFields()
        {
            if (owner == null || owner.planet == null || owner.planet.colorSettings == null) return;

            if (debugModeToggle != null) debugModeToggle.SetIsOnWithoutNotify(owner.planet.colorSettings.debugMode);
            if (blendAmountSlider != null) blendAmountSlider.SetValueWithoutNotify(owner.planet.colorSettings.biomeSettings.blendAmount);
            UpdateBlendAmountText(owner.planet.colorSettings.biomeSettings.blendAmount);
        }

        private void UpdateBlendAmountText(float value)
        {
            if (blendAmountText != null) blendAmountText.text = $"Blend: {value:0.00}";
        }

        private void FillSimpleFields(NoiseSettings.SimpleNoiseSettings s)
        {
            SetText(simpleStrengthInput, s.strength);
            if (simpleNumLayersSlider != null) simpleNumLayersSlider.SetValueWithoutNotify(s.numLayers);
            UpdateNumLayersLabel(s.numLayers);
            SetText(simpleBaseRoughnessInput, s.baseRoughness);
            SetText(simpleRoughnessInput, s.roughness);
            SetText(simplePersistenceInput, s.persistence);
            SetText(simpleMinValueInput, s.minValue);
            SetText(simpleCentreXInput, s.centre.x);
            SetText(simpleCentreYInput, s.centre.y);
            SetText(simpleCentreZInput, s.centre.z);
        }

        private void SetText(TMP_InputField input, float value)
        {
            if (input != null) input.SetTextWithoutNotify(value.ToString("0.###"));
        }

        private float ParseFloat(TMP_InputField input, float fallback)
        {
            if (input == null) return fallback;
            string normalized = input.text.Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        private int GetNumLayersValue()
        {
            if (simpleNumLayersSlider != null) return Mathf.RoundToInt(simpleNumLayersSlider.value);
            return 1;
        }

        private void OnNumLayersSliderChanged(float value)
        {
            UpdateNumLayersLabel(Mathf.RoundToInt(value));
            OnValueChanged();
        }

        private void UpdateNumLayersLabel(int numLayers)
        {
            if (simpleNumLayersText != null) simpleNumLayersText.text = $"Layers: {numLayers}";
        }

        private void ToggleExpanded()
        {
            expanded = !expanded;
            ApplyExpandedState();
            owner?.NotifyLayoutChanged();
        }

        private void ApplyExpandedState()
        {
            if (detailsRoot != null) detailsRoot.SetActive(expanded);
            if (foldoutArrowText != null) foldoutArrowText.text = expanded ? "▼" : "►";
            if (layoutElement != null) layoutElement.preferredHeight = expanded ? -1f : collapsedHeight;
        }
    }
}
