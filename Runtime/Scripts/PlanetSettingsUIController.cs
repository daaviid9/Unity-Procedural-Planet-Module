using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProceduralPlanet
{
    public class PlanetSettingsUIController : MonoBehaviour
    {
        [Header("Target")]
        public Planet planet;

        [Header("Slot")]
        public TMP_Dropdown slotDropdown;
        public TextMeshProUGUI statusText;

        [Header("Simple Controls")]
        public Slider radiusSlider;
        public TextMeshProUGUI radiusValueText;
        public Toggle autoUpdateToggle;
        public TMP_Dropdown lodQualityDropdown;
        public PlanetColorSettingsUIController colorSettingsUi;
        public PlanetTextureSettingsUIController textureSettingsUi;
        public PlanetShapeSettingsUIController shapeSettingsUi;

        [Header("Actions")]
        public Button applyButton;
        public Button saveButton;
        public Button copyButton;
        public Button pasteButton;

        private bool isRefreshingUi;
        private bool didInitialLoad;

        private void Start()
        {
            if (applyButton != null) applyButton.onClick.AddListener(ApplyBasicValuesToPlanet);
            if (saveButton != null) saveButton.onClick.AddListener(SaveCurrentSlot);
            if (copyButton != null) copyButton.onClick.AddListener(CopyCurrentPlanet);
            if (pasteButton != null) pasteButton.onClick.AddListener(PasteToCurrentSlot);
            if (slotDropdown != null) slotDropdown.onValueChanged.AddListener(OnSlotChanged);
            if (radiusSlider != null) radiusSlider.onValueChanged.AddListener(OnRadiusSliderChanged);
            if (autoUpdateToggle != null) autoUpdateToggle.onValueChanged.AddListener(OnAutoUpdateChanged);
            if (lodQualityDropdown != null) lodQualityDropdown.onValueChanged.AddListener(OnLodQualityChanged);

            TryLoadInitialSlot();
            if (!didInitialLoad)
            {
                RefreshUiFromPlanet();
                SetStatus("Runtime preset UI ready.");
            }
        }

        public void RefreshUiFromPlanet()
        {
            if (planet == null || planet.shapeSettings == null)
            {
                SetStatus("Planet or ShapeSettings is missing.");
                return;
            }

            isRefreshingUi = true;
            try
            {
                if (radiusSlider != null)
                {
                    radiusSlider.SetValueWithoutNotify(planet.shapeSettings.planetRadius);
                    UpdateRadiusValueLabel();
                }

                if (autoUpdateToggle != null)
                {
                    autoUpdateToggle.SetIsOnWithoutNotify(planet.autoUpdate);
                }

                if (colorSettingsUi != null)
                {
                    colorSettingsUi.planet = planet;
                    colorSettingsUi.RefreshFromPlanet();
                }

                if (textureSettingsUi != null)
                {
                    textureSettingsUi.planet = planet;
                    textureSettingsUi.RefreshFromPlanet();
                }

                if (shapeSettingsUi != null)
                {
                    shapeSettingsUi.planet = planet;
                    shapeSettingsUi.RefreshFromPlanet();
                }
            }
            finally
            {
                isRefreshingUi = false;
            }
        }

        public void ApplyBasicValuesToPlanet()
        {
            if (planet == null || planet.shapeSettings == null)
            {
                SetStatus("Cannot apply: missing Planet reference.");
                return;
            }

            if (radiusSlider != null)
            {
                planet.shapeSettings.planetRadius = radiusSlider.value;
            }

            planet.GeneratePlanet();
            UpdateRadiusValueLabel();
            SetStatus("Applied settings. " + GetGenerationTimeStatus());
        }

        public void SaveCurrentSlot()
        {
            if (planet == null)
            {
                SetStatus("Cannot save: missing Planet reference.");
                return;
            }

            if (!TryGetSlot(out int slot))
            {
                return;
            }

            ApplyBasicValuesToPlanet();
            if (textureSettingsUi != null) textureSettingsUi.ApplyAllFromUi();
            PlanetRuntimeSimplePresetData data = PlanetRuntimeSimplePresetMapper.Capture(planet);
            bool ok = PlanetRuntimeSimplePresetStorage.SaveSlot(slot, data, out string error);
            SetStatus(ok ? $"Saved slot {slot+1}." : $"Save failed: {error}");
        }

        public void LoadCurrentSlot()
        {
            if (planet == null)
            {
                SetStatus("Cannot load: missing Planet reference.");
                return;
            }

            if (!TryGetSlot(out int slot))
            {
                return;
            }

            if (textureSettingsUi != null) textureSettingsUi.RegisterTextureOptions();
            bool ok = PlanetRuntimeSimplePresetStorage.LoadSlot(slot, out PlanetRuntimeSimplePresetData data, out string error);
            if (!ok)
            {
                SetStatus($"Load failed: {error}");
                return;
            }

            PlanetRuntimeSimplePresetMapper.Apply(planet, data);
            planet.GeneratePlanet();
            RefreshUiFromPlanet();
            SetStatus($"Loaded slot {slot+1}. {GetGenerationTimeStatus()}");
        }

        public void CopyCurrentPlanet()
        {
            if (planet == null)
            {
                SetStatus("Cannot copy: missing Planet reference.");
                return;
            }

            ApplyBasicValuesToPlanet();
            if (textureSettingsUi != null) textureSettingsUi.ApplyAllFromUi();
            PlanetRuntimeClipboard.CopyFrom(planet);
            SetStatus("Copied planet.");
        }

        public void PasteToCurrentSlot()
        {
            if (planet == null)
            {
                SetStatus("Cannot paste: missing Planet reference.");
                return;
            }

            if (!PlanetRuntimeClipboard.HasData)
            {
                SetStatus("Clipboard is empty.");
                return;
            }

            if (!PlanetRuntimeClipboard.PasteTo(planet))
            {
                SetStatus("Paste failed.");
                return;
            }

            RefreshUiFromPlanet();
            SetStatus("Pasted planet. Save manually to keep it.");
        }

        public void UpdateRadiusValueLabel()
        {
            if (radiusSlider != null && radiusValueText != null)
            {
                radiusValueText.text = "Radius: " + radiusSlider.value.ToString("0");
            }
        }

        public void OnRadiusSliderChanged(float _)
        {
            UpdateRadiusValueLabel();
            if (planet != null && planet.autoUpdate)
            {
                ApplyBasicValuesToPlanet();
            }
        }

        public void OnLodQualityChanged(int index)
        {
            if (planet == null)
            {
                return;
            }

            ApplyLodQualityPreset(index);
            planet.GeneratePlanet();

            SetStatus($"LOD preset: {GetLodPresetName(index)}");
        }

        public void OnAutoUpdateChanged(bool value)
        {
            if (planet == null)
            {
                return;
            }

            planet.autoUpdate = value;
            SetStatus(value ? "Auto Update enabled." : "Auto Update disabled.");
        }

        public void OnSlotChanged(int _)
        {
            if (isRefreshingUi)
            {
                return;
            }

            LoadCurrentSlot();
        }

        private void TryLoadInitialSlot()
        {
            if (planet == null) return;

            if (slotDropdown != null)
            {
                slotDropdown.SetValueWithoutNotify(0);
            }

            if (textureSettingsUi != null) textureSettingsUi.RegisterTextureOptions();
            bool ok = PlanetRuntimeSimplePresetStorage.LoadSlot(0, out PlanetRuntimeSimplePresetData data, out _);
            if (!ok) return;

            PlanetRuntimeSimplePresetMapper.Apply(planet, data);
            planet.GeneratePlanet();
            RefreshUiFromPlanet();
            didInitialLoad = true;
            SetStatus("Loaded slot 1. " + GetGenerationTimeStatus());
        }

        private bool TryGetSlot(out int slot)
        {
            slot = 0;
            if (slotDropdown == null)
            {
                SetStatus("Slot dropdown is not assigned.");
                return false;
            }

            slot = slotDropdown.value;
            return true;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private string GetGenerationTimeStatus()
        {
            if (planet == null)
            {
                return string.Empty;
            }

            return $"Generation: {planet.LastGenerationTimeMs:0.00} ms.";
        }

        private void ApplyLodQualityPreset(int index)
        {
            PlanetLodPresetUtility.ApplyPreset(planet, index);
        }

        private string GetLodPresetName(int index)
        {
            return PlanetLodPresetUtility.GetPresetName(index);
        }
    }
}
