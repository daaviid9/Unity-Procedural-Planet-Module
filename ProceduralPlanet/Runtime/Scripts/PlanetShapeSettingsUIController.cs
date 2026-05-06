using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetShapeSettingsUIController : MonoBehaviour
    {
        [Header("Target")]
        public Planet planet;

        [Header("UI")]
        public Transform layersRoot;
        public PlanetNoiseLayerUIItem layerItemPrefab;
        public Button addLayerButton;
        public Button noiseLayersFoldoutButton;
        public TextMeshProUGUI noiseLayersFoldoutArrowText;
        public GameObject noiseLayersDetailsRoot;
        public int maxLayers = 8;

        private readonly List<PlanetNoiseLayerUIItem> layerItems = new List<PlanetNoiseLayerUIItem>();
        private readonly Dictionary<int, bool> expandedByIndex = new Dictionary<int, bool>();
        private int lastAddFrame = -1;
        private bool noiseLayersExpanded;

        protected void Start()
        {
            ResolveLayersRoot();
            HideSceneTemplateItem();

            if (addLayerButton != null)
            {
                addLayerButton.onClick.RemoveListener(AddLayer);
                addLayerButton.onClick.AddListener(AddLayer);
            }
            if (noiseLayersFoldoutButton != null)
            {
                noiseLayersFoldoutButton.onClick.AddListener(ToggleNoiseLayersExpanded);
            }
            RefreshFromPlanet();
        }

        public void RefreshFromPlanet()
        {
            ResolveLayersRoot();

            if (planet == null || planet.shapeSettings == null || layersRoot == null || layerItemPrefab == null)
            {
                return;
            }

            ClearItems();

            ShapeSettings.NoiseLayer[] layers = planet.shapeSettings.noiseLayers;
            if (layers == null || layers.Length == 0)
            {
                AddDefaultLayerToPlanet();
                layers = planet.shapeSettings.noiseLayers;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                SpawnLayerItem(i, layers[i]);
            }

            ApplyNoiseLayersExpandedState();
            ForceRebuildLayersLayout();
        }

        public void AddLayer()
        {
            if (lastAddFrame == Time.frameCount)
            {
                return;
            }
            lastAddFrame = Time.frameCount;

            if (planet == null || planet.shapeSettings == null)
            {
                return;
            }

            ShapeSettings.NoiseLayer[] existing = planet.shapeSettings.noiseLayers ?? new ShapeSettings.NoiseLayer[0];
            if (existing.Length >= maxLayers)
            {
                return;
            }

            ShapeSettings.NoiseLayer[] next = new ShapeSettings.NoiseLayer[existing.Length + 1];
            for (int i = 0; i < existing.Length; i++) next[i] = existing[i];
            next[next.Length - 1] = CreateDefaultLayer();
            planet.shapeSettings.noiseLayers = next;

            RefreshFromPlanet();
            AutoApply();
        }

        public void RemoveLayer(int index)
        {
            if (planet == null || planet.shapeSettings == null || planet.shapeSettings.noiseLayers == null)
            {
                return;
            }

            ShapeSettings.NoiseLayer[] layers = planet.shapeSettings.noiseLayers;
            if (layers.Length <= 1 || index < 0 || index >= layers.Length)
            {
                return;
            }

            ShapeSettings.NoiseLayer[] next = new ShapeSettings.NoiseLayer[layers.Length - 1];
            int write = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                if (i == index) continue;
                next[write++] = layers[i];
            }

            planet.shapeSettings.noiseLayers = next;
            RefreshFromPlanet();
            AutoApply();
        }

        public void ApplyLayersFromUi()
        {
            if (planet == null || planet.shapeSettings == null)
            {
                return;
            }

            ShapeSettings.NoiseLayer[] output = new ShapeSettings.NoiseLayer[layerItems.Count];
            for (int i = 0; i < layerItems.Count; i++)
            {
                output[i] = layerItems[i].BuildLayerData();
            }

            planet.shapeSettings.noiseLayers = output;
            AutoApply();
        }

        private void AutoApply()
        {
            if (planet != null && planet.autoUpdate)
            {
                planet.GeneratePlanet();
            }
        }

        private void SpawnLayerItem(int index, ShapeSettings.NoiseLayer data)
        {
            PlanetNoiseLayerUIItem item = Instantiate(layerItemPrefab, layersRoot);
            item.gameObject.SetActive(true);
            bool startExpanded = expandedByIndex.ContainsKey(index) && expandedByIndex[index];
            item.Initialize(this, index, data ?? CreateDefaultLayer(), startExpanded);
            layerItems.Add(item);
        }

        private void ClearItems()
        {
            for (int i = 0; i < layerItems.Count; i++)
            {
                if (layerItems[i] != null)
                {
                    layerItems[i].gameObject.SetActive(false);
                    Destroy(layerItems[i].gameObject);
                }
            }
            layerItems.Clear();

            PlanetNoiseLayerUIItem[] childItems = GetComponentsInChildren<PlanetNoiseLayerUIItem>(true);
            for (int i = 0; i < childItems.Length; i++)
            {
                if (childItems[i] == null || childItems[i] == layerItemPrefab)
                {
                    continue;
                }

                if (childItems[i].gameObject.name.Contains("(Clone)"))
                {
                    childItems[i].gameObject.SetActive(false);
                    Destroy(childItems[i].gameObject);
                }
            }
        }

        private void AddDefaultLayerToPlanet()
        {
            planet.shapeSettings.noiseLayers = new[] { CreateDefaultLayer() };
        }

        private ShapeSettings.NoiseLayer CreateDefaultLayer()
        {
            return new ShapeSettings.NoiseLayer
            {
                enabled = true,
                useFirstLayerAsMask = false,
                noiseSettings = new NoiseSettings
                {
                    filterType = NoiseSettings.FilterType.Simple,
                    simpleNoiseSettings = new NoiseSettings.SimpleNoiseSettings(),
                    ridgidNoiseSettings = new NoiseSettings.RidgidNoiseSettings()
                }
            };
        }

        private void ToggleNoiseLayersExpanded()
        {
            noiseLayersExpanded = !noiseLayersExpanded;
            ApplyNoiseLayersExpandedState();
            ForceRebuildLayersLayout();
        }

        private void ApplyNoiseLayersExpandedState()
        {
            GameObject target = noiseLayersDetailsRoot != null
                ? noiseLayersDetailsRoot
                : (layersRoot != null ? layersRoot.gameObject : null);

            if (target != null) target.SetActive(noiseLayersExpanded);
            if (noiseLayersFoldoutArrowText != null) noiseLayersFoldoutArrowText.text = noiseLayersExpanded ? "▼" : "►";
        }

        public void NotifyLayerFoldoutChanged(int index, bool isExpanded)
        {
            expandedByIndex[index] = isExpanded;
            ForceRebuildLayersLayout();
        }

        private void ForceRebuildLayersLayout()
        {
            Canvas.ForceUpdateCanvases();
            if (layersRoot is RectTransform rect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }

        private void ResolveLayersRoot()
        {
            Transform found = transform.Find("LayersRoot");
            if (found == null)
            {
                Transform[] children = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null && children[i].name == "LayersRoot")
                    {
                        found = children[i];
                        break;
                    }
                }
            }

            if (found != null)
            {
                layersRoot = found;
            }
        }

        private void HideSceneTemplateItem()
        {
            if (layerItemPrefab != null && layerItemPrefab.transform.IsChildOf(transform))
            {
                layerItemPrefab.gameObject.SetActive(false);
            }
        }
    }
}
