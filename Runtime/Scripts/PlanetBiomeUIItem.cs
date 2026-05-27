using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetBiomeUIItem : MonoBehaviour
    {
        [Header("Main Header")]
        public TextMeshProUGUI titleText;
        public Button removeButton;
        public Button foldoutButton;
        public TextMeshProUGUI foldoutArrowText;
        public GameObject detailsRoot;
        public LayoutElement layoutElement;
        public float collapsedHeight = 44f;

        [Header("Start Height")]
        public Slider startHeightSlider;
        public TextMeshProUGUI startHeightText;

        [Header("Tint Foldout")]
        public Button tintFoldoutButton;
        public TextMeshProUGUI tintArrowText;
        public GameObject tintDetailsRoot;
        public Image tintPreviewImage;
        
        [Header("Tint Fields")]
        public Slider tintRSlider;
        public TextMeshProUGUI tintRText;
        public Slider tintGSlider;
        public TextMeshProUGUI tintGText;
        public Slider tintBSlider;
        public TextMeshProUGUI tintBText;

        [Header("Gradient Foldout")]
        public Button gradientFoldoutButton;
        public TextMeshProUGUI gradientArrowText;
        public GameObject gradientDetailsRoot;
        public PlanetGradientEditorUI gradientEditor;

        private PlanetColorSettingsUIController owner;
        private int biomeIndex;
        private bool expanded;
        private bool tintExpanded;
        private bool gradientExpanded;
        private bool isUpdatingUI = false;
        private Gradient currentGradient;

        public void Initialize(PlanetColorSettingsUIController controller, int index, ColorSettings.BiomeSettings.Biome biome, bool startExpanded)
        {
            owner = controller;
            biomeIndex = index;
            expanded = startExpanded;
            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();

            if (titleText != null) titleText.text = $"Biome {index + 1}";

            isUpdatingUI = true;
            if (startHeightSlider != null) startHeightSlider.SetValueWithoutNotify(biome.startHeight);
            if (tintRSlider != null) tintRSlider.SetValueWithoutNotify(biome.tint.r);
            if (tintGSlider != null) tintGSlider.SetValueWithoutNotify(biome.tint.g);
            if (tintBSlider != null) tintBSlider.SetValueWithoutNotify(biome.tint.b);
            
            UpdateLabels();

            currentGradient = biome.gradient ?? new Gradient();
            if (gradientEditor != null) gradientEditor.SetGradient(currentGradient);
            
            isUpdatingUI = false;

            ApplyExpandedState();
            ApplyTintExpandedState();
            ApplyGradientExpandedState();
            
            BindEvents();
        }

        public bool IsExpanded => expanded;

        public ColorSettings.BiomeSettings.Biome BuildBiomeData()
        {
            return new ColorSettings.BiomeSettings.Biome
            {
                startHeight = startHeightSlider != null ? startHeightSlider.value : 0f,
                tint = new Color(
                    tintRSlider != null ? tintRSlider.value : 1f,
                    tintGSlider != null ? tintGSlider.value : 1f,
                    tintBSlider != null ? tintBSlider.value : 1f,
                    1f
                ),
                gradient = currentGradient
            };
        }

        private void BindEvents()
        {
            if (startHeightSlider != null) startHeightSlider.onValueChanged.AddListener(_ => OnValueChanged());
            if (tintRSlider != null) tintRSlider.onValueChanged.AddListener(_ => OnValueChanged());
            if (tintGSlider != null) tintGSlider.onValueChanged.AddListener(_ => OnValueChanged());
            if (tintBSlider != null) tintBSlider.onValueChanged.AddListener(_ => OnValueChanged());
            
            if (gradientEditor != null) gradientEditor.onGradientChanged.AddListener(OnGradientChanged);
            
            if (removeButton != null) removeButton.onClick.AddListener(OnRemoveClicked);
            if (foldoutButton != null) foldoutButton.onClick.AddListener(ToggleExpanded);
            
            if (tintFoldoutButton != null) tintFoldoutButton.onClick.AddListener(ToggleTintExpanded);
            if (gradientFoldoutButton != null) gradientFoldoutButton.onClick.AddListener(ToggleGradientExpanded);
        }

        private void UpdateLabels()
        {
            if (startHeightText != null && startHeightSlider != null) 
                startHeightText.text = $"Start Height: {startHeightSlider.value.ToString("0.00")}";
                
            if (tintRText != null && tintRSlider != null) 
                tintRText.text = $"R: {Mathf.RoundToInt(tintRSlider.value * 255f)}";
                
            if (tintGText != null && tintGSlider != null) 
                tintGText.text = $"G: {Mathf.RoundToInt(tintGSlider.value * 255f)}";
                
            if (tintBText != null && tintBSlider != null) 
                tintBText.text = $"B: {Mathf.RoundToInt(tintBSlider.value * 255f)}";

            if (tintPreviewImage != null && tintRSlider != null && tintGSlider != null && tintBSlider != null)
            {
                tintPreviewImage.color = new Color(tintRSlider.value, tintGSlider.value, tintBSlider.value, 1f);
            }
        }

        private void OnGradientChanged(Gradient g)
        {
            currentGradient = g;
            OnValueChanged();
        }

        private void OnValueChanged()
        {
            UpdateLabels();
            if (!isUpdatingUI) owner?.ApplyBiomesFromUi();
        }

        private void OnRemoveClicked()
        {
            owner?.RemoveBiome(biomeIndex);
        }

        private void ToggleExpanded()
        {
            expanded = !expanded;
            ApplyExpandedState();
            owner?.NotifyLayoutChanged();
        }

        private void ApplyExpandedState()
        {
            if (detailsRoot != null) detailsRoot.SetActive(expanded);
            if (foldoutArrowText != null) foldoutArrowText.text = expanded ? "▼" : "►";
            if (layoutElement != null) layoutElement.preferredHeight = expanded ? -1f : collapsedHeight;
        }

        private void ToggleTintExpanded()
        {
            tintExpanded = !tintExpanded;
            ApplyTintExpandedState();
            owner?.NotifyLayoutChanged();
        }

        private void ApplyTintExpandedState()
        {
            if (tintDetailsRoot != null) tintDetailsRoot.SetActive(tintExpanded);
            if (tintArrowText != null) tintArrowText.text = tintExpanded ? "▼" : "►";
        }

        private void ToggleGradientExpanded()
        {
            gradientExpanded = !gradientExpanded;
            ApplyGradientExpandedState();
            owner?.NotifyLayoutChanged();
        }

        private void ApplyGradientExpandedState()
        {
            if (gradientDetailsRoot != null) gradientDetailsRoot.SetActive(gradientExpanded);
            if (gradientExpanded && gradientEditor != null)
            {
                gradientEditor.RefreshAfterLayout();
            }
            if (gradientArrowText != null) gradientArrowText.text = gradientExpanded ? "▼" : "►";
        }
    }
}
