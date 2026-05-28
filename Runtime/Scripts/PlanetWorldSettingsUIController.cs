using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetWorldSettingsUIController : MonoBehaviour
    {
        [Header("Targets")]
        public Planet planet;
        public OrbitCamera orbitCamera;
        public Light sunLight;

        [Header("LOD")]
        public TMP_Dropdown lodQualityDropdown;

        [Header("Camera")]
        public Button cameraFoldoutButton;
        public TextMeshProUGUI cameraFoldoutArrowText;
        public GameObject cameraSettingsRoot;
        public TMP_InputField minDistanceInput;
        public TMP_InputField maxDistanceInput;
        public Toggle autoRotateToggle;
        public TMP_InputField autoRotateSpeedInput;

        [Header("Sun")]
        public Button sunFoldoutButton;
        public TextMeshProUGUI sunFoldoutArrowText;
        public GameObject sunSettingsRoot;
        public Slider sunYawSlider;
        public TextMeshProUGUI sunYawText;
        public Slider sunPitchSlider;
        public TextMeshProUGUI sunPitchText;

        [Header("Status")]
        public TextMeshProUGUI statusText;

        private bool isRefreshingUi;
        private bool cameraExpanded;
        private bool sunExpanded;

        private void Start()
        {
            ResolveTargets();

            if (lodQualityDropdown != null) lodQualityDropdown.onValueChanged.AddListener(OnLodQualityChanged);
            if (cameraFoldoutButton != null) cameraFoldoutButton.onClick.AddListener(ToggleCameraExpanded);
            if (sunFoldoutButton != null) sunFoldoutButton.onClick.AddListener(ToggleSunExpanded);
            if (minDistanceInput != null) minDistanceInput.onEndEdit.AddListener(OnMinDistanceChanged);
            if (maxDistanceInput != null) maxDistanceInput.onEndEdit.AddListener(OnMaxDistanceChanged);
            if (autoRotateToggle != null) autoRotateToggle.onValueChanged.AddListener(OnAutoRotateChanged);
            if (autoRotateSpeedInput != null) autoRotateSpeedInput.onEndEdit.AddListener(OnAutoRotateSpeedChanged);
            if (sunYawSlider != null) sunYawSlider.onValueChanged.AddListener(OnSunChanged);
            if (sunPitchSlider != null) sunPitchSlider.onValueChanged.AddListener(OnSunChanged);

            RefreshFromTargets();
        }

        public void RefreshFromTargets()
        {
            ResolveTargets();
            isRefreshingUi = true;

            if (orbitCamera != null)
            {
                SetText(minDistanceInput, orbitCamera.minDistance);
                SetText(maxDistanceInput, orbitCamera.maxDistance);
                if (autoRotateToggle != null) autoRotateToggle.SetIsOnWithoutNotify(orbitCamera.autoRotate);
                SetText(autoRotateSpeedInput, orbitCamera.autoRotateSpeed);
            }

            if (sunLight != null)
            {
                Vector3 angles = sunLight.transform.eulerAngles;
                if (sunYawSlider != null) sunYawSlider.SetValueWithoutNotify(NormalizeSignedAngle(angles.y));
                if (sunPitchSlider != null) sunPitchSlider.SetValueWithoutNotify(NormalizeSignedAngle(angles.x));
                UpdateSunLabels();
            }

            ApplyFoldoutStates();
            isRefreshingUi = false;
        }

        private void OnLodQualityChanged(int index)
        {
            if (isRefreshingUi || planet == null) return;

            PlanetLodPresetUtility.ApplyPreset(planet, index);
            planet.GeneratePlanet();
            SetStatus($"LOD: {PlanetLodPresetUtility.GetPresetName(index)}");
        }

        private void OnMinDistanceChanged(string value)
        {
            if (isRefreshingUi || orbitCamera == null) return;

            orbitCamera.minDistance = Mathf.Max(0.1f, ParseFloat(value, orbitCamera.minDistance));
            if (orbitCamera.maxDistance < orbitCamera.minDistance)
            {
                orbitCamera.maxDistance = orbitCamera.minDistance;
                SetText(maxDistanceInput, orbitCamera.maxDistance);
            }
            SetText(minDistanceInput, orbitCamera.minDistance);
        }

        private void OnMaxDistanceChanged(string value)
        {
            if (isRefreshingUi || orbitCamera == null) return;

            orbitCamera.maxDistance = Mathf.Max(orbitCamera.minDistance, ParseFloat(value, orbitCamera.maxDistance));
            SetText(maxDistanceInput, orbitCamera.maxDistance);
        }

        private void OnAutoRotateChanged(bool value)
        {
            if (isRefreshingUi || orbitCamera == null) return;

            orbitCamera.autoRotate = value;
        }

        private void OnAutoRotateSpeedChanged(string value)
        {
            if (isRefreshingUi || orbitCamera == null) return;

            orbitCamera.autoRotateSpeed = ParseFloat(value, orbitCamera.autoRotateSpeed);
            SetText(autoRotateSpeedInput, orbitCamera.autoRotateSpeed);
        }

        private void OnSunChanged(float _)
        {
            if (isRefreshingUi || sunLight == null) return;

            float pitch = sunPitchSlider != null ? sunPitchSlider.value : NormalizeSignedAngle(sunLight.transform.eulerAngles.x);
            float yaw = sunYawSlider != null ? sunYawSlider.value : NormalizeSignedAngle(sunLight.transform.eulerAngles.y);
            sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            UpdateSunLabels();
        }

        private void ToggleCameraExpanded()
        {
            cameraExpanded = !cameraExpanded;
            ApplyFoldoutStates();
            NotifyLayoutChanged();
        }

        private void ToggleSunExpanded()
        {
            sunExpanded = !sunExpanded;
            ApplyFoldoutStates();
            NotifyLayoutChanged();
        }

        private void ApplyFoldoutStates()
        {
            if (cameraSettingsRoot != null) cameraSettingsRoot.SetActive(cameraExpanded);
            if (cameraFoldoutArrowText != null) cameraFoldoutArrowText.text = cameraExpanded ? "▼" : "►";
            if (sunSettingsRoot != null) sunSettingsRoot.SetActive(sunExpanded);
            if (sunFoldoutArrowText != null) sunFoldoutArrowText.text = sunExpanded ? "▼" : "►";
        }

        private void NotifyLayoutChanged()
        {
            Canvas.ForceUpdateCanvases();
            if (transform is RectTransform rect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }

        private void ResolveTargets()
        {
            if (planet == null) planet = FindFirstObjectByType<Planet>();
            if (orbitCamera == null) orbitCamera = FindFirstObjectByType<OrbitCamera>();
            if (sunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null && lights[i].type == LightType.Directional)
                    {
                        sunLight = lights[i];
                        break;
                    }
                }
            }
        }

        private void UpdateSunLabels()
        {
            if (sunYawText != null && sunYawSlider != null) sunYawText.text = $"Yaw: {sunYawSlider.value:0}";
            if (sunPitchText != null && sunPitchSlider != null) sunPitchText.text = $"Pitch: {sunPitchSlider.value:0}";
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private static float NormalizeSignedAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        private static void SetText(TMP_InputField input, float value)
        {
            if (input != null) input.SetTextWithoutNotify(value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static float ParseFloat(string value, float fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;

            string normalized = value.Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
        }
    }
}
