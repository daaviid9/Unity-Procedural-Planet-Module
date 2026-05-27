using System;
using System.IO;
using UnityEngine;

namespace ProceduralPlanet
{
    [Serializable]
    public class PlanetRuntimeSimplePresetData
    {
        public int resolution;
        public float planetRadius;
        public Planet.LODLevel[] lodLevels;
        public RuntimeNoiseLayerData[] noiseLayers;
        public RuntimeColorSettingsData colorSettings;
    }

    [Serializable]
    public class RuntimeNoiseLayerData
    {
        public bool enabled;
        public bool useFirstLayerAsMask;
        public RuntimeNoiseSettingsData noiseSettings;
    }

    [Serializable]
    public class RuntimeNoiseSettingsData
    {
        public NoiseSettings.FilterType filterType;
        public RuntimeSimpleNoiseSettingsData simple;
        public RuntimeRidgidNoiseSettingsData ridgid;
    }

    [Serializable]
    public class RuntimeSimpleNoiseSettingsData
    {
        public float strength;
        public int numLayers;
        public float baseRoughness;
        public float roughness;
        public float persistence;
        public Vector3 centre;
        public float minValue;
    }

    [Serializable]
    public class RuntimeRidgidNoiseSettingsData : RuntimeSimpleNoiseSettingsData
    {
        public float weightMultiplier;
    }

    // --- COLOR SETTINGS DATA WRAPPERS ---
    
    [Serializable]
    public class RuntimeGradientColorKeyData
    {
        public Color color;
        public float time;
    }

    [Serializable]
    public class RuntimeGradientAlphaKeyData
    {
        public float alpha;
        public float time;
    }

    [Serializable]
    public class RuntimeGradientData
    {
        public RuntimeGradientColorKeyData[] colorKeys;
        public RuntimeGradientAlphaKeyData[] alphaKeys;
        public GradientMode mode;
    }

    [Serializable]
    public class RuntimeBiomeData
    {
        public Color tint;
        public float startHeight;
        public RuntimeGradientData gradient;
    }

    [Serializable]
    public class RuntimeBiomeSettingsData
    {
        public float blendAmount;
        public RuntimeBiomeData[] biomes;
        public RuntimeNoiseSettingsData temperatureNoise;
    }

    [Serializable]
    public class RuntimeOceanSettingsData
    {
        public RuntimeGradientData oceanGradient;
        public string oceanNormalMapName;
        public float waveSpeed = 0.02f;
        public float waveScale = 0.5f;
        public float normalStrength = 1f;
    }

    [Serializable]
    public class RuntimeTerrainTextureSettingsData
    {
        public bool enabled = true;
        public string textureName;
        public string normalMapName;
        public string roughnessMapName;
        public float normalStrength = 1f;
        public float tiling = 0.2f;
    }

    [Serializable]
    public class RuntimeColorSettingsData
    {
        public bool debugMode;
        public RuntimeOceanSettingsData oceanSettings;
        public RuntimeBiomeSettingsData biomeSettings;
        public RuntimeTerrainTextureSettingsData sand;
        public RuntimeTerrainTextureSettingsData grass;
        public RuntimeTerrainTextureSettingsData mountain;
        public RuntimeTerrainTextureSettingsData snow;
    }

    // ------------------------------------

    public static class PlanetRuntimeSimplePresetMapper
    {
        public static PlanetRuntimeSimplePresetData Capture(Planet planet)
        {
            ShapeSettings shape = planet.shapeSettings;
            ColorSettings color = planet.colorSettings;
            
            return new PlanetRuntimeSimplePresetData
            {
                resolution = planet.resolution,
                planetRadius = shape != null ? shape.planetRadius : 1f,
                lodLevels = CloneLodLevels(planet.lodLevels),
                noiseLayers = CloneNoiseLayers(shape != null ? shape.noiseLayers : null),
                colorSettings = CloneColorSettings(color)
            };
        }

        public static void Apply(Planet planet, PlanetRuntimeSimplePresetData data)
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
                planet.shapeSettings.noiseLayers = BuildRuntimeNoiseLayers(data.noiseLayers);
            }

            if (planet.colorSettings != null && data.colorSettings != null)
            {
                planet.colorSettings.debugMode = data.colorSettings.debugMode;
                if (data.colorSettings.oceanSettings != null)
                {
                    if (planet.colorSettings.oceanSettings == null)
                    {
                        planet.colorSettings.oceanSettings = new ColorSettings.OceanSettings();
                    }

                    planet.colorSettings.oceanSettings.oceanGradient = BuildRuntimeGradient(data.colorSettings.oceanSettings.oceanGradient);
                    planet.colorSettings.oceanSettings.oceanNormalMap = PlanetRuntimeTextureRegistry.Get(data.colorSettings.oceanSettings.oceanNormalMapName);
                    planet.colorSettings.oceanSettings.waveSpeed = data.colorSettings.oceanSettings.waveSpeed;
                    planet.colorSettings.oceanSettings.waveScale = data.colorSettings.oceanSettings.waveScale;
                    planet.colorSettings.oceanSettings.normalStrength = data.colorSettings.oceanSettings.normalStrength;
                }

                if (data.colorSettings.biomeSettings != null)
                {
                    planet.colorSettings.biomeSettings.blendAmount = data.colorSettings.biomeSettings.blendAmount;
                    planet.colorSettings.biomeSettings.temperatureNoise = BuildRuntimeNoiseSettings(data.colorSettings.biomeSettings.temperatureNoise);
                    planet.colorSettings.biomeSettings.biomes = BuildRuntimeBiomes(data.colorSettings.biomeSettings.biomes);
                }

                if (data.colorSettings.sand != null)
                {
                    planet.colorSettings.sand = BuildRuntimeTerrainTextureSettings(data.colorSettings.sand);
                }
                if (data.colorSettings.grass != null)
                {
                    planet.colorSettings.grass = BuildRuntimeTerrainTextureSettings(data.colorSettings.grass);
                }
                if (data.colorSettings.mountain != null)
                {
                    planet.colorSettings.mountain = BuildRuntimeTerrainTextureSettings(data.colorSettings.mountain);
                }
                if (data.colorSettings.snow != null)
                {
                    planet.colorSettings.snow = BuildRuntimeTerrainTextureSettings(data.colorSettings.snow);
                }
            }
        }

        // --- CLONING METHODS ---

        private static Planet.LODLevel[] CloneLodLevels(Planet.LODLevel[] source)
        {
            if (source == null) return Array.Empty<Planet.LODLevel>();
            Planet.LODLevel[] copy = new Planet.LODLevel[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static RuntimeNoiseLayerData[] CloneNoiseLayers(ShapeSettings.NoiseLayer[] source)
        {
            if (source == null) return Array.Empty<RuntimeNoiseLayerData>();
            RuntimeNoiseLayerData[] output = new RuntimeNoiseLayerData[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ShapeSettings.NoiseLayer src = source[i];
                output[i] = new RuntimeNoiseLayerData
                {
                    enabled = src.enabled,
                    useFirstLayerAsMask = src.useFirstLayerAsMask,
                    noiseSettings = CloneNoiseSettings(src.noiseSettings)
                };
            }
            return output;
        }

        private static RuntimeNoiseSettingsData CloneNoiseSettings(NoiseSettings source)
        {
            if (source == null)
            {
                return new RuntimeNoiseSettingsData
                {
                    simple = new RuntimeSimpleNoiseSettingsData(),
                    ridgid = new RuntimeRidgidNoiseSettingsData()
                };
            }
            return new RuntimeNoiseSettingsData
            {
                filterType = source.filterType,
                simple = CloneSimple(source.simpleNoiseSettings),
                ridgid = CloneRidgid(source.ridgidNoiseSettings)
            };
        }

        private static RuntimeSimpleNoiseSettingsData CloneSimple(NoiseSettings.SimpleNoiseSettings source)
        {
            if (source == null) return new RuntimeSimpleNoiseSettingsData();
            return new RuntimeSimpleNoiseSettingsData
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

        private static RuntimeRidgidNoiseSettingsData CloneRidgid(NoiseSettings.RidgidNoiseSettings source)
        {
            if (source == null) return new RuntimeRidgidNoiseSettingsData();
            return new RuntimeRidgidNoiseSettingsData
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

        private static RuntimeColorSettingsData CloneColorSettings(ColorSettings source)
        {
            if (source == null) return null;
            return new RuntimeColorSettingsData
            {
                debugMode = source.debugMode,
                oceanSettings = CloneOceanSettings(source.oceanSettings),
                biomeSettings = CloneBiomeSettings(source.biomeSettings),
                sand = CloneTerrainTextureSettings(source.sand),
                grass = CloneTerrainTextureSettings(source.grass),
                mountain = CloneTerrainTextureSettings(source.mountain),
                snow = CloneTerrainTextureSettings(source.snow)
            };
        }

        private static RuntimeOceanSettingsData CloneOceanSettings(ColorSettings.OceanSettings source)
        {
            if (source == null) return null;
            return new RuntimeOceanSettingsData
            {
                oceanGradient = CloneGradient(source.oceanGradient),
                oceanNormalMapName = source.oceanNormalMap != null ? source.oceanNormalMap.name : null,
                waveSpeed = source.waveSpeed,
                waveScale = source.waveScale,
                normalStrength = source.normalStrength
            };
        }

        private static RuntimeTerrainTextureSettingsData CloneTerrainTextureSettings(ColorSettings.TerrainTextureSettings source)
        {
            if (source == null) return null;

            return new RuntimeTerrainTextureSettingsData
            {
                enabled = source.enabled,
                textureName = source.texture != null ? source.texture.name : null,
                normalMapName = source.normalMap != null ? source.normalMap.name : null,
                roughnessMapName = source.roughnessMap != null ? source.roughnessMap.name : null,
                normalStrength = source.normalStrength,
                tiling = source.tiling
            };
        }

        private static RuntimeBiomeSettingsData CloneBiomeSettings(ColorSettings.BiomeSettings source)
        {
            if (source == null) return null;
            return new RuntimeBiomeSettingsData
            {
                blendAmount = source.blendAmount,
                temperatureNoise = CloneNoiseSettings(source.temperatureNoise),
                biomes = CloneBiomes(source.biomes)
            };
        }

        private static RuntimeBiomeData[] CloneBiomes(ColorSettings.BiomeSettings.Biome[] source)
        {
            if (source == null) return Array.Empty<RuntimeBiomeData>();
            RuntimeBiomeData[] output = new RuntimeBiomeData[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                output[i] = new RuntimeBiomeData
                {
                    tint = source[i].tint,
                    startHeight = source[i].startHeight,
                    gradient = CloneGradient(source[i].gradient)
                };
            }
            return output;
        }

        private static RuntimeGradientData CloneGradient(Gradient source)
        {
            if (source == null) return null;
            RuntimeGradientData data = new RuntimeGradientData();
            data.mode = source.mode;
            
            data.colorKeys = new RuntimeGradientColorKeyData[source.colorKeys.Length];
            for (int i = 0; i < source.colorKeys.Length; i++)
            {
                data.colorKeys[i] = new RuntimeGradientColorKeyData { color = source.colorKeys[i].color, time = source.colorKeys[i].time };
            }

            data.alphaKeys = new RuntimeGradientAlphaKeyData[source.alphaKeys.Length];
            for (int i = 0; i < source.alphaKeys.Length; i++)
            {
                data.alphaKeys[i] = new RuntimeGradientAlphaKeyData { alpha = source.alphaKeys[i].alpha, time = source.alphaKeys[i].time };
            }
            return data;
        }

        // --- BUILDING METHODS ---

        private static ShapeSettings.NoiseLayer[] BuildRuntimeNoiseLayers(RuntimeNoiseLayerData[] source)
        {
            if (source == null) return Array.Empty<ShapeSettings.NoiseLayer>();
            ShapeSettings.NoiseLayer[] output = new ShapeSettings.NoiseLayer[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RuntimeNoiseLayerData src = source[i] ?? new RuntimeNoiseLayerData();
                output[i] = new ShapeSettings.NoiseLayer
                {
                    enabled = src.enabled,
                    useFirstLayerAsMask = src.useFirstLayerAsMask,
                    noiseSettings = BuildRuntimeNoiseSettings(src.noiseSettings)
                };
            }
            return output;
        }

        private static NoiseSettings BuildRuntimeNoiseSettings(RuntimeNoiseSettingsData source)
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
                simpleNoiseSettings = BuildRuntimeSimple(source.simple),
                ridgidNoiseSettings = BuildRuntimeRidgid(source.ridgid)
            };
        }

        private static NoiseSettings.SimpleNoiseSettings BuildRuntimeSimple(RuntimeSimpleNoiseSettingsData source)
        {
            if (source == null) return new NoiseSettings.SimpleNoiseSettings();
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

        private static NoiseSettings.RidgidNoiseSettings BuildRuntimeRidgid(RuntimeRidgidNoiseSettingsData source)
        {
            if (source == null) return new NoiseSettings.RidgidNoiseSettings();
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

        private static ColorSettings.BiomeSettings.Biome[] BuildRuntimeBiomes(RuntimeBiomeData[] source)
        {
            if (source == null) return Array.Empty<ColorSettings.BiomeSettings.Biome>();
            ColorSettings.BiomeSettings.Biome[] output = new ColorSettings.BiomeSettings.Biome[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                output[i] = new ColorSettings.BiomeSettings.Biome
                {
                    tint = source[i].tint,
                    startHeight = source[i].startHeight,
                    gradient = BuildRuntimeGradient(source[i].gradient)
                };
            }
            return output;
        }

        private static ColorSettings.TerrainTextureSettings BuildRuntimeTerrainTextureSettings(RuntimeTerrainTextureSettingsData source)
        {
            ColorSettings.TerrainTextureSettings settings = new ColorSettings.TerrainTextureSettings();
            if (source == null) return settings;

            settings.enabled = source.enabled;
            settings.texture = PlanetRuntimeTextureRegistry.Get(source.textureName);
            settings.normalMap = PlanetRuntimeTextureRegistry.Get(source.normalMapName);
            settings.roughnessMap = PlanetRuntimeTextureRegistry.Get(source.roughnessMapName);
            settings.normalStrength = source.normalStrength;
            settings.tiling = source.tiling;
            return settings;
        }

        private static Gradient BuildRuntimeGradient(RuntimeGradientData source)
        {
            Gradient g = new Gradient();
            if (source == null) return g;
            g.mode = source.mode;

            GradientColorKey[] cKeys = new GradientColorKey[2] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.white, 1f) };
            if (source.colorKeys != null && source.colorKeys.Length > 0)
            {
                cKeys = new GradientColorKey[source.colorKeys.Length];
                for (int i = 0; i < source.colorKeys.Length; i++)
                {
                    cKeys[i] = new GradientColorKey(source.colorKeys[i].color, source.colorKeys[i].time);
                }
            }

            GradientAlphaKey[] aKeys = new GradientAlphaKey[2] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };
            if (source.alphaKeys != null && source.alphaKeys.Length > 0)
            {
                aKeys = new GradientAlphaKey[source.alphaKeys.Length];
                for (int i = 0; i < source.alphaKeys.Length; i++)
                {
                    aKeys[i] = new GradientAlphaKey(source.alphaKeys[i].alpha, source.alphaKeys[i].time);
                }
            }

            g.SetKeys(cKeys, aKeys);
            return g;
        }
    }

    public static class PlanetRuntimeTextureRegistry
    {
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> texturesByName =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        public static void Register(Texture2D[] textures)
        {
            if (textures == null) return;

            for (int i = 0; i < textures.Length; i++)
            {
                Register(textures[i]);
            }
        }

        public static void Register(Texture2D texture)
        {
            if (texture == null || string.IsNullOrEmpty(texture.name)) return;

            texturesByName[texture.name] = texture;
        }

        public static Texture2D Get(string textureName)
        {
            if (string.IsNullOrEmpty(textureName)) return null;
            return texturesByName.TryGetValue(textureName, out Texture2D texture) ? texture : null;
        }
    }

    public static class PlanetRuntimeSimplePresetStorage
    {
        private const string UserPresetFolderName = "PlanetPresets";
        private const string DefaultPresetResourceFolder = "ProceduralPlanet/DefaultPresets";

        [Serializable]
        private class Wrapper
        {
            public PlanetRuntimeSimplePresetData data;
        }

        public static bool SaveSlot(int slot, PlanetRuntimeSimplePresetData data, out string error)
        {
            error = null;
            if (slot < 0)
            {
                error = "Slot must be >= 0.";
                return false;
            }

            try
            {
                string path = GetSlotPath(slot);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                Wrapper wrapper = new Wrapper { data = data };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool LoadSlot(int slot, out PlanetRuntimeSimplePresetData data, out string error)
        {
            error = null;
            data = null;
            if (slot < 0)
            {
                error = "Slot must be >= 0.";
                return false;
            }

            try
            {
                string path = GetSlotPath(slot);
                if (!File.Exists(path))
                {
                    return LoadDefaultSlot(slot, out data, out error);
                }

                string json = File.ReadAllText(path);
                if (!TryDeserialize(json, out data))
                {
                    error = "Preset file is invalid.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool LoadDefaultSlot(int slot, out PlanetRuntimeSimplePresetData data, out string error)
        {
            data = null;
            error = null;

            TextAsset presetAsset = Resources.Load<TextAsset>(GetDefaultSlotResourcePath(slot));
            if (presetAsset == null)
            {
                error = $"Slot {slot + 1} is empty.";
                return false;
            }

            if (!TryDeserialize(presetAsset.text, out data))
            {
                error = $"Default slot {slot + 1} is invalid.";
                return false;
            }

            return true;
        }

        private static bool TryDeserialize(string json, out PlanetRuntimeSimplePresetData data)
        {
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);
            data = wrapper != null ? wrapper.data : null;
            return data != null;
        }

        private static string GetSlotPath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, UserPresetFolderName, $"slot_{slot}.json");
        }

        private static string GetDefaultSlotResourcePath(int slot)
        {
            return $"{DefaultPresetResourceFolder}/slot_{slot}";
        }
    }
}
