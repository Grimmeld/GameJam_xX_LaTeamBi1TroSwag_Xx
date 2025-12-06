using FPS.Scripts.Gameplay.Managers;
using System.Collections.Generic;
using TMPro;
using Unity.FPS.Gameplay;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        //[SerializeField] private TextMeshProUGUI currentAmmo;
        [SerializeField] private TextMeshProUGUI stockAmmo;
        [SerializeField] private Image spriteAmmo; // Image on the UI
        [SerializeField] private Sprite spriteNull;
        [SerializeField] private Sprite sprite1;
        [SerializeField] private Sprite sprite2;



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
            if (spriteAmmo != null)
            {
                if (current == 0)
                {
                    spriteAmmo.sprite = spriteNull;
                }
                else if (current == 1)
                {
                    spriteAmmo.sprite = sprite1;
                }
                else if (current == 2)
                {
                    spriteAmmo.sprite = sprite2;
                }
            }

            if (stockAmmo)
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

        public void ExitGame()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
Application.Quit();
#endif
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
