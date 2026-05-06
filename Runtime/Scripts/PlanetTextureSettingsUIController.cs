using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetTextureSettingsUIController : MonoBehaviour
    {
        [System.Serializable]
        public class TerrainTextureSlot
        {
            public string title = "Texture";
            public string alphaLabel = "A";
            public TerrainTextureKind kind;
        }

        [System.Serializable]
        public class TerrainTexturePreset
        {
            public string title = "Texture";
            public Texture2D texture;
            public Texture2D normalMap;
            public Texture2D roughnessMap;
        }

        public enum TerrainTextureKind
        {
            Sand,
            Grass,
            Mountain,
            Snow
        }

        [Header("Target")]
        public Planet planet;

        [Header("Ocean Foldout")]
        public Button oceanFoldoutButton;
        public TextMeshProUGUI oceanFoldoutArrowText;
        public GameObject oceanDetailsRoot;

        [Header("Ocean")]
        public TMP_Dropdown oceanNormalMapDropdown;
        public Texture2D[] oceanNormalMaps;
        public TMP_InputField oceanWaveSpeedInput;
        public TMP_InputField oceanWaveScaleInput;
        public Slider oceanNormalStrengthSlider;
        public TextMeshProUGUI oceanNormalStrengthText;

        [Header("Terrain Textures")]
        public Transform terrainTexturesRoot;
        public PlanetTerrainTextureUIItem terrainTextureItemPrefab;
        public TerrainTextureSlot[] terrainTextureSlots =
        {
            new TerrainTextureSlot { title = "Texture 1", alphaLabel = "A0", kind = TerrainTextureKind.Sand },
            new TerrainTextureSlot { title = "Texture 2", alphaLabel = "A33", kind = TerrainTextureKind.Grass },
            new TerrainTextureSlot { title = "Texture 3", alphaLabel = "A66", kind = TerrainTextureKind.Mountain },
            new TerrainTextureSlot { title = "Texture 4", alphaLabel = "A100", kind = TerrainTextureKind.Snow }
        };
        public TerrainTexturePreset[] terrainTexturePresets;

        private readonly List<PlanetTerrainTextureUIItem> terrainItems = new List<PlanetTerrainTextureUIItem>();
        private bool isUpdatingUI;
        private bool oceanExpanded;

        private void Start()
        {
            if (oceanFoldoutButton != null) oceanFoldoutButton.onClick.AddListener(ToggleOceanExpanded);
            if (oceanNormalMapDropdown != null) oceanNormalMapDropdown.onValueChanged.AddListener(OnOceanNormalMapChanged);
            if (oceanWaveSpeedInput != null) oceanWaveSpeedInput.onEndEdit.AddListener(OnOceanWaveSpeedChanged);
            if (oceanWaveScaleInput != null) oceanWaveScaleInput.onEndEdit.AddListener(OnOceanWaveScaleChanged);
            if (oceanNormalStrengthSlider != null) oceanNormalStrengthSlider.onValueChanged.AddListener(OnOceanNormalStrengthChanged);

            RegisterTextureOptions();
            RefreshFromPlanet();
        }

        public void RegisterTextureOptions()
        {
            PlanetRuntimeTextureRegistry.Register(oceanNormalMaps);
            if (terrainTexturePresets == null) return;

            for (int i = 0; i < terrainTexturePresets.Length; i++)
            {
                TerrainTexturePreset preset = terrainTexturePresets[i];
                if (preset == null) continue;

                PlanetRuntimeTextureRegistry.Register(preset.texture);
                PlanetRuntimeTextureRegistry.Register(preset.normalMap);
                PlanetRuntimeTextureRegistry.Register(preset.roughnessMap);
            }
        }

        public TerrainTexturePreset GetTerrainTexturePreset(int index)
        {
            if (index < 0 || terrainTexturePresets == null || index >= terrainTexturePresets.Length) return null;
            return terrainTexturePresets[index];
        }

        public int GetTerrainTexturePresetIndex(Texture2D texture)
        {
            if (texture == null || terrainTexturePresets == null) return 0;

            for (int i = 0; i < terrainTexturePresets.Length; i++)
            {
                TerrainTexturePreset preset = terrainTexturePresets[i];
                if (preset != null && (preset.texture == texture || (preset.texture != null && preset.texture.name == texture.name)))
                {
                    return i + 1;
                }
            }

            return 0;
        }

        public void RefreshFromPlanet()
        {
            if (planet == null || planet.colorSettings == null) return;

            RegisterTextureOptions();
            EnsureOceanSettings();
            EnsureTerrainTextureSettings();
            isUpdatingUI = true;

            RefreshOceanNormalMapDropdown();
            SetText(oceanWaveSpeedInput, planet.colorSettings.oceanSettings.waveSpeed);
            SetText(oceanWaveScaleInput, planet.colorSettings.oceanSettings.waveScale);
            if (oceanNormalStrengthSlider != null)
            {
                oceanNormalStrengthSlider.SetValueWithoutNotify(planet.colorSettings.oceanSettings.normalStrength);
            }
            UpdateOceanNormalStrengthText();
            ApplyOceanExpandedState();

            RefreshTerrainTextureItems();

            isUpdatingUI = false;
            NotifyLayoutChanged();
        }

        public void ApplyTerrainTextureFromUi(int slotIndex)
        {
            if (isUpdatingUI || planet == null || planet.colorSettings == null) return;
            if (slotIndex < 0 || slotIndex >= terrainItems.Count || slotIndex >= terrainTextureSlots.Length) return;

            ColorSettings.TerrainTextureSettings current = GetTerrainSettings(terrainTextureSlots[slotIndex].kind);
            SetTerrainSettings(terrainTextureSlots[slotIndex].kind, terrainItems[slotIndex].BuildSettings(current));
            AutoApply();
        }

        public void ApplyAllFromUi()
        {
            if (planet == null || planet.colorSettings == null) return;

            RegisterTextureOptions();
            EnsureOceanSettings();
            EnsureTerrainTextureSettings();

            if (oceanNormalMapDropdown != null)
            {
                planet.colorSettings.oceanSettings.oceanNormalMap = GetOceanNormalMap(oceanNormalMapDropdown.value);
            }
            planet.colorSettings.oceanSettings.waveSpeed = ParseFloat(
                oceanWaveSpeedInput != null ? oceanWaveSpeedInput.text : null,
                planet.colorSettings.oceanSettings.waveSpeed);
            planet.colorSettings.oceanSettings.waveScale = ParseFloat(
                oceanWaveScaleInput != null ? oceanWaveScaleInput.text : null,
                planet.colorSettings.oceanSettings.waveScale);

            if (oceanNormalStrengthSlider != null)
            {
                planet.colorSettings.oceanSettings.normalStrength = oceanNormalStrengthSlider.value;
            }

            for (int i = 0; i < terrainItems.Count && i < terrainTextureSlots.Length; i++)
            {
                ColorSettings.TerrainTextureSettings current = GetTerrainSettings(terrainTextureSlots[i].kind);
                SetTerrainSettings(terrainTextureSlots[i].kind, terrainItems[i].BuildSettings(current));
            }
        }

        public void NotifyLayoutChanged()
        {
            Canvas.ForceUpdateCanvases();
            if (terrainTexturesRoot is RectTransform rect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
            if (transform is RectTransform self)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(self);
            }
        }

        private void RefreshTerrainTextureItems()
        {
            ClearTerrainItems();

            if (terrainTexturesRoot == null || terrainTextureItemPrefab == null || terrainTextureSlots == null) return;

            for (int i = 0; i < terrainTextureSlots.Length; i++)
            {
                PlanetTerrainTextureUIItem item = Instantiate(terrainTextureItemPrefab, terrainTexturesRoot);
                item.Initialize(this, i, GetTerrainSlotTitle(terrainTextureSlots[i]), GetTerrainSettings(terrainTextureSlots[i].kind));
                terrainItems.Add(item);
            }
        }

        private string GetTerrainSlotTitle(TerrainTextureSlot slot)
        {
            if (slot == null) return "Texture";
            if (string.IsNullOrWhiteSpace(slot.alphaLabel)) return slot.title;
            return $"{slot.title} {slot.alphaLabel}";
        }

        private void ClearTerrainItems()
        {
            for (int i = 0; i < terrainItems.Count; i++)
            {
                if (terrainItems[i] != null) Destroy(terrainItems[i].gameObject);
            }

            terrainItems.Clear();
        }

        private void RefreshOceanNormalMapDropdown()
        {
            if (oceanNormalMapDropdown == null) return;

            List<string> options = new List<string> { "None" };
            if (oceanNormalMaps != null)
            {
                for (int i = 0; i < oceanNormalMaps.Length; i++)
                {
                    options.Add(oceanNormalMaps[i] != null ? oceanNormalMaps[i].name : "Missing");
                }
            }

            oceanNormalMapDropdown.ClearOptions();
            oceanNormalMapDropdown.AddOptions(options);
            oceanNormalMapDropdown.SetValueWithoutNotify(GetOceanNormalMapDropdownIndex());
            oceanNormalMapDropdown.RefreshShownValue();
        }

        private int GetOceanNormalMapDropdownIndex()
        {
            Texture2D current = planet.colorSettings.oceanSettings.oceanNormalMap;
            if (current == null || oceanNormalMaps == null) return 0;

            for (int i = 0; i < oceanNormalMaps.Length; i++)
            {
                if (oceanNormalMaps[i] == current) return i + 1;
            }

            return 0;
        }

        private void OnOceanNormalMapChanged(int index)
        {
            if (isUpdatingUI || planet == null || planet.colorSettings == null) return;

            EnsureOceanSettings();
            planet.colorSettings.oceanSettings.oceanNormalMap = GetOceanNormalMap(index);
            AutoApply();
        }

        private Texture2D GetOceanNormalMap(int dropdownIndex)
        {
            int mapIndex = dropdownIndex - 1;
            if (mapIndex < 0 || oceanNormalMaps == null || mapIndex >= oceanNormalMaps.Length) return null;
            return oceanNormalMaps[mapIndex];
        }

        private void OnOceanWaveSpeedChanged(string value)
        {
            if (isUpdatingUI || planet == null || planet.colorSettings == null) return;

            EnsureOceanSettings();
            planet.colorSettings.oceanSettings.waveSpeed = ParseFloat(value, planet.colorSettings.oceanSettings.waveSpeed);
            SetText(oceanWaveSpeedInput, planet.colorSettings.oceanSettings.waveSpeed);
            AutoApply();
        }

        private void OnOceanWaveScaleChanged(string value)
        {
            if (isUpdatingUI || planet == null || planet.colorSettings == null) return;

            EnsureOceanSettings();
            planet.colorSettings.oceanSettings.waveScale = ParseFloat(value, planet.colorSettings.oceanSettings.waveScale);
            SetText(oceanWaveScaleInput, planet.colorSettings.oceanSettings.waveScale);
            AutoApply();
        }

        private void OnOceanNormalStrengthChanged(float value)
        {
            UpdateOceanNormalStrengthText();
            if (isUpdatingUI || planet == null || planet.colorSettings == null) return;

            EnsureOceanSettings();
            planet.colorSettings.oceanSettings.normalStrength = value;
            AutoApply();
        }

        private void UpdateOceanNormalStrengthText()
        {
            if (oceanNormalStrengthText != null && oceanNormalStrengthSlider != null)
            {
                oceanNormalStrengthText.text = $"Normal Strength: {oceanNormalStrengthSlider.value:0.00}";
            }
        }

        private void ToggleOceanExpanded()
        {
            oceanExpanded = !oceanExpanded;
            ApplyOceanExpandedState();
            NotifyLayoutChanged();
        }

        private void ApplyOceanExpandedState()
        {
            if (oceanDetailsRoot != null) oceanDetailsRoot.SetActive(oceanExpanded);
            if (oceanFoldoutArrowText != null) oceanFoldoutArrowText.text = oceanExpanded ? "▼" : "►";
        }

        private ColorSettings.TerrainTextureSettings GetTerrainSettings(TerrainTextureKind kind)
        {
            switch (kind)
            {
                case TerrainTextureKind.Grass: return planet.colorSettings.grass;
                case TerrainTextureKind.Mountain: return planet.colorSettings.mountain;
                case TerrainTextureKind.Snow: return planet.colorSettings.snow;
                default: return planet.colorSettings.sand;
            }
        }

        private void SetTerrainSettings(TerrainTextureKind kind, ColorSettings.TerrainTextureSettings settings)
        {
            switch (kind)
            {
                case TerrainTextureKind.Grass:
                    planet.colorSettings.grass = settings;
                    break;
                case TerrainTextureKind.Mountain:
                    planet.colorSettings.mountain = settings;
                    break;
                case TerrainTextureKind.Snow:
                    planet.colorSettings.snow = settings;
                    break;
                default:
                    planet.colorSettings.sand = settings;
                    break;
            }
        }

        private void EnsureTerrainTextureSettings()
        {
            if (planet.colorSettings.sand == null) planet.colorSettings.sand = new ColorSettings.TerrainTextureSettings();
            if (planet.colorSettings.grass == null) planet.colorSettings.grass = new ColorSettings.TerrainTextureSettings();
            if (planet.colorSettings.mountain == null) planet.colorSettings.mountain = new ColorSettings.TerrainTextureSettings();
            if (planet.colorSettings.snow == null) planet.colorSettings.snow = new ColorSettings.TerrainTextureSettings();
        }

        private void SetText(TMP_InputField input, float value)
        {
            if (input != null) input.SetTextWithoutNotify(value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private float ParseFloat(string value, float fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;

            string normalized = value.Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
        }

        private void EnsureOceanSettings()
        {
            if (planet.colorSettings.oceanSettings == null)
            {
                planet.colorSettings.oceanSettings = new ColorSettings.OceanSettings();
            }
        }

        private void AutoApply()
        {
            if (planet != null && planet.autoUpdate)
            {
                planet.OnPlanetSettingsUpdated();
            }
        }
    }
}
