using System;
using System.Collections;
using UnityEngine;

public class LaternSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] public Light laternDirectionalLight;

    [Header("Settings")]
    [SerializeField] public float switchDuration = 4f;
    [SerializeField] public float baseIntensity = 1f;

    [Header("Colors")]
    [SerializeField] public Color firstColor;
    [SerializeField] public Color secondColor;

    private Coroutine _activeTransition;

    private void Awake()
    {
        laternDirectionalLight.color = firstColor;

        playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (playerHealth)
        {
            playerHealth.OnPlayerHealthChanged += UpdateLightIntensity;
        }
    }

    private void OnDisable()
    {
        if (playerHealth)
        {
            playerHealth.OnPlayerHealthChanged -= UpdateLightIntensity;
        }
    }

    private void UpdateLightIntensity(float currentHealth, float maxHealth)
    {
        float healthPercent = currentHealth / maxHealth;

        laternDirectionalLight.intensity = healthPercent * baseIntensity;
    }

    [ContextMenu("Switch to second color")]
    public void SwitchToSecondColor()
    {
        if (_activeTransition != null) StopCoroutine(_activeTransition);
        _activeTransition = StartCoroutine(SmoothColorTransition(secondColor));
    }

    private IEnumerator SmoothColorTransition(Color targetColor)
    {
        Color startDirColor = laternDirectionalLight.color;

        float elapsedTime = 0f;

        while (elapsedTime < switchDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / switchDuration;

            laternDirectionalLight.color = Color.Lerp(startDirColor, targetColor, t);

            yield return null;
        }

        laternDirectionalLight.color = targetColor;
        _activeTransition = null;
    }
}