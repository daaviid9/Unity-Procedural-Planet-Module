namespace ProceduralPlanet
{
    public static class PlanetRuntimeClipboard
    {
        public static PlanetRuntimeSimplePresetData Data { get; private set; }
        public static bool HasData => Data != null;

        public static void CopyFrom(Planet planet)
        {
            if (planet == null) return;

            RegisterPlanetTextures(planet);
            Data = PlanetRuntimeSimplePresetMapper.Capture(planet);
        }

        public static bool PasteTo(Planet planet)
        {
            if (planet == null || Data == null) return false;

            PlanetRuntimeSimplePresetMapper.Apply(planet, Data);
            planet.GeneratePlanet();
            return true;
        }

        private static void RegisterPlanetTextures(Planet planet)
        {
            if (planet.colorSettings == null) return;

            if (planet.colorSettings.oceanSettings != null)
            {
                PlanetRuntimeTextureRegistry.Register(planet.colorSettings.oceanSettings.oceanNormalMap);
            }

            RegisterTerrainTextures(planet.colorSettings.sand);
            RegisterTerrainTextures(planet.colorSettings.grass);
            RegisterTerrainTextures(planet.colorSettings.mountain);
            RegisterTerrainTextures(planet.colorSettings.snow);
        }

        private static void RegisterTerrainTextures(ColorSettings.TerrainTextureSettings settings)
        {
            if (settings == null) return;

            PlanetRuntimeTextureRegistry.Register(settings.texture);
            PlanetRuntimeTextureRegistry.Register(settings.normalMap);
            PlanetRuntimeTextureRegistry.Register(settings.roughnessMap);
        }
    }
}
