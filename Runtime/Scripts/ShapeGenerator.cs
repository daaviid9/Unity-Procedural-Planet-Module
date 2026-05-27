using UnityEngine;

namespace ProceduralPlanet
{
    public class ShapeGenerator
    {
        ShapeSettings settings;
        INoiseFilter[] noiseFilters;
        public float minElevationHeight;
        public float maxElevationHeight;
        public bool updateBounds = true;
    
        public ShapeGenerator(ShapeSettings settings)
        {
            this.settings = settings;
            noiseFilters = new INoiseFilter[settings.noiseLayers.Length];
            for (int i = 0; i < noiseFilters.Length; i++)
            {
                noiseFilters[i] = NoiseFilterFactory.CreateNoiseFilter(settings.noiseLayers[i].noiseSettings);
            }
        }
    
        public float CalculateUnscaledElevation(Vector3 pointOnUnitSphere)
        {
            float firstLayerValue = 0;
            float elevation = 0;
    
            if (noiseFilters.Length > 0)
            {
                firstLayerValue = noiseFilters[0].Evaluate(pointOnUnitSphere);
                if (settings.noiseLayers[0].enabled)
                {
                    elevation = firstLayerValue;
                }
            }
    
            for (int i = 1; i < noiseFilters.Length; i++)
            {
                if (settings.noiseLayers[i].enabled)
                {
                    float mask = (settings.noiseLayers[i].useFirstLayerAsMask) ? firstLayerValue : 1;
                    elevation += noiseFilters[i].Evaluate(pointOnUnitSphere) * mask;
                }
            }
            float height = settings.planetRadius * (elevation + 1);
    
            if (updateBounds)
            {
                minElevationHeight = Mathf.Min(minElevationHeight, height);
                maxElevationHeight = Mathf.Max(maxElevationHeight, height);
            }
    
            return elevation;
        }
    
        public float GetScaledElevation(float unscaledElevation, bool clampToRadius = false)
        {
            float elevation = unscaledElevation;
            if (clampToRadius)
            {
                elevation = Mathf.Max(0, elevation);
            }
            elevation = settings.planetRadius * (1 + elevation);
            return elevation;
        }
    }
}
