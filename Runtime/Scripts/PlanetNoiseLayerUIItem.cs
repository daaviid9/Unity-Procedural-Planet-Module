using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

namespace ProceduralPlanet
{
    public class PlanetNoiseLayerUIItem : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI titleText;
        public Toggle enabledToggle;
        public Toggle useFirstLayerMaskToggle;
        public TMP_Dropdown filterTypeDropdown;
        public Button removeButton;
        public Button foldoutButton;
        public TextMeshProUGUI foldoutArrowText;
        public GameObject detailsRoot;
        public LayoutElement layoutElement;
        public float collapsedHeight = 44f;

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

        [Header("Ridgid Extra")]
        public GameObject ridgidExtraGroup;
        public TMP_InputField ridgidWeightMultiplierInput;

        private PlanetShapeSettingsUIController owner;
        private int layerIndex;
        private bool expanded;

        public void Initialize(PlanetShapeSettingsUIController controller, int index, ShapeSettings.NoiseLayer layerData, bool startExpanded)
        {
            owner = controller;
            layerIndex = index;
            expanded = startExpanded;
            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();

            if (titleText != null) titleText.text = $"Layer {index + 1}";

            NoiseSettings noise = layerData.noiseSettings ?? new NoiseSettings
            {
                simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings(),
                ridgidNoiseSettings = new NoiseSettings.RidgidNoiseSettings()
            };

            if (enabledToggle != null) enabledToggle.SetIsOnWithoutNotify(layerData.enabled);
            if (useFirstLayerMaskToggle != null) useFirstLayerMaskToggle.SetIsOnWithoutNotify(layerData.useFirstLayerAsMask);
            if (filterTypeDropdown != null) filterTypeDropdown.SetValueWithoutNotify((int)noise.filterType);

            FillSimpleFields(noise.simpleNoiseSettings);
            FillRidgidFields(noise.ridgidNoiseSettings);
            RefreshFilterGroups();
            ApplyExpandedState();
            BindEvents();
        }

        public bool IsExpanded => expanded;

        public ShapeSettings.NoiseLayer BuildLayerData()
        {
            NoiseSettings.FilterType filterType = filterTypeDropdown != null
                ? (NoiseSettings.FilterType)filterTypeDropdown.value
                : NoiseSettings.FilterType.Simple;

            var simple = new NoiseSettings.SimpleNoiseSettings
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
            };

            var ridgid = new NoiseSettings.RidgidNoiseSettings
            {
                strength = simple.strength,
                numLayers = simple.numLayers,
                baseRoughness = simple.baseRoughness,
                roughness = simple.roughness,
                persistence = simple.persistence,
                minValue = simple.minValue,
                centre = simple.centre,
                weightMultiplier = ParseFloat(ridgidWeightMultiplierInput, 0.8f)
            };

            return new ShapeSettings.NoiseLayer
            {
                enabled = enabledToggle == null || enabledToggle.isOn,
                useFirstLayerAsMask = useFirstLayerMaskToggle != null && useFirstLayerMaskToggle.isOn,
                noiseSettings = new NoiseSettings
                {
                    filterType = filterType,
                    simpleNoiseSettings = simple,
                    ridgidNoiseSettings = ridgid
                }
            };
        }

        private void BindEvents()
        {
            if (enabledToggle != null) enabledToggle.onValueChanged.AddListener(_ => OnValueChanged());
            if (useFirstLayerMaskToggle != null) useFirstLayerMaskToggle.onValueChanged.AddListener(_ => OnValueChanged());
            if (filterTypeDropdown != null) filterTypeDropdown.onValueChanged.AddListener(_ => OnFilterChanged());
            if (removeButton != null) removeButton.onClick.AddListener(OnRemoveClicked);

            BindInput(simpleStrengthInput);
            BindInput(simpleBaseRoughnessInput);
            BindInput(simpleRoughnessInput);
            BindInput(simplePersistenceInput);
            BindInput(simpleMinValueInput);
            BindInput(simpleCentreXInput);
            BindInput(simpleCentreYInput);
            BindInput(simpleCentreZInput);
            BindInput(ridgidWeightMultiplierInput);
            if (simpleNumLayersSlider != null) simpleNumLayersSlider.onValueChanged.AddListener(OnNumLayersSliderChanged);
            if (foldoutButton != null) foldoutButton.onClick.AddListener(ToggleExpanded);
        }

        private void BindInput(TMP_InputField input)
        {
            if (input != null) input.onEndEdit.AddListener(_ => OnValueChanged());
        }

        private void OnFilterChanged()
        {
            RefreshFilterGroups();
            OnValueChanged();
        }

        private void RefreshFilterGroups()
        {
            bool ridgid = filterTypeDropdown != null && filterTypeDropdown.value == (int)NoiseSettings.FilterType.Ridgid;
            if (simpleGroup != null) simpleGroup.SetActive(true);
            if (ridgidExtraGroup != null) ridgidExtraGroup.SetActive(ridgid);
        }

        private void OnRemoveClicked()
        {
            owner?.RemoveLayer(layerIndex);
        }

        private void OnValueChanged()
        {
            owner?.ApplyLayersFromUi();
        }

        private void FillSimpleFields(NoiseSettings.SimpleNoiseSettings s)
        {
            if (s == null) s = new NoiseSettings.SimpleNoiseSettings();
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

        private void FillRidgidFields(NoiseSettings.RidgidNoiseSettings r)
        {
            if (r == null) r = new NoiseSettings.RidgidNoiseSettings();
            SetText(ridgidWeightMultiplierInput, r.weightMultiplier);
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
            if (simpleNumLayersSlider != null)
            {
                return Mathf.RoundToInt(simpleNumLayersSlider.value);
            }

            return 1;
        }

        private void OnNumLayersSliderChanged(float value)
        {
            UpdateNumLayersLabel(Mathf.RoundToInt(value));
            OnValueChanged();
        }

        private void UpdateNumLayersLabel(int numLayers)
        {
            if (simpleNumLayersText != null)
            {
                simpleNumLayersText.text = $"Layers: {numLayers}";
            }
        }

        private void ToggleExpanded()
        {
            expanded = !expanded;
            ApplyExpandedState();
            owner?.NotifyLayerFoldoutChanged(layerIndex, expanded);
        }

        private void ApplyExpandedState()
        {
            if (detailsRoot != null)
            {
                detailsRoot.SetActive(expanded);
            }

            if (foldoutArrowText != null)
            {
                foldoutArrowText.text = expanded ? "▼" : "►";
            }

            if (layoutElement != null)
            {
                layoutElement.preferredHeight = expanded ? -1f : collapsedHeight;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            if (transform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }
    }
}
