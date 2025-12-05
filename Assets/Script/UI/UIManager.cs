using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;


    [SerializeField] private TextMeshProUGUI currentTxt;
    [SerializeField] private TextMeshProUGUI maxTxt;

    private void Start()
    {
        if (Instance != null)
        {
            return;
        }
        Instance = this;
    }

    public void UpdateHealthHUD(float current, float max)
    {
        if (currentTxt != null)
            currentTxt.text = current.ToString();

        if (maxTxt != null)
            maxTxt.text = max.ToString();
    }

}
