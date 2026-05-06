using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace ProceduralPlanet {
    [BurstCompile]
    public struct TerrainJob : IJobParallelFor {
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<NoiseLayerStruct> shapeLayers;
        [ReadOnly] public NativeArray<BiomeStruct> biomes;
        
        public float planetRadius;
        public float blendAmount;
        public float3 temperatureNoiseCentre;
        public NoiseLayerStruct temperatureNoiseSettings;

        // Výstupy
        public NativeArray<Vector3> outVertices;
        public NativeArray<Vector2> outUVs;

        public void Execute(int i) {
            float3 pointOnSphere = vertices[i];
            
            // 1. Výpočet elevácie
            float firstLayerValue = 0;
            float elevation = 0;

            if (shapeLayers.Length > 0 && shapeLayers[0].enabled) {
                firstLayerValue = (shapeLayers[0].filterType == 0) ? 
                    NoiseMath.Evaluate(pointOnSphere, shapeLayers[0]) : 
                    NoiseMath.EvaluateRidgid(pointOnSphere, shapeLayers[0]);
                elevation = firstLayerValue;
            }

            for (int j = 1; j < shapeLayers.Length; j++) {
                if (shapeLayers[j].enabled) {
                    float mask = (shapeLayers[j].useFirstLayerAsMask) ? firstLayerValue : 1;
                    float v = (shapeLayers[j].filterType == 0) ? 
                        NoiseMath.Evaluate(pointOnSphere, shapeLayers[j]) : 
                        NoiseMath.EvaluateRidgid(pointOnSphere, shapeLayers[j]);
                    elevation += v * mask;
                }
            }

            // Implementácia GetScaledElevation (Ocean clamping)
            float unscaledElevation = planetRadius * (1 + elevation);
            float clampedElevation = math.max(unscaledElevation, planetRadius);
            
            outVertices[i] = (Vector3)pointOnSphere * clampedElevation;

            // 2. Výpočet biomu (Plynulé miešanie s blendAmount)
            float latitude = math.abs(pointOnSphere.y);
            float temperature = 1 - latitude;
            temperature += NoiseMath.Evaluate(pointOnSphere, temperatureNoiseSettings) * 0.1f;

            float biomePercent = 0;
            float blendRange = blendAmount / 2f + 0.001f;

            for (int j = 0; j < biomes.Length; j++) {
                float dst = temperature - biomes[j].startHeight;
                float weight = math.clamp((dst + blendRange) / (blendRange * 2f), 0, 1);
                biomePercent = biomePercent * (1 - weight) + j * weight;
            }

            // 3. Zápis UV (X = skutočná výška pre farbenie, Y = biome)
            float biomeUV = (biomePercent + 0.5f) / math.max(1, biomes.Length);
            outUVs[i] = new Vector2(unscaledElevation, biomeUV);
        }
    }
}
