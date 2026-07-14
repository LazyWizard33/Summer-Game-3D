using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject playPanel;
    public GameObject optionsPanel;
    public GameObject resetConfirmPanel;
    public GameObject quitConfirmPanel;

    private GameObject currentPanel;
    private readonly Stack<GameObject> panelHistory = new Stack<GameObject>();

    void Start()
    {
        currentPanel = mainMenuPanel;
        ShowOnly(mainMenuPanel);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }

    public void ShowPlayMenu() => Navigate(playPanel);
    public void ShowOptions() => Navigate(optionsPanel);
    public void ShowResetConfirm() => Navigate(resetConfirmPanel);

    void Navigate(GameObject target)
    {
        panelHistory.Push(currentPanel);
        ShowOnly(target);
    }

    public void HandleBackButton()
    {
        if (panelHistory.Count > 0)
        {
            GameObject previous = panelHistory.Pop();
            ShowOnly(previous);
        }
        else
        {
            ShowOnly(quitConfirmPanel);
        }
    }

    void ShowOnly(GameObject target)
    {
        mainMenuPanel.SetActive(false);
        playPanel.SetActive(false);
        optionsPanel.SetActive(false);
        resetConfirmPanel.SetActive(false);
        quitConfirmPanel.SetActive(false); // was missing before

        target.SetActive(true);
        currentPanel = target;
    }
    public void ShowQuitConfirm()
    {
        ShowOnly(quitConfirmPanel); // note: not Navigate(), matches how HandleBackButton triggers it
    }
    // --- Quit confirm ---
    public void ConfirmQuit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }

    public void CancelQuit()
    {
        ShowOnly(mainMenuPanel); // fixed: was HandleBackButton(), caused a loop
    }

    // --- Reset confirm ---
    public void ConfirmReset()
    {
        Debug.Log("Reset");
        PlayerPrefs.DeleteAll(); // swap for your actual reset/save-wipe logic
        HandleBackButton(); // safe here: resetConfirmPanel WAS reached via Navigate(), so history has an entry
    }

    public void CancelReset()
    {
        HandleBackButton(); // same reasoning — pops back to Options correctly
    }
}