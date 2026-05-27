using Unity.Mathematics;

namespace ProceduralPlanet
{
    // Job-friendly settings for one noise layer.
    public struct NoiseLayerStruct
    {
        public float strength;
        public float baseRoughness;
        public float roughness;
        public float persistence;
        public float3 centre;
        public float minValue;
        public int numLayers;
        public bool enabled;
        public bool useFirstLayerAsMask;
        public int filterType; // 0 = Simple, 1 = Ridgid
        public float weightMultiplier;
    }

    // Job-friendly settings for one biome.
    public struct BiomeStruct
    {
        public float startHeight;
    }

    public static class NoiseMath
    {
        // Burst-compatible simplex noise from Unity.Mathematics.
        public static float Evaluate(float3 point, NoiseLayerStruct settings)
        {
            float noiseValue = 0;
            float frequency = settings.baseRoughness;
            float amplitude = 1;

            for (int i = 0; i < settings.numLayers; i++)
            {
                float v = noise.snoise(point * frequency + settings.centre);
                noiseValue += (v + 1) * 0.5f * amplitude;
                frequency *= settings.roughness;
                amplitude *= settings.persistence;
            }

            noiseValue -= settings.minValue;
            return noiseValue * settings.strength;
        }

        public static float EvaluateRidgid(float3 point, NoiseLayerStruct settings)
        {
            float noiseValue = 0;
            float frequency = settings.baseRoughness;
            float amplitude = 1;
            float weight = 1;

            for (int i = 0; i < settings.numLayers; i++)
            {
                float v = 1 - math.abs(noise.snoise(point * frequency + settings.centre));
                v *= v;
                v *= weight;
                weight = math.clamp(v * settings.weightMultiplier, 0, 1);

                noiseValue += v * amplitude;
                frequency *= settings.roughness;
                amplitude *= settings.persistence;
            }

            noiseValue -= settings.minValue;
            return noiseValue * settings.strength;
        }
    }
}
