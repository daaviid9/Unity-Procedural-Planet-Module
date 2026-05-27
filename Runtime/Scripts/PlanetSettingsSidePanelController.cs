using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    public class PlanetSettingsSidePanelController : MonoBehaviour
    {
        public enum SlideFrom
        {
            Right,
            Left
        }

        [System.Serializable]
        public class SettingsTab
        {
            public string title;
            public Button iconButton;
            public GameObject content;
        }

        [Header("Panel")]
        public RectTransform panel;
        public SlideFrom slideFrom = SlideFrom.Right;
        public GameObject iconButtonsRoot;
        public Button collapseButton;
        public TextMeshProUGUI titleText;
        public float animationTime = 0.18f;

        [Header("Tabs")]
        public SettingsTab[] tabs;

        private Vector2 shownPosition;
        private Vector2 hiddenPosition;
        private Coroutine slideRoutine;
        private int activeIndex = -1;
        private bool isOpen;

        private void Awake()
        {
            if (panel != null)
            {
                shownPosition = panel.anchoredPosition;
                float direction = slideFrom == SlideFrom.Right ? 1f : -1f;
                hiddenPosition = shownPosition + new Vector2(panel.rect.width + 5, 0f) * direction;
                panel.anchoredPosition = hiddenPosition;
            }

            if (collapseButton != null) collapseButton.onClick.AddListener(Collapse);

            if (tabs == null) return;
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                if (tabs[i].iconButton != null)
                {
                    tabs[i].iconButton.onClick.AddListener(() => Open(index));
                }

                if (tabs[i].content != null) tabs[i].content.SetActive(false);
            }
        }

        public void Open(int index)
        {
            if (tabs == null || index < 0 || index >= tabs.Length) return;

            activeIndex = index;
            isOpen = true;

            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i].content != null) tabs[i].content.SetActive(i == activeIndex);
            }

            if (titleText != null) titleText.text = tabs[activeIndex].title;
            if (iconButtonsRoot != null) iconButtonsRoot.SetActive(false);
            SlideTo(shownPosition);
        }

        public void Collapse()
        {
            isOpen = false;
            SlideTo(hiddenPosition);
        }

        private void SlideTo(Vector2 target)
        {
            if (panel == null) return;

            if (slideRoutine != null) StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(SlidePanel(target));
        }

        private IEnumerator SlidePanel(Vector2 target)
        {
            Vector2 start = panel.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < animationTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = animationTime <= 0f ? 1f : Mathf.Clamp01(elapsed / animationTime);
                t = 1f - Mathf.Pow(1f - t, 3f);
                panel.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                yield return null;
            }

            panel.anchoredPosition = target;

            if (!isOpen)
            {
                for (int i = 0; tabs != null && i < tabs.Length; i++)
                {
                    if (tabs[i].content != null) tabs[i].content.SetActive(false);
                }

                if (iconButtonsRoot != null) iconButtonsRoot.SetActive(true);
            }
        }
    }
}
