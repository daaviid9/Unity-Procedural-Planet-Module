using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetColorSettingsUIController : MonoBehaviour
    {
        [Header("Target")]
        public Planet planet;

        [Header("Temperature UI")]
        public Transform temperatureRoot;
        public PlanetTemperatureNoiseUIItem temperatureItemPrefab;

        [Header("Ocean UI")]
        public Button oceanFoldoutButton;
        public TextMeshProUGUI oceanFoldoutArrowText;
        public GameObject oceanDetailsRoot;
        public PlanetGradientEditorUI oceanGradientEditor;

        [Header("Biomes UI")]
        public Button biomesFoldoutButton;
        public TextMeshProUGUI biomesFoldoutArrowText;
        public GameObject biomesDetailsRoot;
        public Transform biomesRoot;
        public PlanetBiomeUIItem biomeItemPrefab;
        public Button addBiomeButton;
        
        private PlanetTemperatureNoiseUIItem tempItemInstance;
        private List<PlanetBiomeUIItem> biomeItems = new List<PlanetBiomeUIItem>();
        private Dictionary<int, bool> expandedByIndex = new Dictionary<int, bool>();
        private bool isUpdatingUI = false;
        private bool oceanExpanded;
        private bool biomesExpanded;

        private void Start()
        {
            if (addBiomeButton != null) addBiomeButton.onClick.AddListener(AddBiome);
            if (biomesFoldoutButton != null) biomesFoldoutButton.onClick.AddListener(ToggleBiomesExpanded);
            if (oceanFoldoutButton != null) oceanFoldoutButton.onClick.AddListener(ToggleOceanExpanded);
            if (oceanGradientEditor != null) oceanGradientEditor.onGradientChanged.AddListener(OnOceanGradientChanged);
            
            RefreshFromPlanet();
        }

        public void RefreshFromPlanet()
        {
            if (planet == null || planet.colorSettings == null) return;
            isUpdatingUI = true;

            EnsureOceanSettings();
            if (oceanGradientEditor != null)
            {
                oceanGradientEditor.SetGradient(planet.colorSettings.oceanSettings.oceanGradient);
            }
            ApplyOceanExpandedState();

            if (tempItemInstance != null) Destroy(tempItemInstance.gameObject);
            if (temperatureRoot != null && temperatureItemPrefab != null)
            {
                tempItemInstance = Instantiate(temperatureItemPrefab, temperatureRoot);
                tempItemInstance.Initialize(this, planet.colorSettings.biomeSettings.temperatureNoise, false);
            }

            ClearBiomes();
            var biomes = planet.colorSettings.biomeSettings.biomes;
            if (biomes != null && biomesRoot != null && biomeItemPrefab != null)
            {
                for (int i = 0; i < biomes.Length; i++)
                {
                    PlanetBiomeUIItem item = Instantiate(biomeItemPrefab, biomesRoot);
                    bool exp = expandedByIndex.ContainsKey(i) && expandedByIndex[i];
                    item.Initialize(this, i, biomes[i], exp);
                    biomeItems.Add(item);
                }
            }

            ApplyBiomesExpandedState();
            isUpdatingUI = false;
            NotifyLayoutChanged();
        }

        private void OnOceanGradientChanged(Gradient gradient)
        {
            if (isUpdatingUI || planet == null || planet.colorSettings == null) return;

            EnsureOceanSettings();
            planet.colorSettings.oceanSettings.oceanGradient = gradient;
            AutoApply();
        }

        private void ToggleOceanExpanded()
        {
            oceanExpanded = !oceanExpanded;
            ApplyOceanExpandedState();
            NotifyLayoutChanged();
        }

        private void ApplyOceanExpandedState()
        {
            if (oceanDetailsRoot != null) oceanDetailsRoot.SetActive(oceanExpanded);
            if (oceanExpanded && oceanGradientEditor != null)
            {
                oceanGradientEditor.RefreshAfterLayout();
            }
            if (oceanFoldoutArrowText != null) oceanFoldoutArrowText.text = oceanExpanded ? "▼" : "►";
        }

        private void ToggleBiomesExpanded()
        {
            biomesExpanded = !biomesExpanded;
            ApplyBiomesExpandedState();
            NotifyLayoutChanged();
        }

        private void ApplyBiomesExpandedState()
        {
            GameObject target = biomesDetailsRoot != null
                ? biomesDetailsRoot
                : (biomesRoot != null ? biomesRoot.gameObject : null);

            if (target != null) target.SetActive(biomesExpanded);
            if (biomesFoldoutArrowText != null) biomesFoldoutArrowText.text = biomesExpanded ? "▼" : "►";
        }

        private void EnsureOceanSettings()
        {
            if (planet == null || planet.colorSettings == null) return;
            if (planet.colorSettings.oceanSettings == null)
            {
                planet.colorSettings.oceanSettings = new ColorSettings.OceanSettings();
            }
            if (planet.colorSettings.oceanSettings.oceanGradient == null)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.02f, 0.12f, 0.35f), 0f),
                        new GradientColorKey(new Color(0.15f, 0.65f, 0.9f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    });
                planet.colorSettings.oceanSettings.oceanGradient = gradient;
            }
        }

        public void ApplyTemperatureFromUi()
        {
            if (isUpdatingUI || planet == null || tempItemInstance == null) return;
            planet.colorSettings.biomeSettings.temperatureNoise = tempItemInstance.BuildNoiseSettings();
            AutoApply();
        }

        public void ApplyTemperatureGlobalsFromUi()
        {
            if (isUpdatingUI || planet == null) return;
            AutoApply();
        }

        public void ApplyBiomesFromUi()
        {
            if (isUpdatingUI || planet == null) return;
            
            ColorSettings.BiomeSettings.Biome[] next = new ColorSettings.BiomeSettings.Biome[biomeItems.Count];
            for (int i = 0; i < biomeItems.Count; i++)
            {
                next[i] = biomeItems[i].BuildBiomeData();
                expandedByIndex[i] = biomeItems[i].IsExpanded;
            }
            planet.colorSettings.biomeSettings.biomes = next;
            AutoApply();
        }

        public void AddBiome()
        {
            if (planet == null || planet.colorSettings == null) return;
            
            var existing = planet.colorSettings.biomeSettings.biomes ?? new ColorSettings.BiomeSettings.Biome[0];
            var next = new ColorSettings.BiomeSettings.Biome[existing.Length + 1];
            for (int i = 0; i < existing.Length; i++) next[i] = existing[i];
            
            next[next.Length - 1] = new ColorSettings.BiomeSettings.Biome
            {
                startHeight = 0.5f,
                tint = Color.white,
                gradient = new Gradient()
            };

            planet.colorSettings.biomeSettings.biomes = next;
            RefreshFromPlanet();
            AutoApply();
        }

        public void RemoveBiome(int index)
        {
            if (planet == null || planet.colorSettings == null) return;
            var layers = planet.colorSettings.biomeSettings.biomes;
            if (layers == null || layers.Length <= 1) return;

            var next = new ColorSettings.BiomeSettings.Biome[layers.Length - 1];
            int write = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                if (i == index) continue;
                next[write++] = layers[i];
            }

            planet.colorSettings.biomeSettings.biomes = next;
            RefreshFromPlanet();
            AutoApply();
        }

        private void ClearBiomes()
        {
            foreach (var b in biomeItems)
            {
                if (b != null) Destroy(b.gameObject);
            }
            biomeItems.Clear();
        }

        public void NotifyLayoutChanged()
        {
            Canvas.ForceUpdateCanvases();
            if (biomesRoot is RectTransform r1) LayoutRebuilder.ForceRebuildLayoutImmediate(r1);
            if (temperatureRoot is RectTransform r2) LayoutRebuilder.ForceRebuildLayoutImmediate(r2);
            if (transform is RectTransform r3) LayoutRebuilder.ForceRebuildLayoutImmediate(r3);
        }

        private void AutoApply()
        {
            if (planet != null && planet.autoUpdate)
            {
                planet.OnPlanetSettingsUpdated();
            }
        }
    }
}
