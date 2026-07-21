using UnityEngine;

public class JavelinHoldDisplay : MonoBehaviour
{
    public JavelinController javelinController;
    public GameObject holdButton;


    void OnEnable()
    {
        javelinController.OnApproachWindowOpen += ShowButton;
        javelinController.OnHoldReleased += HideButton;
        javelinController.OnFoul += HideButtonNoArg;
    }

    void OnDisable()
    {
        javelinController.OnApproachWindowOpen -= ShowButton;
        javelinController.OnHoldReleased -= HideButton;
        javelinController.OnFoul -= HideButtonNoArg;
    }

    void ShowButton() => holdButton.SetActive(true);
    void HideButton(float distance) => holdButton.SetActive(false);
    void HideButtonNoArg() => holdButton.SetActive(false);
}