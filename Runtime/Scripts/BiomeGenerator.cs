using UnityEngine;

namespace ProceduralPlanet
{
    public class BiomeGenerator
    {
        ColorSettings settings;
    
        INoiseFilter temperatureNoise;
    
        int biomeCount;
    
        public BiomeGenerator(ColorSettings settings)
        {
            this.settings = settings;
    
            temperatureNoise = NoiseFilterFactory.CreateNoiseFilter(settings.biomeSettings.temperatureNoise);
            
    
            biomeCount = (settings != null && settings.biomeSettings != null && settings.biomeSettings.biomes != null)
                ? settings.biomeSettings.biomes.Length
                : 1;
        }
    
        public float GetBiomePercent(Vector3 pointOnSphere)
        {
            float latitude = Mathf.Abs(pointOnSphere.y);
    
            float temperature = 1 - latitude;
            temperature += temperatureNoise.Evaluate(pointOnSphere) * 0.1f;
    
            temperature = Mathf.Clamp01(temperature);
    
            var biomes = settings.biomeSettings.biomes;
            float blendRange = settings.biomeSettings.blendAmount / 2f + 0.001f;
    
            float biomeIndex = 0;
    
            for (int i = 0; i < biomes.Length; i++)
            {
                float dst = temperature - biomes[i].startHeight;
                float weight = Mathf.InverseLerp(-blendRange, blendRange, dst);
                biomeIndex *= (1 - weight);
                biomeIndex += i * weight;
            }
    
            biomeIndex /= Mathf.Max(1, biomes.Length - 1);
    
            return biomeIndex;
        }
    
        // Convert normalized biomeIndex (0..1) into texture V coordinate that samples the center of the biome row.
        public float BiomeIndexToTextureV(float biomeIndexNormalized)
        {
            // Protect from division by zero
            if (biomeCount <= 0) return 0f;
            // row = biomeIndexNormalized * (biomeCount - 1)
            float row = Mathf.Clamp01(biomeIndexNormalized) * (biomeCount - 1);
            // sample in the vertical center of that row: (row + 0.5) / biomeCount
            float v = (row + 0.5f) / (float)biomeCount;
            return Mathf.Clamp01(v);
        }
    }
}
