using UnityEngine;

namespace ProceduralPlanet
{
    [CreateAssetMenu()]
    public class ColorSettings : ScriptableObject
    {
        [Range(2, 512)]
        public int textureResolution = 50;
        public bool debugMode;
    
        public OceanSettings oceanSettings;
        
        [System.Serializable]
        public class OceanSettings
        {
            public Gradient oceanGradient;
            public Texture2D oceanNormalMap; 
            public float waveSpeed = 0.02f;  
            public float waveScale = 0.5f;
            [Range(0, 1)]
            public float normalStrength = 1f; 
        }
    
        public BiomeSettings biomeSettings;
    
        [System.Serializable]
        public class BiomeSettings 
        {
            [Range(0, 1)]
            public float blendAmount;
            public Biome[] biomes;
            public NoiseSettings temperatureNoise;
            [System.Serializable]
            public class Biome
            {
                public Gradient gradient;
                public Color tint;
    
                [Range(0, 1)]
                public float startHeight;
            }
        }
    
        public TerrainTextureSettings sand, grass, mountain, snow;
    
        [System.Serializable]
        public class TerrainTextureSettings
        {
            [Range(0, 1)]
            public float normalStrength = 1;
            public bool enabled = true;
            public Texture2D texture;
            public Texture2D normalMap;
            public Texture2D roughnessMap;
            public float tiling = 0.2f;
        }

        public TextureTransitionSettings transitions;
        [System.Serializable]
        public class TextureTransitionSettings
        {
            [Range(0, 1)] public float sandToGrassStart = 0.1f;
            [Range(0, 1)] public float sandToGrassEnd = 0.2f;
            
            [Range(0, 1)] public float grassToMountainStart = 0.4f;
            [Range(0, 1)] public float grassToMountainEnd = 0.5f;
    
            [Range(0, 1)] public float mountainToSnowStart = 0.7f;
            [Range(0, 1)] public float mountainToSnowEnd = 0.8f;
        }
    }
    
}
