using UnityEngine;
using TMPro;

namespace ProceduralPlanet
{
    public class PlanetSettingsTabController : MonoBehaviour
    {
        public TMP_Dropdown tabDropdown;
        public GameObject[] tabContents;

        private void Start()
        {
            if (tabDropdown != null)
            {
                tabDropdown.onValueChanged.AddListener(OnTabChanged);
                // Match the initial dropdown value.
                OnTabChanged(tabDropdown.value);
            }
        }

        private void OnTabChanged(int index)
        {
            if (tabContents == null) return;

            for (int i = 0; i < tabContents.Length; i++)
            {
                if (tabContents[i] != null)
                {
                    tabContents[i].SetActive(i == index);
                }
            }
        }
    }
}
