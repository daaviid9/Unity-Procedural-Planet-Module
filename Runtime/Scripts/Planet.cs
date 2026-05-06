using UnityEngine;
using Unity.Collections;

namespace ProceduralPlanet
{
    public class Planet : MonoBehaviour
    {
        [System.Serializable]

        // LOD Settings
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

        [Header("Preset Settings")]
        public PlanetPresetDatabase presetDatabase;
        [Min(0)]
        public int presetSlot;

        [HideInInspector]
        public bool shapeSettingsFoldout;
        [HideInInspector]
        public bool colorSettingsFoldout;

        ShapeGenerator shapeGenerator;
        ColorGenerator colorGenerator;

        [SerializeField, HideInInspector]
        MeshFilter[] meshFilters;
        TerrainFace[] terrainFaces;

        // NativeArrays for Jobs
        NativeArray<NoiseLayerStruct> shapeLayersNative;
        NativeArray<BiomeStruct> biomesNative;

        private void Start()
        {
            GeneratePlanet();
        }

        void Update()
        {
            if (viewer == null || terrainFaces == null || terrainFaces.Length == 0) return;

            UpdateSettingsNative();

            int facesUpdatedThisFrame = 0;
            for (int i = 0; i < 6; i++)
            {
                if (!meshFilters[i].gameObject.activeSelf) continue;

                Vector3 faceWorldCenter = transform.TransformPoint(terrainFaces[i].localup_for_lod * shapeSettings.planetRadius);
                float distanceToViewer = Vector3.Distance(faceWorldCenter, viewer.position);

                Vector3 dirToFace = (faceWorldCenter - transform.position).normalized;
                Vector3 dirToViewer = (viewer.position - transform.position).normalized;
                float dotProduct = Vector3.Dot(dirToFace, dirToViewer);

                // Default na nízke rozlíšenie (odvrátená strana)
                int targetResolution = 2; 

                if (dotProduct > -0.4f)
                {
                    targetResolution = resolution;
                    for (int lvl = 0; lvl < lodLevels.Length; lvl++)
                    {
                        if (distanceToViewer < lodLevels[lvl].distance)
                        {
                            targetResolution = lodLevels[lvl].resolution;
                            break;
                        }
                    }
                }

                if (terrainFaces[i].resolution_property != targetResolution && facesUpdatedThisFrame < 1)
                {
                    terrainFaces[i].UpdateResolution(targetResolution);
                    terrainFaces[i].ConstructMesh(shapeLayersNative, biomesNative, GetTempNoiseStruct(), colorSettings.biomeSettings.blendAmount, shapeSettings.planetRadius);
                    
                    // Použijeme existujúce min/max aby sme zabránili flickeringu počas LoD
                    terrainFaces[i].UpdateUVs(shapeGenerator.minElevationHeight, shapeGenerator.maxElevationHeight);
                    facesUpdatedThisFrame++;
                }
            }
        }

        void UpdateSettingsNative()
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
                var layer = shapeSettings.noiseLayers[i];
                var s = (layer.noiseSettings.filterType == NoiseSettings.FilterType.Simple) ? 
                         layer.noiseSettings.simpleNoiseSettings : layer.noiseSettings.ridgidNoiseSettings;
                
                float weightMult = (layer.noiseSettings.filterType == NoiseSettings.FilterType.Ridgid) ? layer.noiseSettings.ridgidNoiseSettings.weightMultiplier : 1f;

                shapeLayersNative[i] = new NoiseLayerStruct {
                    enabled = layer.enabled,
                    useFirstLayerAsMask = layer.useFirstLayerAsMask,
                    strength = s.strength,
                    numLayers = s.numLayers,
                    baseRoughness = s.baseRoughness,
                    roughness = s.roughness,
                    persistence = s.persistence,
                    centre = s.centre,
                    minValue = s.minValue,
                    filterType = (int)layer.noiseSettings.filterType,
                    weightMultiplier = weightMult
                };
            }

            for (int i = 0; i < colorSettings.biomeSettings.biomes.Length; i++)
            {
                biomesNative[i] = new BiomeStruct {
                    startHeight = colorSettings.biomeSettings.biomes[i].startHeight
                };
            }
        }

        NoiseLayerStruct GetTempNoiseStruct()
        {
            var ns = colorSettings.biomeSettings.temperatureNoise;
            var s = ns.simpleNoiseSettings;
            return new NoiseLayerStruct {
                strength = s.strength,
                numLayers = s.numLayers,
                baseRoughness = s.baseRoughness,
                roughness = s.roughness,
                persistence = s.persistence,
                centre = s.centre,
                minValue = s.minValue,
                weightMultiplier = 1f
            };
        }

        public void GeneratePlanet()
        {
            Initialize();
            GenerateMesh();
            GenerateColors();
        }

        public void OnPlanetSettingsUpdated()
        {
            if (autoUpdate)
            {
                Initialize();
                GenerateMesh();
                GenerateColors();
            }
        }

        void Initialize()
        {
            shapeGenerator = new ShapeGenerator(shapeSettings);
            colorGenerator = new ColorGenerator(colorSettings);

            if (meshFilters == null || meshFilters.Length == 0) meshFilters = new MeshFilter[6];

            if (terrainFaces != null)
            {
                for (int i = 0; i < terrainFaces.Length; i++)
                {
                    terrainFaces[i]?.Release();
                }
            }

            terrainFaces = new TerrainFace[6];

            Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

            for (int i = 0; i < 6; i++)
            {
                if (meshFilters[i] == null)
                {
                    GameObject meshObj = new GameObject("Terrain Face " + i);
                    meshObj.transform.parent = transform;
                    meshObj.AddComponent<MeshRenderer>().sharedMaterial = planetMaterial;
                    meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                    meshFilters[i].sharedMesh = new Mesh();
                }

                terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i]);
                bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
                meshFilters[i].gameObject.SetActive(renderFace);
            }
        }

        void GenerateMesh()
        {
            UpdateSettingsNative();
            shapeGenerator.minElevationHeight = float.MaxValue;
            shapeGenerator.maxElevationHeight = float.MinValue;

            for (int i = 0; i < 6; i++)
            {
                if (meshFilters[i].gameObject.activeSelf)
                {
                    terrainFaces[i].ConstructMesh(shapeLayersNative, biomesNative, GetTempNoiseStruct(), colorSettings.biomeSettings.blendAmount, shapeSettings.planetRadius);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                if (meshFilters[i].gameObject.activeSelf)
                {
                    terrainFaces[i].UpdateUVs(shapeGenerator.minElevationHeight, shapeGenerator.maxElevationHeight);
                }
            }
        }

        void GenerateColors()
        {
            if (colorGenerator == null) colorGenerator = new ColorGenerator(colorSettings);
            Texture2D gradientTexture = colorGenerator.GenerateGradientTexture(shapeGenerator);

            planetMaterial.SetFloat("_MinHeight", shapeGenerator.minElevationHeight);
            planetMaterial.SetFloat("_MaxHeight", shapeGenerator.maxElevationHeight);
            planetMaterial.SetFloat("_PlanetRadius", shapeSettings.planetRadius);
            planetMaterial.SetTexture("_PlanetGradientTexture", gradientTexture);

            planetMaterial.SetTexture("_OceanNormalMap", colorSettings.oceanSettings.oceanNormalMap);
            planetMaterial.SetFloat("_WaveSpeed", colorSettings.oceanSettings.waveSpeed);
            planetMaterial.SetFloat("_WaveScale", colorSettings.oceanSettings.waveScale);
            planetMaterial.SetFloat("_NormalStrength", colorSettings.oceanSettings.normalStrength);

            PassTerrainTextureSettings("_Sand", colorSettings.sand);
            PassTerrainTextureSettings("_Grass", colorSettings.grass);
            PassTerrainTextureSettings("_Mountain", colorSettings.mountain);
            PassTerrainTextureSettings("_Snow", colorSettings.snow);

            if (colorSettings.transitions != null)
            {
                planetMaterial.SetFloat("_SandToGrassStart", colorSettings.transitions.sandToGrassStart);
                planetMaterial.SetFloat("_SandToGrassEnd", colorSettings.transitions.sandToGrassEnd);
                
                planetMaterial.SetFloat("_GrassToMountainStart", colorSettings.transitions.grassToMountainStart);
                planetMaterial.SetFloat("_GrassToMountainEnd", colorSettings.transitions.grassToMountainEnd);
                
                planetMaterial.SetFloat("_MountainToSnowStart", colorSettings.transitions.mountainToSnowStart);
                planetMaterial.SetFloat("_MountainToSnowEnd", colorSettings.transitions.mountainToSnowEnd);
            }
        }

        void PassTerrainTextureSettings(string prefix, ColorSettings.TerrainTextureSettings textureSettings)
        {
            if (textureSettings != null)
            {
                //planetMaterial.SetFloat(prefix + "Enabled", textureSettings.enabled ? 1f : 0f);
                if (textureSettings.enabled) {
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
        }

        private void OnDestroy()
        {
            if (shapeLayersNative.IsCreated) shapeLayersNative.Dispose();
            if (biomesNative.IsCreated) biomesNative.Dispose();
            if (terrainFaces != null)
            {
                foreach (var face in terrainFaces) face?.Release();
            }
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