using FPS.Scripts.Gameplay.Managers;
using System.Collections.Generic;
using TMPro;
using Unity.FPS.Gameplay;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Script.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        private PlayerInputHandler m_InputHandler;
        private GameObject player;

        [Header("Menu")]
        [SerializeField] private List<GameObject> menus;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject hudMenu;
        [SerializeField] private bool isPaused;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI currentTxt;
        [SerializeField] private TextMeshProUGUI maxTxt;

        [SerializeField] private TextMeshProUGUI currentAmmo;
        [SerializeField] private TextMeshProUGUI stockAmmo;



        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                m_InputHandler = player.GetComponent<PlayerInputHandler>();
            }
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

        public void ShowMenu(GameObject menu)
        {
            foreach (GameObject m in menus)
            {
                if (m.activeSelf)
                {
                    m.SetActive(false);
                }
            }

            menu.SetActive(true);
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            //player.GetComponent<PlayerCharacterController>().enabled = true;
            EventSystem.current.SetSelectedGameObject(null);

            foreach (GameObject m in menus)
            {
                if (m.activeSelf)
                {
                    m.SetActive(false);
                }
            }

            hudMenu.SetActive(true);
        }

        private void Update()
        {
            if(m_InputHandler.GetPauseButtonDown() && !isPaused)
            {
                Time.timeScale = 0f;
                pauseMenu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                //player.GetComponent<PlayerCharacterController>().enabled = false;
            }
        }
    }
}
