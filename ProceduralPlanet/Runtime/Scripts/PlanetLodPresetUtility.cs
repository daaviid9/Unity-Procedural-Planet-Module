namespace ProceduralPlanet
{
    public static class PlanetLodPresetUtility
    {
        public const int High = 0;
        public const int Medium = 1;
        public const int Low = 2;

        public static void ApplyPreset(Planet planet, int index)
        {
            if (planet == null) return;

            switch (index)
            {
                case High:
                    planet.resolution = 256;
                    planet.lodLevels = new[]
                    {
                        new Planet.LODLevel { resolution = 256, distance = 240f },
                        new Planet.LODLevel { resolution = 128, distance = 460f },
                        new Planet.LODLevel { resolution = 48, distance = 760f }
                    };
                    break;
                case Medium:
                    planet.resolution = 128;
                    planet.lodLevels = new[]
                    {
                        new Planet.LODLevel { resolution = 128, distance = 180f },
                        new Planet.LODLevel { resolution = 64, distance = 340f },
                        new Planet.LODLevel { resolution = 24, distance = 560f }
                    };
                    break;
                default:
                    planet.resolution = 64;
                    planet.lodLevels = new[]
                    {
                        new Planet.LODLevel { resolution = 64, distance = 140f },
                        new Planet.LODLevel { resolution = 32, distance = 260f },
                        new Planet.LODLevel { resolution = 16, distance = 420f }
                    };
                    break;
            }
        }

        public static string GetPresetName(int index)
        {
            switch (index)
            {
                case High: return "High";
                case Medium: return "Medium";
                default: return "Low";
            }
        }
    }
}
