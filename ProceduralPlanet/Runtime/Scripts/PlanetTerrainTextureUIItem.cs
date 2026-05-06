using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetTerrainTextureUIItem : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public Button foldoutButton;
        public TextMeshProUGUI foldoutArrowText;
        public GameObject detailsRoot;

        public Toggle enabledToggle;
        public TMP_Dropdown textureDropdown;
        public Slider normalStrengthSlider;
        public TextMeshProUGUI normalStrengthText;
        public TMP_InputField tilingInput;

        private PlanetTextureSettingsUIController owner;
        private int slotIndex;
        private bool isUpdatingUI;
        private bool expanded;

        public void Initialize(PlanetTextureSettingsUIController owner, int slotIndex, string title, ColorSettings.TerrainTextureSettings settings)
        {
            this.owner = owner;
            this.slotIndex = slotIndex;

            if (titleText != null) titleText.text = title;

            BindEvents();
            Refresh(settings);
        }

        public void Refresh(ColorSettings.TerrainTextureSettings settings)
        {
            if (settings == null) return;

            isUpdatingUI = true;

            FillTextureDropdown(settings.texture);

            if (enabledToggle != null) enabledToggle.SetIsOnWithoutNotify(settings.enabled);
            if (normalStrengthSlider != null) normalStrengthSlider.SetValueWithoutNotify(settings.normalStrength);
            if (tilingInput != null) tilingInput.SetTextWithoutNotify(settings.tiling.ToString("0.###", CultureInfo.InvariantCulture));

            UpdateNormalStrengthText();
            ApplyExpandedState();

            isUpdatingUI = false;
        }

        public ColorSettings.TerrainTextureSettings BuildSettings(ColorSettings.TerrainTextureSettings fallback)
        {
            ColorSettings.TerrainTextureSettings settings = fallback ?? new ColorSettings.TerrainTextureSettings();
            PlanetTextureSettingsUIController.TerrainTexturePreset preset = owner.GetTerrainTexturePreset(GetSelectedTexturePresetIndex());

            settings.enabled = enabledToggle == null || enabledToggle.isOn;
            settings.texture = preset != null ? preset.texture : null;
            settings.normalMap = preset != null ? preset.normalMap : null;
            settings.roughnessMap = preset != null ? preset.roughnessMap : null;
            settings.normalStrength = normalStrengthSlider != null ? normalStrengthSlider.value : settings.normalStrength;
            settings.tiling = ParseFloat(tilingInput != null ? tilingInput.text : null, settings.tiling);
            return settings;
        }

        private void BindEvents()
        {
            if (foldoutButton != null) foldoutButton.onClick.AddListener(ToggleExpanded);
            if (enabledToggle != null) enabledToggle.onValueChanged.AddListener(_ => OnValueChanged());
            if (textureDropdown != null) textureDropdown.onValueChanged.AddListener(_ => OnValueChanged());
            if (normalStrengthSlider != null) normalStrengthSlider.onValueChanged.AddListener(_ => OnNormalStrengthChanged());
            if (tilingInput != null) tilingInput.onEndEdit.AddListener(_ => OnValueChanged());
        }

        private void FillTextureDropdown(Texture2D selected)
        {
            if (textureDropdown == null) return;

            List<string> names = new List<string> { "None" };
            if (owner.terrainTexturePresets != null)
            {
                for (int i = 0; i < owner.terrainTexturePresets.Length; i++)
                {
                    var preset = owner.terrainTexturePresets[i];
                    names.Add(GetPresetTitle(preset));
                }
            }

            textureDropdown.ClearOptions();
            textureDropdown.AddOptions(names);
            textureDropdown.SetValueWithoutNotify(owner.GetTerrainTexturePresetIndex(selected));
            textureDropdown.RefreshShownValue();
        }

        private string GetPresetTitle(PlanetTextureSettingsUIController.TerrainTexturePreset preset)
        {
            if (preset == null) return "Missing";
            if (!string.IsNullOrWhiteSpace(preset.title)) return preset.title;
            if (preset.texture != null) return preset.texture.name;
            return "Missing";
        }

        private int GetSelectedTexturePresetIndex()
        {
            if (textureDropdown == null) return -1;
            int index = textureDropdown.value - 1;
            if (index < 0) return -1;
            return index;
        }

        private void OnNormalStrengthChanged()
        {
            UpdateNormalStrengthText();
            OnValueChanged();
        }

        private void UpdateNormalStrengthText()
        {
            if (normalStrengthText != null && normalStrengthSlider != null)
            {
                normalStrengthText.text = $"Normal Strength: {normalStrengthSlider.value:0.00}";
            }
        }

        private void OnValueChanged()
        {
            if (isUpdatingUI) return;
            owner.ApplyTerrainTextureFromUi(slotIndex);
        }

        private void ToggleExpanded()
        {
            expanded = !expanded;
            ApplyExpandedState();
            owner.NotifyLayoutChanged();
        }

        private void ApplyExpandedState()
        {
            if (detailsRoot != null) detailsRoot.SetActive(expanded);
            if (foldoutArrowText != null) foldoutArrowText.text = expanded ? "▼" : "►";
        }

        private float ParseFloat(string value, float fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;

            string normalized = value.Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
        }
    }
}
