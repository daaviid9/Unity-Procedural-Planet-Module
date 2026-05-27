using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace ProceduralPlanet
{
    [BurstCompile]
    public struct TerrainJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<NoiseLayerStruct> shapeLayers;
        [ReadOnly] public NativeArray<BiomeStruct> biomes;

        public float planetRadius;
        public float blendAmount;
        public NoiseLayerStruct temperatureNoiseSettings;

        public NativeArray<Vector3> outVertices;
        public NativeArray<Vector2> outUVs;

        public void Execute(int i)
        {
            float3 pointOnSphere = vertices[i];
            float elevation = CalculateElevation(pointOnSphere);
            float unscaledElevation = planetRadius * (1 + elevation);
            float clampedElevation = math.max(unscaledElevation, planetRadius);

            outVertices[i] = (Vector3)pointOnSphere * clampedElevation;
            outUVs[i] = new Vector2(unscaledElevation, CalculateBiomeUV(pointOnSphere));
        }

        private float CalculateElevation(float3 pointOnSphere)
        {
            float firstLayerValue = 0;
            float elevation = 0;

            if (shapeLayers.Length > 0 && shapeLayers[0].enabled)
            {
                firstLayerValue = EvaluateNoise(pointOnSphere, shapeLayers[0]);
                elevation = firstLayerValue;
            }

            for (int j = 1; j < shapeLayers.Length; j++)
            {
                if (!shapeLayers[j].enabled)
                {
                    continue;
                }

                float mask = shapeLayers[j].useFirstLayerAsMask ? firstLayerValue : 1;
                elevation += EvaluateNoise(pointOnSphere, shapeLayers[j]) * mask;
            }

            return elevation;
        }

        private float CalculateBiomeUV(float3 pointOnSphere)
        {
            float latitude = math.abs(pointOnSphere.y);
            float temperature = 1 - latitude;
            temperature += NoiseMath.Evaluate(pointOnSphere, temperatureNoiseSettings) * 0.1f;

            float biomePercent = 0;
            float blendRange = blendAmount / 2f + 0.001f;

            for (int j = 0; j < biomes.Length; j++)
            {
                float distanceFromBiomeStart = temperature - biomes[j].startHeight;
                float weight = math.clamp((distanceFromBiomeStart + blendRange) / (blendRange * 2f), 0, 1);
                biomePercent = biomePercent * (1 - weight) + j * weight;
            }

            return (biomePercent + 0.5f) / math.max(1, biomes.Length);
        }

        private static float EvaluateNoise(float3 point, NoiseLayerStruct settings)
        {
            return settings.filterType == 0
                ? NoiseMath.Evaluate(point, settings)
                : NoiseMath.EvaluateRidgid(point, settings);
        }
    }
}
