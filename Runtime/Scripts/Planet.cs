using UnityEngine;
using Unity.Collections;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ProceduralPlanet
{
    public class Planet : MonoBehaviour
    {
        private const int FaceCount = 6;
        private const int BackFaceResolution = 2;
        private const int MaxFacesUpdatedPerFrame = 1;
        private const float BackFaceDotThreshold = -0.4f;

        private static readonly Vector3[] FaceDirections =
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        [System.Serializable]
        public struct LODLevel
        {
            [Range(2, 256)]
            public int resolution;
            public float distance;
        }

        [Header("LOD Settings")]
        public Transform viewer;
        public LODLevel[] lodLevels;

        [Range(2, 256)]
        public int resolution = 10;
        public bool autoUpdate = true;
        public enum FaceRenderMask { All, Top, Bottom, Left, Right, Front, Back }
        public FaceRenderMask faceRenderMask;

        public ShapeSettings shapeSettings;
        public ColorSettings colorSettings;
        public Material planetMaterial;

        [Header("Generation Profiling")]
        [SerializeField]
        private bool logGenerationTime = true;
        [SerializeField, HideInInspector]
        private float lastGenerationTimeMs;
        [SerializeField, HideInInspector]
        private int lastGeneratedVertexCount;
        [SerializeField, HideInInspector]
        private int lastGeneratedFaceCount;

        public float LastGenerationTimeMs => lastGenerationTimeMs;
        public float LastGenerationTimeSeconds => lastGenerationTimeMs / 1000f;
        public int LastGeneratedVertexCount => lastGeneratedVertexCount;
        public int LastGeneratedFaceCount => lastGeneratedFaceCount;

        [Header("Preset Settings")]
        public PlanetPresetDatabase presetDatabase;
        [Min(0)]
        public int presetSlot;

        [HideInInspector]
        public bool shapeSettingsFoldout;
        [HideInInspector]
        public bool colorSettingsFoldout;

        private ShapeGenerator shapeGenerator;
        private ColorGenerator colorGenerator;

        [SerializeField, HideInInspector]
        private MeshFilter[] meshFilters;
        private TerrainFace[] terrainFaces;
        private Texture2D generatedGradientTexture;

        private NativeArray<NoiseLayerStruct> shapeLayersNative;
        private NativeArray<BiomeStruct> biomesNative;

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (shapeSettings != null)
            {
                shapeSettings = Instantiate(shapeSettings);
            }

            if (colorSettings != null)
            {
                colorSettings = Instantiate(colorSettings);
            }

            if (planetMaterial != null)
            {
                planetMaterial = Instantiate(planetMaterial);
            }
        }

        private void Start()
        {
            GeneratePlanet();
        }

        private void Update()
        {
            if (viewer == null || terrainFaces == null || terrainFaces.Length == 0 || meshFilters == null || !HasValidGenerationSettings(false))
            {
                return;
            }

            UpdateSettingsNative();
            UpdateVisibleFaceLod();
        }

        private void UpdateVisibleFaceLod()
        {
            int facesUpdatedThisFrame = 0;

            for (int i = 0; i < FaceCount; i++)
            {
                if (meshFilters[i] == null || !meshFilters[i].gameObject.activeSelf)
                {
                    continue;
                }

                int targetResolution = GetTargetResolutionForFace(i);
                if (terrainFaces[i].resolution_property == targetResolution || facesUpdatedThisFrame >= MaxFacesUpdatedPerFrame)
                {
                    continue;
                }

                terrainFaces[i].UpdateResolution(targetResolution);
                terrainFaces[i].ConstructMesh(shapeLayersNative, biomesNative, GetTempNoiseStruct(), colorSettings.biomeSettings.blendAmount, shapeSettings.planetRadius);

                // Reuse the current height range to avoid visible flicker during LOD updates.
                terrainFaces[i].UpdateUVs(shapeGenerator.minElevationHeight, shapeGenerator.maxElevationHeight);
                facesUpdatedThisFrame++;
            }
        }

        private int GetTargetResolutionForFace(int faceIndex)
        {
            Vector3 faceWorldCenter = transform.TransformPoint(terrainFaces[faceIndex].localup_for_lod * shapeSettings.planetRadius);
            float distanceToViewer = Vector3.Distance(faceWorldCenter, viewer.position);

            Vector3 dirToFace = (faceWorldCenter - transform.position).normalized;
            Vector3 dirToViewer = (viewer.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(dirToFace, dirToViewer);

            return dotProduct > BackFaceDotThreshold
                ? GetResolutionForDistance(distanceToViewer)
                : BackFaceResolution;
        }

        private int GetResolutionForDistance(float distanceToViewer)
        {
            if (lodLevels == null || lodLevels.Length == 0)
            {
                return resolution;
            }

            int fallbackResolution = lodLevels[lodLevels.Length - 1].resolution;
            for (int lvl = 0; lvl < lodLevels.Length; lvl++)
            {
                if (distanceToViewer < lodLevels[lvl].distance)
                {
                    return lodLevels[lvl].resolution;
                }
            }

            return fallbackResolution;
        }

        private void UpdateSettingsNative()
        {
            if (!shapeLayersNative.IsCreated || shapeLayersNative.Length != shapeSettings.noiseLayers.Length)
            {
                if (shapeLayersNative.IsCreated) shapeLayersNative.Dispose();
                shapeLayersNative = new NativeArray<NoiseLayerStruct>(shapeSettings.noiseLayers.Length, Allocator.Persistent);
            }

            if (!biomesNative.IsCreated || biomesNative.Length != colorSettings.biomeSettings.biomes.Length)
            {
                if (biomesNative.IsCreated) biomesNative.Dispose();
                biomesNative = new NativeArray<BiomeStruct>(colorSettings.biomeSettings.biomes.Length, Allocator.Persistent);
            }

            for (int i = 0; i < shapeSettings.noiseLayers.Length; i++)
            {
                shapeLayersNative[i] = BuildNoiseLayerStruct(shapeSettings.noiseLayers[i]);
            }

            for (int i = 0; i < colorSettings.biomeSettings.biomes.Length; i++)
            {
                biomesNative[i] = new BiomeStruct
                {
                    startHeight = colorSettings.biomeSettings.biomes[i].startHeight
                };
            }
        }

        private NoiseLayerStruct BuildNoiseLayerStruct(ShapeSettings.NoiseLayer layer)
        {
            if (layer == null || layer.noiseSettings == null)
            {
                return new NoiseLayerStruct { enabled = false, weightMultiplier = 1f };
            }

            NoiseSettings settings = layer.noiseSettings;
            bool isRidgid = settings.filterType == NoiseSettings.FilterType.Ridgid;
            NoiseSettings.SimpleNoiseSettings values = isRidgid
                ? settings.ridgidNoiseSettings
                : settings.simpleNoiseSettings;

            if (values == null)
            {
                return new NoiseLayerStruct { enabled = false, weightMultiplier = 1f };
            }

            return new NoiseLayerStruct
            {
                enabled = layer.enabled,
                useFirstLayerAsMask = layer.useFirstLayerAsMask,
                strength = values.strength,
                numLayers = values.numLayers,
                baseRoughness = values.baseRoughness,
                roughness = values.roughness,
                persistence = values.persistence,
                centre = values.centre,
                minValue = values.minValue,
                filterType = (int)settings.filterType,
                weightMultiplier = isRidgid ? settings.ridgidNoiseSettings.weightMultiplier : 1f
            };
        }

        private NoiseLayerStruct GetTempNoiseStruct()
        {
            NoiseSettings.SimpleNoiseSettings settings = colorSettings.biomeSettings.temperatureNoise.simpleNoiseSettings;
            return new NoiseLayerStruct
            {
                strength = settings.strength,
                numLayers = settings.numLayers,
                baseRoughness = settings.baseRoughness,
                roughness = settings.roughness,
                persistence = settings.persistence,
                centre = settings.centre,
                minValue = settings.minValue,
                weightMultiplier = 1f
            };
        }

        public void GeneratePlanet()
        {
            if (!HasValidGenerationSettings(true))
            {
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Initialize();
            GenerateMesh();
            GenerateColors();

            stopwatch.Stop();
            StoreGenerationMeasurement(stopwatch);
        }

        public void OnPlanetSettingsUpdated()
        {
            if (autoUpdate)
            {
                GeneratePlanet();
            }
        }

        private void StoreGenerationMeasurement(Stopwatch stopwatch)
        {
            lastGenerationTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            lastGeneratedFaceCount = CountActiveFaces();
            lastGeneratedVertexCount = CountGeneratedVertices();

            if (logGenerationTime)
            {
                Debug.Log(
                    $"Planet generation finished in {lastGenerationTimeMs:0.00} ms " +
                    $"({LastGenerationTimeSeconds:0.000} s), resolution {resolution}, " +
                    $"{lastGeneratedFaceCount} faces, {lastGeneratedVertexCount} vertices.",
                    this);
            }
        }

        private int CountActiveFaces()
        {
            if (meshFilters == null) return 0;

            int activeFaces = 0;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i] != null && meshFilters[i].gameObject.activeSelf)
                {
                    activeFaces++;
                }
            }

            return activeFaces;
        }

        private int CountGeneratedVertices()
        {
            if (meshFilters == null) return 0;

            int vertexCount = 0;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i] != null && meshFilters[i].gameObject.activeSelf && meshFilters[i].sharedMesh != null)
                {
                    vertexCount += meshFilters[i].sharedMesh.vertexCount;
                }
            }

            return vertexCount;
        }

        private void Initialize()
        {
            shapeGenerator = new ShapeGenerator(shapeSettings);
            colorGenerator = new ColorGenerator(colorSettings);

            EnsureMeshFiltersArray();

            ReleaseTerrainFaces();
            terrainFaces = new TerrainFace[FaceCount];

            for (int i = 0; i < FaceCount; i++)
            {
                if (meshFilters[i] == null)
                {
                    meshFilters[i] = CreateTerrainFaceObject(i);
                }

                ApplyPlanetMaterial(meshFilters[i]);
                terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, FaceDirections[i]);
                bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
                meshFilters[i].gameObject.SetActive(renderFace);
            }
        }

        private MeshFilter CreateTerrainFaceObject(int index)
        {
            GameObject meshObj = new GameObject("Terrain Face " + index);
            meshObj.transform.parent = transform;
            meshObj.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = meshObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            return meshFilter;
        }

        private void ApplyPlanetMaterial(MeshFilter meshFilter)
        {
            if (meshFilter == null || planetMaterial == null)
            {
                return;
            }

            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = meshFilter.gameObject.AddComponent<MeshRenderer>();
            }

            meshRenderer.sharedMaterial = planetMaterial;
        }

        private void EnsureMeshFiltersArray()
        {
            if (meshFilters == null)
            {
                meshFilters = new MeshFilter[FaceCount];
                return;
            }

            if (meshFilters.Length == FaceCount)
            {
                return;
            }

            MeshFilter[] resized = new MeshFilter[FaceCount];
            int copyCount = Mathf.Min(meshFilters.Length, FaceCount);
            for (int i = 0; i < copyCount; i++)
            {
                resized[i] = meshFilters[i];
            }

            meshFilters = resized;
        }

        private void ReleaseTerrainFaces()
        {
            if (terrainFaces == null)
            {
                return;
            }

            for (int i = 0; i < terrainFaces.Length; i++)
            {
                terrainFaces[i]?.Release();
            }
        }

        private void GenerateMesh()
        {
            UpdateSettingsNative();
            shapeGenerator.minElevationHeight = float.MaxValue;
            shapeGenerator.maxElevationHeight = float.MinValue;

            for (int i = 0; i < FaceCount; i++)
            {
                if (meshFilters[i].gameObject.activeSelf)
                {
                    terrainFaces[i].ConstructMesh(shapeLayersNative, biomesNative, GetTempNoiseStruct(), colorSettings.biomeSettings.blendAmount, shapeSettings.planetRadius);
                }
            }

            for (int i = 0; i < FaceCount; i++)
            {
                if (meshFilters[i].gameObject.activeSelf)
                {
                    terrainFaces[i].UpdateUVs(shapeGenerator.minElevationHeight, shapeGenerator.maxElevationHeight);
                }
            }
        }

        private void GenerateColors()
        {
            if (colorGenerator == null) colorGenerator = new ColorGenerator(colorSettings);
            DestroyGeneratedGradientTexture();
            generatedGradientTexture = colorGenerator.GenerateGradientTexture(shapeGenerator);

            planetMaterial.SetFloat("_MinHeight", shapeGenerator.minElevationHeight);
            planetMaterial.SetFloat("_MaxHeight", shapeGenerator.maxElevationHeight);
            planetMaterial.SetFloat("_PlanetRadius", shapeSettings.planetRadius);
            planetMaterial.SetTexture("_PlanetGradientTexture", generatedGradientTexture);

            planetMaterial.SetTexture("_OceanNormalMap", colorSettings.oceanSettings.oceanNormalMap);
            planetMaterial.SetFloat("_WaveSpeed", colorSettings.oceanSettings.waveSpeed);
            planetMaterial.SetFloat("_WaveScale", colorSettings.oceanSettings.waveScale);
            planetMaterial.SetFloat("_NormalStrength", colorSettings.oceanSettings.normalStrength);

            PassTerrainTextureSettings("_Sand", colorSettings.sand);
            PassTerrainTextureSettings("_Grass", colorSettings.grass);
            PassTerrainTextureSettings("_Mountain", colorSettings.mountain);
            PassTerrainTextureSettings("_Snow", colorSettings.snow);
            PassTextureTransitionSettings();
        }

        private void PassTextureTransitionSettings()
        {
            if (colorSettings.transitions == null)
            {
                return;
            }

            planetMaterial.SetFloat("_SandToGrassStart", colorSettings.transitions.sandToGrassStart);
            planetMaterial.SetFloat("_SandToGrassEnd", colorSettings.transitions.sandToGrassEnd);
            planetMaterial.SetFloat("_GrassToMountainStart", colorSettings.transitions.grassToMountainStart);
            planetMaterial.SetFloat("_GrassToMountainEnd", colorSettings.transitions.grassToMountainEnd);
            planetMaterial.SetFloat("_MountainToSnowStart", colorSettings.transitions.mountainToSnowStart);
            planetMaterial.SetFloat("_MountainToSnowEnd", colorSettings.transitions.mountainToSnowEnd);
        }

        private void PassTerrainTextureSettings(string prefix, ColorSettings.TerrainTextureSettings textureSettings)
        {
            if (textureSettings == null)
            {
                return;
            }

            if (textureSettings.enabled)
            {
                planetMaterial.SetTexture(prefix + "Tex", textureSettings.texture);
                planetMaterial.SetTexture(prefix + "Normal", textureSettings.normalMap);
                planetMaterial.SetTexture(prefix + "Rough", textureSettings.roughnessMap);
                planetMaterial.SetFloat(prefix + "Tiling", textureSettings.tiling);
            }
            else
            {
                planetMaterial.SetTexture(prefix + "Tex", null);
                planetMaterial.SetTexture(prefix + "Normal", null);
                planetMaterial.SetTexture(prefix + "Rough", null);
                planetMaterial.SetFloat(prefix + "Tiling", 0);
            }

            planetMaterial.SetFloat(prefix + "NormalStrength", textureSettings.normalStrength);
        }

        private void OnDestroy()
        {
            if (shapeLayersNative.IsCreated) shapeLayersNative.Dispose();
            if (biomesNative.IsCreated) biomesNative.Dispose();
            ReleaseTerrainFaces();
            DestroyGeneratedGradientTexture();
        }

        private bool HasValidGenerationSettings(bool logErrors)
        {
            if (resolution < 2)
            {
                if (logErrors) Debug.LogError("Planet generation failed: resolution must be at least 2.", this);
                return false;
            }

            if (lodLevels != null)
            {
                for (int i = 0; i < lodLevels.Length; i++)
                {
                    if (lodLevels[i].resolution < 2)
                    {
                        if (logErrors) Debug.LogError($"Planet generation failed: LOD level {i} resolution must be at least 2.", this);
                        return false;
                    }
                }
            }

            if (shapeSettings == null)
            {
                if (logErrors) Debug.LogError("Planet generation failed: ShapeSettings is not assigned.", this);
                return false;
            }

            if (shapeSettings.noiseLayers == null)
            {
                if (logErrors) Debug.LogError("Planet generation failed: ShapeSettings has no noise layer array.", this);
                return false;
            }

            for (int i = 0; i < shapeSettings.noiseLayers.Length; i++)
            {
                ShapeSettings.NoiseLayer layer = shapeSettings.noiseLayers[i];
                if (layer == null || layer.noiseSettings == null)
                {
                    if (logErrors) Debug.LogError($"Planet generation failed: noise layer {i} is incomplete.", this);
                    return false;
                }

                bool isRidgid = layer.noiseSettings.filterType == NoiseSettings.FilterType.Ridgid;
                if (isRidgid && layer.noiseSettings.ridgidNoiseSettings == null)
                {
                    if (logErrors) Debug.LogError($"Planet generation failed: ridgid noise layer {i} has no settings.", this);
                    return false;
                }

                if (!isRidgid && layer.noiseSettings.simpleNoiseSettings == null)
                {
                    if (logErrors) Debug.LogError($"Planet generation failed: simple noise layer {i} has no settings.", this);
                    return false;
                }
            }

            if (colorSettings == null)
            {
                if (logErrors) Debug.LogError("Planet generation failed: ColorSettings is not assigned.", this);
                return false;
            }

            if (colorSettings.textureResolution < 2)
            {
                if (logErrors) Debug.LogError("Planet generation failed: color texture resolution must be at least 2.", this);
                return false;
            }

            if (colorSettings.oceanSettings == null)
            {
                if (logErrors) Debug.LogError("Planet generation failed: ColorSettings has no ocean settings.", this);
                return false;
            }

            if (colorSettings.biomeSettings == null)
            {
                if (logErrors) Debug.LogError("Planet generation failed: ColorSettings has no biome settings.", this);
                return false;
            }

            if (colorSettings.biomeSettings.biomes == null || colorSettings.biomeSettings.biomes.Length == 0)
            {
                if (logErrors) Debug.LogError("Planet generation failed: at least one biome is required.", this);
                return false;
            }

            for (int i = 0; i < colorSettings.biomeSettings.biomes.Length; i++)
            {
                ColorSettings.BiomeSettings.Biome biome = colorSettings.biomeSettings.biomes[i];
                if (biome == null || biome.gradient == null)
                {
                    if (logErrors) Debug.LogError($"Planet generation failed: biome {i} is incomplete.", this);
                    return false;
                }
            }

            if (colorSettings.biomeSettings.temperatureNoise == null || colorSettings.biomeSettings.temperatureNoise.simpleNoiseSettings == null)
            {
                if (logErrors) Debug.LogError("Planet generation failed: temperature noise settings are incomplete.", this);
                return false;
            }

            if (planetMaterial == null)
            {
                if (logErrors) Debug.LogError("Planet generation failed: Planet material is not assigned.", this);
                return false;
            }

            return true;
        }

        private void DestroyGeneratedGradientTexture()
        {
            if (generatedGradientTexture == null || generatedGradientTexture == Texture2D.blackTexture)
            {
                generatedGradientTexture = null;
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedGradientTexture);
            }
            else
            {
                DestroyImmediate(generatedGradientTexture);
            }

            generatedGradientTexture = null;
        }

        public bool SavePresetToCurrentSlot()
        {
            if (presetDatabase == null)
            {
                Debug.LogWarning("Preset database is not assigned.");
                return false;
            }

            PlanetPresetData captured = PlanetPresetMapper.CaptureFromPlanet(this);
            presetDatabase.SaveSlot(presetSlot, captured);
            return true;
        }

        public bool LoadPresetFromCurrentSlot()
        {
            if (presetDatabase == null)
            {
                Debug.LogWarning("Preset database is not assigned.");
                return false;
            }

            PlanetPresetData data = presetDatabase.LoadSlot(presetSlot);
            if (data == null)
            {
                Debug.LogWarning($"Preset slot {presetSlot} is empty.");
                return false;
            }

            PlanetPresetMapper.ApplyToPlanet(this, data);
            GeneratePlanet();
            return true;
        }
    }
}
