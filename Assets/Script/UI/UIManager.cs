using TMPro;
using UnityEngine;

namespace Script.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;


        [SerializeField] private TextMeshProUGUI currentTxt;
        [SerializeField] private TextMeshProUGUI maxTxt;

        [SerializeField] private TextMeshProUGUI currentAmmo;
        [SerializeField] private TextMeshProUGUI stockAmmo;

        private void Start()
        {
            if (Instance)
            {
                return;
            }
            Instance = this;
        }

        public void UpdateHealthHUD(float current, float max)
        {
            if (currentTxt)
                currentTxt.text = current.ToString();

            if (maxTxt)
                maxTxt.text = max.ToString();
        }

        public void UpdateAmmoHUD(float current, float stock)
        {
            if(currentAmmo)
                currentAmmo.text = current.ToString();
            if(stockAmmo)
                stockAmmo.text = stock.ToString();
        }
    }
}
