using UnityEngine;

namespace ProceduralPlanet
{
    public class ColorGenerator
    {
        ColorSettings settings;
    
        public ColorGenerator(ColorSettings settings)
        {
            this.settings = settings;
        }
    
        public Texture2D GenerateGradientTexture(ShapeGenerator shapeGenerator)
        {
            var biomes = settings?.biomeSettings?.biomes;
            if (biomes == null || biomes.Length == 0)
            {
                Debug.LogError("Biome gradients are not set!");
                return Texture2D.blackTexture;
            }
    
            int width = settings.textureResolution;
            int height = biomes.Length;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
    
            float minHeight = shapeGenerator.minElevationHeight;
            float maxHeight = shapeGenerator.maxElevationHeight;
            float radius = shapeGenerator.GetScaledElevation(0); // radius is elevation 0
    
            float seaLevelPercent = Mathf.InverseLerp(minHeight, maxHeight, radius);
    
            for (int y = 0; y < height; y++){
                float biomePercent = y / (float)(height - 1);
    
                int biomeIndex = Mathf.FloorToInt(biomePercent * biomes.Length);
                biomeIndex = Mathf.Clamp(biomeIndex, 0, biomes.Length - 1);
    
                for (int x = 0; x < width; x++){
                    float t = x / (float)(width - 1);
                    Color color;
    
                    if (settings.debugMode)
                    {
                        color = biomes[biomeIndex].tint;
                    }
                    else
                    {
                        if (t <= seaLevelPercent)
                        {
                            // depth: 0 at deepest, 1 at surface
                            float depthT = Mathf.InverseLerp(0, seaLevelPercent, t);
                            Gradient oceanGradient = settings.oceanSettings != null ? settings.oceanSettings.oceanGradient : null;
                            color = oceanGradient != null ? oceanGradient.Evaluate(depthT) : Color.blue;
                        }
                        else
                        {
                            // land: map t from [seaLevelPercent, 1] to [0, 1] for biome gradient
                            float landT = Mathf.InverseLerp(seaLevelPercent, 1, t);
                            color = biomes[biomeIndex].gradient.Evaluate(landT);
                        }
                    }
    
                    texture.SetPixel(x, y, color);
                }
            }
    
            texture.Apply();
            return texture;
        }
    }
    
}
