using UnityEngine;
using UnityEngine.UI;

namespace ProceduralPlanet
{
    [RequireComponent(typeof(Toggle))]
    public class PlanetHideUIController : MonoBehaviour
    {
        public Toggle hideUiToggle;
        public GameObject planetSettingsPanel;

        private void Start()
        {
            if (hideUiToggle == null) hideUiToggle = GetComponent<Toggle>();
            if (hideUiToggle != null)
            {
                hideUiToggle.onValueChanged.AddListener(SetHidden);
                SetHidden(hideUiToggle.isOn);
            }
        }

        public void SetHidden(bool hidden)
        {
            if (planetSettingsPanel != null)
            {
                planetSettingsPanel.SetActive(hidden);
            }
        }
    }
}
