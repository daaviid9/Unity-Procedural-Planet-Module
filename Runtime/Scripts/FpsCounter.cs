using TMPro;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace ProceduralPlanet
{
    public class FpsCounter : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TMP_Dropdown lodPresetDropdown;
        [SerializeField] private float displayRefreshInterval = 0.25f;

        [Header("Measurement")]
        [SerializeField] private bool autoStartMeasurement;
        [SerializeField] private float warmupSeconds = 5f;
        [SerializeField] private float measurementSeconds = 60f;
        [SerializeField] private bool logResultToConsole = true;
        [SerializeField] private bool saveResultToFile = true;
        [SerializeField] private string resultFileName = "fps_measurements.csv";

        [Header("Benchmark Camera Movement")]
        [SerializeField] private OrbitCamera orbitCamera;
        [SerializeField] private bool moveCameraDuringMeasurement;
        [SerializeField] private float benchmarkYawSpeed = 35f;
        [SerializeField] private float benchmarkBasePitch = 15f;
        [SerializeField] private float benchmarkPitchAmplitude = 12f;
        [SerializeField] private float benchmarkBaseDistance = 90f;
        [SerializeField] private float benchmarkDistanceAmplitude = 35f;
        [SerializeField] private float benchmarkCycleSeconds = 12f;

        private float displayTimer;
        private bool isMeasuring;
        private bool isWarmup;
        private float phaseTimer;
        private float measuredTime;
        private int measuredFrames;
        private float minFrameTime;
        private float maxFrameTime;
        private float benchmarkTimer;
        private float benchmarkYaw;

        private void OnEnable()
        {
            if (autoStartMeasurement)
            {
                BeginMeasurement();
            }
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            UpdateBenchmarkCameraMovement(deltaTime);
            UpdateMeasurement(deltaTime);
            UpdateDisplay(deltaTime);
        }

        [ContextMenu("Begin FPS Measurement")]
        public void BeginMeasurement()
        {
            isMeasuring = true;
            isWarmup = warmupSeconds > 0f;
            phaseTimer = 0f;
            benchmarkTimer = 0f;
            benchmarkYaw = 0f;
            ResetMeasuredValues();

            if (moveCameraDuringMeasurement && orbitCamera != null && !orbitCamera.autoRotate)
            {
                orbitCamera.SetOrbit(benchmarkYaw, benchmarkBasePitch, benchmarkBaseDistance, true);
            }
        }

        public void BeginNonStaticMeasurement()
        {
            moveCameraDuringMeasurement = true;
            BeginMeasurement();
        }

        public void SetBenchmarkCameraMovement(bool enabled)
        {
            moveCameraDuringMeasurement = enabled;
        }

        [ContextMenu("Stop FPS Measurement")]
        public void StopMeasurement()
        {
            if (!isMeasuring)
            {
                return;
            }

            FinishMeasurement();
        }

        private void ResetMeasuredValues()
        {
            measuredTime = 0f;
            measuredFrames = 0;
            minFrameTime = float.MaxValue;
            maxFrameTime = 0f;
        }

        private void UpdateMeasurement(float deltaTime)
        {
            if (!isMeasuring)
            {
                return;
            }

            phaseTimer += deltaTime;

            if (isWarmup)
            {
                if (phaseTimer >= warmupSeconds)
                {
                    isWarmup = false;
                    phaseTimer = 0f;
                    ResetMeasuredValues();
                }

                return;
            }

            measuredTime += deltaTime;
            measuredFrames++;
            minFrameTime = Mathf.Min(minFrameTime, deltaTime);
            maxFrameTime = Mathf.Max(maxFrameTime, deltaTime);

            if (measuredTime >= measurementSeconds)
            {
                FinishMeasurement();
            }
        }

        private void UpdateDisplay(float deltaTime)
        {
            if (fpsText == null)
            {
                return;
            }

            displayTimer += deltaTime;

            if (displayTimer < displayRefreshInterval)
            {
                return;
            }

            int currentFps = Mathf.RoundToInt(1f / deltaTime);
            string measurementStatus = GetMeasurementStatusText();
            fpsText.text = $"FPS: {currentFps}{measurementStatus}";
            displayTimer = 0f;
        }

        private string GetMeasurementStatusText()
        {
            if (!isMeasuring)
            {
                return $"\nMode: {GetMovementMode()}";
            }

            if (isWarmup)
            {
                float remainingWarmup = Mathf.Max(0f, warmupSeconds - phaseTimer);
                return $"\nWarmup: {remainingWarmup:0.0}s";
            }

            float averageFps = measuredTime > 0f ? measuredFrames / measuredTime : 0f;
            float remainingMeasurement = Mathf.Max(0f, measurementSeconds - measuredTime);
            return $"\nMode: {GetMovementMode()}\nAvg: {averageFps:0.0}\nTime: {remainingMeasurement:0.0}s";
        }

        private void FinishMeasurement()
        {
            isMeasuring = false;
            isWarmup = false;

            float averageFps = measuredTime > 0f ? measuredFrames / measuredTime : 0f;
            float minimumFps = maxFrameTime > 0f ? 1f / maxFrameTime : 0f;
            float maximumFps = minFrameTime < float.MaxValue ? 1f / minFrameTime : 0f;

            string result =
                $"FPS measurement finished ({measuredTime:0.00}s): " +
                $"avg={averageFps:0.00}, min={minimumFps:0.00}, max={maximumFps:0.00}, frames={measuredFrames}";
            string filePath = saveResultToFile ? SaveResult(averageFps, minimumFps, maximumFps) : string.Empty;

            if (fpsText != null)
            {
                fpsText.text =
                    $"Mode: {GetMovementMode()}\n" +
                    $"Avg FPS: {averageFps:0.0}\n" +
                    $"Min FPS: {minimumFps:0.0}\n" +
                    $"Max FPS: {maximumFps:0.0}";

                if (!string.IsNullOrEmpty(filePath))
                {
                    fpsText.text += $"\nSaved:\n{filePath}";
                }
            }

            if (logResultToConsole)
            {
                Debug.Log(result, this);
            }
        }

        private string SaveResult(float averageFps, float minimumFps, float maximumFps)
        {
            string filePath = Path.Combine(Application.persistentDataPath, resultFileName);
            bool writeHeader = !File.Exists(filePath);
            string lodPreset = GetLodPresetName();
            string movementMode = GetMovementMode();

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                if (writeHeader)
                {
                    writer.WriteLine("timestamp,scene,lod_preset,movement_mode,duration_seconds,frames,avg_fps,min_fps,max_fps");
                }

                writer.WriteLine(
                    System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "," +
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "," +
                    lodPreset + "," +
                    movementMode + "," +
                    measuredTime.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                    measuredFrames.ToString(CultureInfo.InvariantCulture).PadLeft(10) + "," +
                    averageFps.ToString("0.00", CultureInfo.InvariantCulture).PadLeft(11) + "," +
                    minimumFps.ToString("0.00", CultureInfo.InvariantCulture).PadLeft(11) + "," +
                    maximumFps.ToString("0.00", CultureInfo.InvariantCulture).PadLeft(11));
            }

            return filePath;
        }

        private string GetLodPresetName()
        {
            if (lodPresetDropdown == null ||
                lodPresetDropdown.options == null ||
                lodPresetDropdown.options.Count == 0)
            {
                return "Unknown";
            }

            int index = Mathf.Clamp(lodPresetDropdown.value, 0, lodPresetDropdown.options.Count - 1);
            string optionText = lodPresetDropdown.options[index].text;

            return string.IsNullOrWhiteSpace(optionText) ? "Unknown" : optionText.Trim();
        }

        private void UpdateBenchmarkCameraMovement(float deltaTime)
        {
            if (!isMeasuring || !moveCameraDuringMeasurement || orbitCamera == null || orbitCamera.autoRotate)
            {
                return;
            }

            benchmarkTimer += deltaTime;
            benchmarkYaw += benchmarkYawSpeed * deltaTime;

            float cycle = Mathf.Max(0.1f, benchmarkCycleSeconds);
            float normalizedTime = benchmarkTimer / cycle;
            float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f);
            float pitch = benchmarkBasePitch + wave * benchmarkPitchAmplitude;
            float distance = benchmarkBaseDistance + wave * benchmarkDistanceAmplitude;

            orbitCamera.SetOrbit(benchmarkYaw, pitch, distance);
        }

        private string GetMovementMode()
        {
            if (orbitCamera != null && orbitCamera.autoRotate)
            {
                return "AutoRotate";
            }

            if (moveCameraDuringMeasurement)
            {
                return "NonStatic";
            }

            return "Static";
        }
    }
}
