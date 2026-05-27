using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProceduralPlanet
{
    public class PlanetGenerationBenchmark : MonoBehaviour
    {
        public enum SettingsScenario
        {
            Baseline,
            NoiseOnly,
            ColorOnly,
            TextureOnly,
            NoiseAndColor,
            NoiseAndTexture,
            ColorAndTexture,
            AllSettings
        }

        [Header("Target")]
        [SerializeField] private Planet planet;

        [Header("Benchmark")]
        [SerializeField] private bool autoStart;
        [SerializeField] private int[] lodPresetIndices =
        {
            PlanetLodPresetUtility.High,
            PlanetLodPresetUtility.Medium,
            PlanetLodPresetUtility.Low
        };
        [SerializeField] private SettingsScenario[] scenarios =
        {
            SettingsScenario.NoiseOnly,
            SettingsScenario.ColorOnly,
            SettingsScenario.TextureOnly,
            SettingsScenario.NoiseAndColor,
            SettingsScenario.NoiseAndTexture,
            SettingsScenario.ColorAndTexture,
            SettingsScenario.AllSettings
        };
        [Min(0)]
        [SerializeField] private int warmupRuns = 3;
        [Min(1)]
        [SerializeField] private int repetitionsPerCase = 20;
        [Min(0f)]
        [SerializeField] private float delayBetweenRuns = 0.05f;

        [Header("Output")]
        [SerializeField] private bool saveIndividualRuns = true;
        [SerializeField] private string resultFileName = "planet_generation_benchmark.csv";
        [SerializeField] private string summaryFileName = "planet_generation_benchmark_summary.csv";

        private bool isRunning;
        private OriginalSettings originalSettings;

        private void Start()
        {
            if (autoStart)
            {
                BeginBenchmark();
            }
        }

        [ContextMenu("Begin Generation Benchmark")]
        public void BeginBenchmark()
        {
            if (isRunning)
            {
                return;
            }

            if (planet == null)
            {
                planet = FindFirstObjectByType<Planet>();
            }

            if (planet == null)
            {
                Debug.LogError("PlanetGenerationBenchmark: Planet reference is missing.", this);
                return;
            }

            StartCoroutine(RunBenchmark());
        }

        private IEnumerator RunBenchmark()
        {
            isRunning = true;
            originalSettings = OriginalSettings.Capture(planet);

            try
            {
                for (int lodIndex = 0; lodIndex < lodPresetIndices.Length; lodIndex++)
                {
                    int lodPreset = lodPresetIndices[lodIndex];
                    PlanetLodPresetUtility.ApplyPreset(planet, lodPreset);
                    string lodName = PlanetLodPresetUtility.GetPresetName(lodPreset);

                    for (int warmup = 1; warmup <= warmupRuns; warmup++)
                    {
                        for (int offset = 0; offset < scenarios.Length; offset++)
                        {
                            int scenarioIndex = (warmup - 1 + offset) % scenarios.Length;
                            SettingsScenario scenario = scenarios[scenarioIndex];
                            originalSettings.RestorePlanetSettings(planet);
                            ApplyScenarioMutation(scenario, warmup);
                            planet.GeneratePlanet();
                            yield return WaitBetweenRuns();
                        }
                    }

                    MeasurementStats[] stats = new MeasurementStats[scenarios.Length];

                    for (int repetition = 1; repetition <= repetitionsPerCase; repetition++)
                    {
                        for (int offset = 0; offset < scenarios.Length; offset++)
                        {
                            int scenarioIndex = (repetition - 1 + offset) % scenarios.Length;
                            SettingsScenario scenario = scenarios[scenarioIndex];
                            originalSettings.RestorePlanetSettings(planet);
                            ApplyScenarioMutation(scenario, repetition + warmupRuns);
                            planet.GeneratePlanet();

                            float timeMs = planet.LastGenerationTimeMs;
                            stats[scenarioIndex].Add(timeMs, planet.LastGeneratedFaceCount, planet.LastGeneratedVertexCount);

                            if (saveIndividualRuns)
                            {
                                SaveRun(
                                    lodName,
                                    scenario,
                                    repetition,
                                    timeMs,
                                    planet.LastGeneratedFaceCount,
                                    planet.LastGeneratedVertexCount);
                            }

                            yield return WaitBetweenRuns();
                        }
                    }

                    for (int scenarioIndex = 0; scenarioIndex < scenarios.Length; scenarioIndex++)
                    {
                        SettingsScenario scenario = scenarios[scenarioIndex];
                        MeasurementStats scenarioStats = stats[scenarioIndex];
                        SaveSummary(
                            lodName,
                            scenario,
                            scenarioStats.Average,
                            scenarioStats.Min,
                            scenarioStats.Max,
                            scenarioStats.Faces,
                            scenarioStats.Vertices);
                        Debug.Log(
                            $"Generation benchmark {lodName}/{scenario}: avg={scenarioStats.Average:0.00} ms, " +
                            $"min={scenarioStats.Min:0.00} ms, max={scenarioStats.Max:0.00} ms, " +
                            $"vertices={scenarioStats.Vertices}",
                            this);
                    }
                }
            }
            finally
            {
                originalSettings.RestoreAll(planet);
                isRunning = false;
                Debug.Log($"Generation benchmark finished. Results saved to {Application.persistentDataPath}", this);
            }
        }

        private void ApplyScenarioMutation(SettingsScenario scenario, int step)
        {
            bool mutateNoise = scenario == SettingsScenario.NoiseOnly ||
                scenario == SettingsScenario.NoiseAndColor ||
                scenario == SettingsScenario.NoiseAndTexture ||
                scenario == SettingsScenario.AllSettings;

            bool mutateColor = scenario == SettingsScenario.ColorOnly ||
                scenario == SettingsScenario.NoiseAndColor ||
                scenario == SettingsScenario.ColorAndTexture ||
                scenario == SettingsScenario.AllSettings;

            bool mutateTexture = scenario == SettingsScenario.TextureOnly ||
                scenario == SettingsScenario.NoiseAndTexture ||
                scenario == SettingsScenario.ColorAndTexture ||
                scenario == SettingsScenario.AllSettings;

            if (mutateNoise)
            {
                MutateNoiseSettings(step);
            }

            if (mutateColor)
            {
                MutateColorSettings(step);
            }

            if (mutateTexture)
            {
                MutateTextureSettings(step);
            }
        }

        private void MutateNoiseSettings(int step)
        {
            if (planet.shapeSettings == null || planet.shapeSettings.noiseLayers == null)
            {
                return;
            }

            for (int i = 0; i < planet.shapeSettings.noiseLayers.Length; i++)
            {
                ShapeSettings.NoiseLayer layer = planet.shapeSettings.noiseLayers[i];
                if (layer == null || layer.noiseSettings == null)
                {
                    continue;
                }

                NoiseSettings.SimpleNoiseSettings settings = GetActiveNoiseSettings(layer.noiseSettings);
                if (settings == null)
                {
                    continue;
                }

                float phase = step + i * 0.37f;
                settings.centre = new Vector3(phase * 0.17f, phase * 0.11f, phase * 0.07f);
                settings.strength = 0.35f + 0.08f * ((step + i) % 5);
                settings.baseRoughness = 0.8f + 0.15f * ((step + i) % 4);
                settings.roughness = 1.8f + 0.1f * ((step + i) % 5);
                settings.numLayers = Mathf.Clamp(2 + ((step + i) % 5), 1, 8);
            }
        }

        private void MutateColorSettings(int step)
        {
            if (planet.colorSettings == null || planet.colorSettings.biomeSettings == null)
            {
                return;
            }

            planet.colorSettings.biomeSettings.blendAmount = 0.05f + 0.03f * (step % 8);

            ColorSettings.BiomeSettings.Biome[] biomes = planet.colorSettings.biomeSettings.biomes;
            if (biomes != null)
            {
                for (int i = 0; i < biomes.Length; i++)
                {
                    if (biomes[i] == null)
                    {
                        continue;
                    }

                    float baseHeight = biomes.Length <= 1 ? 0f : i / (float)(biomes.Length - 1);
                    biomes[i].startHeight = Mathf.Clamp01(baseHeight + Mathf.Sin((step + i) * 0.7f) * 0.03f);
                    biomes[i].tint = Color.HSVToRGB(Mathf.Repeat(0.1f * i + step * 0.025f, 1f), 0.55f, 0.95f);
                }
            }

            NoiseSettings temperatureNoise = planet.colorSettings.biomeSettings.temperatureNoise;
            if (temperatureNoise != null)
            {
                NoiseSettings.SimpleNoiseSettings settings = GetActiveNoiseSettings(temperatureNoise);
                if (settings != null)
                {
                    settings.centre = new Vector3(step * 0.09f, step * 0.05f, step * 0.03f);
                    settings.numLayers = Mathf.Clamp(1 + (step % 4), 1, 8);
                }
            }
        }

        private void MutateTextureSettings(int step)
        {
            if (planet.colorSettings == null)
            {
                return;
            }

            MutateTerrainTexture(planet.colorSettings.sand, step, 0);
            MutateTerrainTexture(planet.colorSettings.grass, step, 1);
            MutateTerrainTexture(planet.colorSettings.mountain, step, 2);
            MutateTerrainTexture(planet.colorSettings.snow, step, 3);

            if (planet.colorSettings.oceanSettings != null)
            {
                planet.colorSettings.oceanSettings.waveSpeed = 0.01f + 0.005f * (step % 6);
                planet.colorSettings.oceanSettings.waveScale = 0.35f + 0.04f * (step % 5);
                planet.colorSettings.oceanSettings.normalStrength = 0.5f + 0.1f * (step % 5);
            }

        }

        private void MutateTerrainTexture(ColorSettings.TerrainTextureSettings settings, int step, int index)
        {
            if (settings == null)
            {
                return;
            }

            settings.enabled = (step + index) % 3 != 0;
            settings.tiling = 6f + 2f * ((step + index) % 7);
            settings.normalStrength = 0.35f + 0.1f * ((step + index) % 6);
        }

        private NoiseSettings.SimpleNoiseSettings GetActiveNoiseSettings(NoiseSettings noiseSettings)
        {
            if (noiseSettings.filterType == NoiseSettings.FilterType.Ridgid)
            {
                return noiseSettings.ridgidNoiseSettings;
            }

            return noiseSettings.simpleNoiseSettings;
        }

        private IEnumerator WaitBetweenRuns()
        {
            if (delayBetweenRuns <= 0f)
            {
                yield return null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(delayBetweenRuns);
        }

        private void SaveRun(string lodPreset, SettingsScenario scenario, int repetition, float timeMs, int faces, int vertices)
        {
            string filePath = Path.Combine(Application.persistentDataPath, resultFileName);
            bool writeHeader = !File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                if (writeHeader)
                {
                    writer.WriteLine("timestamp,scene,lod_preset,scenario,resolution,faces,vertices,repetition,time_ms");
                }

                writer.WriteLine(
                    GetTimestamp() + "," +
                    SceneManager.GetActiveScene().name + "," +
                    lodPreset + "," +
                    scenario + "," +
                    planet.resolution.ToString(CultureInfo.InvariantCulture) + "," +
                    faces.ToString(CultureInfo.InvariantCulture) + "," +
                    vertices.ToString(CultureInfo.InvariantCulture) + "," +
                    repetition.ToString(CultureInfo.InvariantCulture) + "," +
                    timeMs.ToString("0.00", CultureInfo.InvariantCulture));
            }
        }

        private void SaveSummary(string lodPreset, SettingsScenario scenario, float averageMs, float minMs, float maxMs, int faces, int vertices)
        {
            string filePath = Path.Combine(Application.persistentDataPath, summaryFileName);
            bool writeHeader = !File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                if (writeHeader)
                {
                    writer.WriteLine("timestamp,scene,lod_preset,scenario,resolution,faces,vertices,repetitions,avg_ms,min_ms,max_ms");
                }

                writer.WriteLine(
                    GetTimestamp() + "," +
                    SceneManager.GetActiveScene().name + "," +
                    lodPreset + "," +
                    scenario + "," +
                    planet.resolution.ToString(CultureInfo.InvariantCulture) + "," +
                    faces.ToString(CultureInfo.InvariantCulture) + "," +
                    vertices.ToString(CultureInfo.InvariantCulture) + "," +
                    repetitionsPerCase.ToString(CultureInfo.InvariantCulture) + "," +
                    averageMs.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                    minMs.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                    maxMs.ToString("0.00", CultureInfo.InvariantCulture));
            }
        }

        private string GetTimestamp()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private struct MeasurementStats
        {
            private float sum;
            private int count;

            public float Min { get; private set; }
            public float Max { get; private set; }
            public int Faces { get; private set; }
            public int Vertices { get; private set; }
            public float Average => count > 0 ? sum / count : 0f;

            public void Add(float timeMs, int faces, int vertices)
            {
                if (count == 0)
                {
                    Min = timeMs;
                    Max = timeMs;
                }
                else
                {
                    Min = Mathf.Min(Min, timeMs);
                    Max = Mathf.Max(Max, timeMs);
                }

                sum += timeMs;
                count++;
                Faces = faces;
                Vertices = vertices;
            }
        }

        private struct OriginalSettings
        {
            private int resolution;
            private Planet.LODLevel[] lodLevels;
            private float planetRadius;
            private bool[] noiseEnabled;
            private NoiseSnapshot[] shapeNoise;
            private float biomeBlendAmount;
            private float[] biomeStartHeights;
            private Color[] biomeTints;
            private NoiseSnapshot temperatureNoise;
            private TerrainTextureSnapshot sand;
            private TerrainTextureSnapshot grass;
            private TerrainTextureSnapshot mountain;
            private TerrainTextureSnapshot snow;
            private OceanSnapshot ocean;

            public static OriginalSettings Capture(Planet planet)
            {
                OriginalSettings snapshot = new OriginalSettings
                {
                    resolution = planet.resolution,
                    lodLevels = CopyLodLevels(planet.lodLevels)
                };

                if (planet.shapeSettings != null)
                {
                    snapshot.planetRadius = planet.shapeSettings.planetRadius;
                    ShapeSettings.NoiseLayer[] layers = planet.shapeSettings.noiseLayers;
                    if (layers != null)
                    {
                        snapshot.noiseEnabled = new bool[layers.Length];
                        snapshot.shapeNoise = new NoiseSnapshot[layers.Length];
                        for (int i = 0; i < layers.Length; i++)
                        {
                            snapshot.noiseEnabled[i] = layers[i] != null && layers[i].enabled;
                            snapshot.shapeNoise[i] = NoiseSnapshot.Capture(layers[i]?.noiseSettings);
                        }
                    }
                }

                if (planet.colorSettings != null)
                {
                    snapshot.sand = TerrainTextureSnapshot.Capture(planet.colorSettings.sand);
                    snapshot.grass = TerrainTextureSnapshot.Capture(planet.colorSettings.grass);
                    snapshot.mountain = TerrainTextureSnapshot.Capture(planet.colorSettings.mountain);
                    snapshot.snow = TerrainTextureSnapshot.Capture(planet.colorSettings.snow);
                    snapshot.ocean = OceanSnapshot.Capture(planet.colorSettings.oceanSettings);

                    ColorSettings.BiomeSettings biomeSettings = planet.colorSettings.biomeSettings;
                    if (biomeSettings != null)
                    {
                        snapshot.biomeBlendAmount = biomeSettings.blendAmount;
                        snapshot.temperatureNoise = NoiseSnapshot.Capture(biomeSettings.temperatureNoise);

                        ColorSettings.BiomeSettings.Biome[] biomes = biomeSettings.biomes;
                        if (biomes != null)
                        {
                            snapshot.biomeStartHeights = new float[biomes.Length];
                            snapshot.biomeTints = new Color[biomes.Length];
                            for (int i = 0; i < biomes.Length; i++)
                            {
                                if (biomes[i] == null)
                                {
                                    continue;
                                }

                                snapshot.biomeStartHeights[i] = biomes[i].startHeight;
                                snapshot.biomeTints[i] = biomes[i].tint;
                            }
                        }
                    }
                }

                return snapshot;
            }

            public void RestoreAll(Planet planet)
            {
                planet.resolution = resolution;
                planet.lodLevels = CopyLodLevels(lodLevels);
                RestorePlanetSettings(planet);
            }

            public void RestorePlanetSettings(Planet planet)
            {
                if (planet.shapeSettings != null)
                {
                    planet.shapeSettings.planetRadius = planetRadius;
                    ShapeSettings.NoiseLayer[] layers = planet.shapeSettings.noiseLayers;
                    if (layers != null && shapeNoise != null)
                    {
                        for (int i = 0; i < layers.Length && i < shapeNoise.Length; i++)
                        {
                            if (layers[i] == null)
                            {
                                continue;
                            }

                            if (noiseEnabled != null && i < noiseEnabled.Length)
                            {
                                layers[i].enabled = noiseEnabled[i];
                            }

                            shapeNoise[i].Restore(layers[i].noiseSettings);
                        }
                    }
                }

                if (planet.colorSettings == null)
                {
                    return;
                }

                sand.Restore(planet.colorSettings.sand);
                grass.Restore(planet.colorSettings.grass);
                mountain.Restore(planet.colorSettings.mountain);
                snow.Restore(planet.colorSettings.snow);
                ocean.Restore(planet.colorSettings.oceanSettings);

                ColorSettings.BiomeSettings biomeSettings = planet.colorSettings.biomeSettings;
                if (biomeSettings == null)
                {
                    return;
                }

                biomeSettings.blendAmount = biomeBlendAmount;
                temperatureNoise.Restore(biomeSettings.temperatureNoise);

                ColorSettings.BiomeSettings.Biome[] biomes = biomeSettings.biomes;
                if (biomes == null || biomeStartHeights == null)
                {
                    return;
                }

                for (int i = 0; i < biomes.Length && i < biomeStartHeights.Length; i++)
                {
                    if (biomes[i] == null)
                    {
                        continue;
                    }

                    biomes[i].startHeight = biomeStartHeights[i];
                    if (biomeTints != null && i < biomeTints.Length)
                    {
                        biomes[i].tint = biomeTints[i];
                    }
                }
            }

            private static Planet.LODLevel[] CopyLodLevels(Planet.LODLevel[] source)
            {
                if (source == null)
                {
                    return null;
                }

                Planet.LODLevel[] copy = new Planet.LODLevel[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    copy[i] = source[i];
                }

                return copy;
            }
        }

        private struct NoiseSnapshot
        {
            private NoiseSettings.FilterType filterType;
            private NoiseValues simple;
            private NoiseValues ridgid;
            private float weightMultiplier;

            public static NoiseSnapshot Capture(NoiseSettings settings)
            {
                if (settings == null)
                {
                    return default;
                }

                return new NoiseSnapshot
                {
                    filterType = settings.filterType,
                    simple = NoiseValues.Capture(settings.simpleNoiseSettings),
                    ridgid = NoiseValues.Capture(settings.ridgidNoiseSettings),
                    weightMultiplier = settings.ridgidNoiseSettings != null ? settings.ridgidNoiseSettings.weightMultiplier : 0f
                };
            }

            public void Restore(NoiseSettings settings)
            {
                if (settings == null)
                {
                    return;
                }

                settings.filterType = filterType;
                simple.Restore(settings.simpleNoiseSettings);
                ridgid.Restore(settings.ridgidNoiseSettings);
                if (settings.ridgidNoiseSettings != null)
                {
                    settings.ridgidNoiseSettings.weightMultiplier = weightMultiplier;
                }
            }
        }

        private struct NoiseValues
        {
            private float strength;
            private int numLayers;
            private float baseRoughness;
            private float roughness;
            private float persistence;
            private Vector3 centre;
            private float minValue;

            public static NoiseValues Capture(NoiseSettings.SimpleNoiseSettings settings)
            {
                if (settings == null)
                {
                    return default;
                }

                return new NoiseValues
                {
                    strength = settings.strength,
                    numLayers = settings.numLayers,
                    baseRoughness = settings.baseRoughness,
                    roughness = settings.roughness,
                    persistence = settings.persistence,
                    centre = settings.centre,
                    minValue = settings.minValue
                };
            }

            public void Restore(NoiseSettings.SimpleNoiseSettings settings)
            {
                if (settings == null)
                {
                    return;
                }

                settings.strength = strength;
                settings.numLayers = numLayers;
                settings.baseRoughness = baseRoughness;
                settings.roughness = roughness;
                settings.persistence = persistence;
                settings.centre = centre;
                settings.minValue = minValue;
            }
        }

        private struct TerrainTextureSnapshot
        {
            private bool enabled;
            private float normalStrength;
            private float tiling;

            public static TerrainTextureSnapshot Capture(ColorSettings.TerrainTextureSettings settings)
            {
                if (settings == null)
                {
                    return default;
                }

                return new TerrainTextureSnapshot
                {
                    enabled = settings.enabled,
                    normalStrength = settings.normalStrength,
                    tiling = settings.tiling
                };
            }

            public void Restore(ColorSettings.TerrainTextureSettings settings)
            {
                if (settings == null)
                {
                    return;
                }

                settings.enabled = enabled;
                settings.normalStrength = normalStrength;
                settings.tiling = tiling;
            }
        }

        private struct OceanSnapshot
        {
            private float waveSpeed;
            private float waveScale;
            private float normalStrength;

            public static OceanSnapshot Capture(ColorSettings.OceanSettings settings)
            {
                if (settings == null)
                {
                    return default;
                }

                return new OceanSnapshot
                {
                    waveSpeed = settings.waveSpeed,
                    waveScale = settings.waveScale,
                    normalStrength = settings.normalStrength
                };
            }

            public void Restore(ColorSettings.OceanSettings settings)
            {
                if (settings == null)
                {
                    return;
                }

                settings.waveSpeed = waveSpeed;
                settings.waveScale = waveScale;
                settings.normalStrength = normalStrength;
            }
        }

    }
}
