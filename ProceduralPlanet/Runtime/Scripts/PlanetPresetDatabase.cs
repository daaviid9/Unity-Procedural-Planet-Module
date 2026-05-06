using System;
using UnityEngine;

namespace ProceduralPlanet
{
    [CreateAssetMenu(fileName = "PlanetPresetDatabase", menuName = "Procedural Planet/Planet Preset Database")]
    public class PlanetPresetDatabase : ScriptableObject
    {
        [SerializeField] private PlanetPresetData[] slots = new PlanetPresetData[5];

        public int SlotCount => slots?.Length ?? 0;

        public PlanetPresetData LoadSlot(int index)
        {
            if (!IsValidIndex(index))
            {
                return null;
            }

            return slots[index];
        }

        public void SaveSlot(int index, PlanetPresetData data)
        {
            if (index < 0)
            {
                return;
            }

            EnsureSize(index + 1);
            slots[index] = data;
        }

        private bool IsValidIndex(int index)
        {
            return slots != null && index >= 0 && index < slots.Length;
        }

        private void EnsureSize(int requiredSize)
        {
            if (slots == null)
            {
                slots = new PlanetPresetData[requiredSize];
                return;
            }

            if (slots.Length >= requiredSize)
            {
                return;
            }

            Array.Resize(ref slots, requiredSize);
        }
    }

    [Serializable]
    public class PlanetPresetData
    {
        public int resolution;
        public Planet.LODLevel[] lodLevels;
        public float planetRadius;
        public ShapeNoiseLayerData[] noiseLayers;
        public ColorSettingsData colorSettings;
    }

    [Serializable]
    public class ShapeNoiseLayerData
    {
        public bool enabled;
        public bool useFirstLayerAsMask;
        public NoiseSettingsData noiseSettings;
    }

    [Serializable]
    public class NoiseSettingsData
    {
        public NoiseSettings.FilterType filterType;
        public NoiseSettings.SimpleNoiseSettings simpleNoiseSettings;
        public NoiseSettings.RidgidNoiseSettings ridgidNoiseSettings;
    }

    [Serializable]
    public class ColorSettingsData
    {
        public int textureResolution;
        public bool debugMode;
        public OceanSettingsData oceanSettings;
        public BiomeSettingsData biomeSettings;
        public TerrainTextureSettingsData sand;
        public TerrainTextureSettingsData grass;
        public TerrainTextureSettingsData mountain;
        public TerrainTextureSettingsData snow;
        public TextureTransitionSettingsData transitions;
    }

    [Serializable]
    public class OceanSettingsData
    {
        public Gradient oceanGradient;
        public Texture2D oceanNormalMap;
        public float waveSpeed;
        public float waveScale;
        public float normalStrength;
    }

    [Serializable]
    public class BiomeSettingsData
    {
        public float blendAmount;
        public BiomeData[] biomes;
        public NoiseSettingsData temperatureNoise;
    }

    [Serializable]
    public class BiomeData
    {
        public Gradient gradient;
        public Color tint;
        public float startHeight;
    }

    [Serializable]
    public class TerrainTextureSettingsData
    {
        public float normalStrength;
        public bool enabled;
        public Texture2D texture;
        public Texture2D normalMap;
        public Texture2D roughnessMap;
        public float tiling;
    }

    [Serializable]
    public class TextureTransitionSettingsData
    {
        public float sandToGrassStart;
        public float sandToGrassEnd;
        public float grassToMountainStart;
        public float grassToMountainEnd;
        public float mountainToSnowStart;
        public float mountainToSnowEnd;
    }

    public static class PlanetPresetMapper
    {
        public static PlanetPresetData CaptureFromPlanet(Planet planet)
        {
            var shape = planet.shapeSettings;
            var color = planet.colorSettings;

            return new PlanetPresetData
            {
                resolution = planet.resolution,
                lodLevels = CloneLodLevels(planet.lodLevels),
                planetRadius = shape != null ? shape.planetRadius : 1f,
                noiseLayers = CloneShapeNoiseLayers(shape != null ? shape.noiseLayers : null),
                colorSettings = CloneColorSettings(color)
            };
        }

        public static void ApplyToPlanet(Planet planet, PlanetPresetData data)
        {
            if (data == null)
            {
                return;
            }

            planet.resolution = data.resolution;
            planet.lodLevels = CloneLodLevels(data.lodLevels);

            if (planet.shapeSettings != null)
            {
                planet.shapeSettings.planetRadius = data.planetRadius;
                planet.shapeSettings.noiseLayers = CloneShapeNoiseLayersToRuntime(data.noiseLayers);
            }

            if (planet.colorSettings != null)
            {
                ApplyColorSettingsToRuntime(planet.colorSettings, data.colorSettings);
            }
        }

        private static Planet.LODLevel[] CloneLodLevels(Planet.LODLevel[] source)
        {
            if (source == null)
            {
                return Array.Empty<Planet.LODLevel>();
            }

            Planet.LODLevel[] copy = new Planet.LODLevel[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static ShapeNoiseLayerData[] CloneShapeNoiseLayers(ShapeSettings.NoiseLayer[] source)
        {
            if (source == null)
            {
                return Array.Empty<ShapeNoiseLayerData>();
            }

            ShapeNoiseLayerData[] output = new ShapeNoiseLayerData[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var layer = source[i];
                output[i] = new ShapeNoiseLayerData
                {
                    enabled = layer.enabled,
                    useFirstLayerAsMask = layer.useFirstLayerAsMask,
                    noiseSettings = CloneNoiseSettings(layer.noiseSettings)
                };
            }

            return output;
        }

        private static ShapeSettings.NoiseLayer[] CloneShapeNoiseLayersToRuntime(ShapeNoiseLayerData[] source)
        {
            if (source == null)
            {
                return Array.Empty<ShapeSettings.NoiseLayer>();
            }

            ShapeSettings.NoiseLayer[] output = new ShapeSettings.NoiseLayer[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var layer = source[i] ?? new ShapeNoiseLayerData();
                output[i] = new ShapeSettings.NoiseLayer
                {
                    enabled = layer.enabled,
                    useFirstLayerAsMask = layer.useFirstLayerAsMask,
                    noiseSettings = BuildRuntimeNoiseSettings(layer.noiseSettings)
                };
            }

            return output;
        }

        private static NoiseSettingsData CloneNoiseSettings(NoiseSettings source)
        {
            if (source == null)
            {
                return new NoiseSettingsData
                {
                    simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings(),
                    ridgidNoiseSettings = new NoiseSettings.RidgidNoiseSettings()
                };
            }

            return new NoiseSettingsData
            {
                filterType = source.filterType,
                simpleNoiseSettings = CloneSimpleNoise(source.simpleNoiseSettings),
                ridgidNoiseSettings = CloneRidgidNoise(source.ridgidNoiseSettings)
            };
        }

        private static NoiseSettings BuildRuntimeNoiseSettings(NoiseSettingsData source)
        {
            if (source == null)
            {
                return new NoiseSettings
                {
                    simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings(),
                    ridgidNoiseSettings = new NoiseSettings.RidgidNoiseSettings()
                };
            }

            return new NoiseSettings
            {
                filterType = source.filterType,
                simpleNoiseSettings = CloneSimpleNoise(source.simpleNoiseSettings),
                ridgidNoiseSettings = CloneRidgidNoise(source.ridgidNoiseSettings)
            };
        }

        private static NoiseSettings.SimpleNoiseSettings CloneSimpleNoise(NoiseSettings.SimpleNoiseSettings source)
        {
            if (source == null)
            {
                return new NoiseSettings.SimpleNoiseSettings();
            }

            return new NoiseSettings.SimpleNoiseSettings
            {
                strength = source.strength,
                numLayers = source.numLayers,
                baseRoughness = source.baseRoughness,
                roughness = source.roughness,
                persistence = source.persistence,
                centre = source.centre,
                minValue = source.minValue
            };
        }

        private static NoiseSettings.RidgidNoiseSettings CloneRidgidNoise(NoiseSettings.RidgidNoiseSettings source)
        {
            if (source == null)
            {
                return new NoiseSettings.RidgidNoiseSettings();
            }

            return new NoiseSettings.RidgidNoiseSettings
            {
                strength = source.strength,
                numLayers = source.numLayers,
                baseRoughness = source.baseRoughness,
                roughness = source.roughness,
                persistence = source.persistence,
                centre = source.centre,
                minValue = source.minValue,
                weightMultiplier = source.weightMultiplier
            };
        }

        private static ColorSettingsData CloneColorSettings(ColorSettings source)
        {
            if (source == null)
            {
                return null;
            }

            return new ColorSettingsData
            {
                textureResolution = source.textureResolution,
                debugMode = source.debugMode,
                oceanSettings = CloneOceanSettings(source.oceanSettings),
                biomeSettings = CloneBiomeSettings(source.biomeSettings),
                sand = CloneTerrainTexture(source.sand),
                grass = CloneTerrainTexture(source.grass),
                mountain = CloneTerrainTexture(source.mountain),
                snow = CloneTerrainTexture(source.snow),
                transitions = CloneTransitions(source.transitions)
            };
        }

        private static void ApplyColorSettingsToRuntime(ColorSettings runtime, ColorSettingsData data)
        {
            if (runtime == null || data == null)
            {
                return;
            }

            runtime.textureResolution = data.textureResolution;
            runtime.debugMode = data.debugMode;
            runtime.oceanSettings = BuildRuntimeOcean(data.oceanSettings);
            runtime.biomeSettings = BuildRuntimeBiomes(data.biomeSettings);
            runtime.sand = BuildRuntimeTerrainTexture(data.sand);
            runtime.grass = BuildRuntimeTerrainTexture(data.grass);
            runtime.mountain = BuildRuntimeTerrainTexture(data.mountain);
            runtime.snow = BuildRuntimeTerrainTexture(data.snow);
            runtime.transitions = BuildRuntimeTransitions(data.transitions);
        }

        private static OceanSettingsData CloneOceanSettings(ColorSettings.OceanSettings source)
        {
            if (source == null)
            {
                return new OceanSettingsData();
            }

            return new OceanSettingsData
            {
                oceanGradient = CloneGradient(source.oceanGradient),
                oceanNormalMap = source.oceanNormalMap,
                waveSpeed = source.waveSpeed,
                waveScale = source.waveScale,
                normalStrength = source.normalStrength
            };
        }

        private static ColorSettings.OceanSettings BuildRuntimeOcean(OceanSettingsData source)
        {
            if (source == null)
            {
                return new ColorSettings.OceanSettings();
            }

            return new ColorSettings.OceanSettings
            {
                oceanGradient = CloneGradient(source.oceanGradient),
                oceanNormalMap = source.oceanNormalMap,
                waveSpeed = source.waveSpeed,
                waveScale = source.waveScale,
                normalStrength = source.normalStrength
            };
        }

        private static BiomeSettingsData CloneBiomeSettings(ColorSettings.BiomeSettings source)
        {
            if (source == null)
            {
                return new BiomeSettingsData
                {
                    biomes = Array.Empty<BiomeData>(),
                    temperatureNoise = new NoiseSettingsData()
                };
            }

            BiomeData[] biomes = source.biomes == null
                ? Array.Empty<BiomeData>()
                : new BiomeData[source.biomes.Length];

            for (int i = 0; i < biomes.Length; i++)
            {
                var biome = source.biomes[i];
                biomes[i] = new BiomeData
                {
                    gradient = CloneGradient(biome.gradient),
                    tint = biome.tint,
                    startHeight = biome.startHeight
                };
            }

            return new BiomeSettingsData
            {
                blendAmount = source.blendAmount,
                biomes = biomes,
                temperatureNoise = CloneNoiseSettings(source.temperatureNoise)
            };
        }

        private static ColorSettings.BiomeSettings BuildRuntimeBiomes(BiomeSettingsData source)
        {
            ColorSettings.BiomeSettings runtime = new ColorSettings.BiomeSettings();
            if (source == null)
            {
                runtime.biomes = Array.Empty<ColorSettings.BiomeSettings.Biome>();
                runtime.temperatureNoise = new NoiseSettings
                {
                    simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings(),
                    ridgidNoiseSettings = new NoiseSettings.RidgidNoiseSettings()
                };
                return runtime;
            }

            runtime.blendAmount = source.blendAmount;
            runtime.biomes = source.biomes == null
                ? Array.Empty<ColorSettings.BiomeSettings.Biome>()
                : new ColorSettings.BiomeSettings.Biome[source.biomes.Length];

            for (int i = 0; i < runtime.biomes.Length; i++)
            {
                var biome = source.biomes[i] ?? new BiomeData();
                runtime.biomes[i] = new ColorSettings.BiomeSettings.Biome
                {
                    gradient = CloneGradient(biome.gradient),
                    tint = biome.tint,
                    startHeight = biome.startHeight
                };
            }

            runtime.temperatureNoise = BuildRuntimeNoiseSettings(source.temperatureNoise);
            return runtime;
        }

        private static TerrainTextureSettingsData CloneTerrainTexture(ColorSettings.TerrainTextureSettings source)
        {
            if (source == null)
            {
                return new TerrainTextureSettingsData();
            }

            return new TerrainTextureSettingsData
            {
                normalStrength = source.normalStrength,
                enabled = source.enabled,
                texture = source.texture,
                normalMap = source.normalMap,
                roughnessMap = source.roughnessMap,
                tiling = source.tiling
            };
        }

        private static ColorSettings.TerrainTextureSettings BuildRuntimeTerrainTexture(TerrainTextureSettingsData source)
        {
            if (source == null)
            {
                return new ColorSettings.TerrainTextureSettings();
            }

            return new ColorSettings.TerrainTextureSettings
            {
                normalStrength = source.normalStrength,
                enabled = source.enabled,
                texture = source.texture,
                normalMap = source.normalMap,
                roughnessMap = source.roughnessMap,
                tiling = source.tiling
            };
        }

        private static TextureTransitionSettingsData CloneTransitions(ColorSettings.TextureTransitionSettings source)
        {
            if (source == null)
            {
                return new TextureTransitionSettingsData();
            }

            return new TextureTransitionSettingsData
            {
                sandToGrassStart = source.sandToGrassStart,
                sandToGrassEnd = source.sandToGrassEnd,
                grassToMountainStart = source.grassToMountainStart,
                grassToMountainEnd = source.grassToMountainEnd,
                mountainToSnowStart = source.mountainToSnowStart,
                mountainToSnowEnd = source.mountainToSnowEnd
            };
        }

        private static ColorSettings.TextureTransitionSettings BuildRuntimeTransitions(TextureTransitionSettingsData source)
        {
            if (source == null)
            {
                return new ColorSettings.TextureTransitionSettings();
            }

            return new ColorSettings.TextureTransitionSettings
            {
                sandToGrassStart = source.sandToGrassStart,
                sandToGrassEnd = source.sandToGrassEnd,
                grassToMountainStart = source.grassToMountainStart,
                grassToMountainEnd = source.grassToMountainEnd,
                mountainToSnowStart = source.mountainToSnowStart,
                mountainToSnowEnd = source.mountainToSnowEnd
            };
        }

        private static Gradient CloneGradient(Gradient source)
        {
            if (source == null)
            {
                return new Gradient();
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(source.colorKeys, source.alphaKeys);
            gradient.mode = source.mode;
            return gradient;
        }
    }
}
