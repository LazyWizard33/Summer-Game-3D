using UnityEngine;

public class TakeoffButtonDisplay : MonoBehaviour
{
    public LongJumpController longJumpController;
    public GameObject takeoffButton;

    void OnEnable()
    {
        longJumpController.OnTakeoffWindowOpen += ShowButton;
        longJumpController.OnJumpResult += HideButton;
    }

    void OnDisable()
    {
        longJumpController.OnTakeoffWindowOpen -= ShowButton;
        longJumpController.OnJumpResult -= HideButton;
    }

    void ShowButton() => takeoffButton.SetActive(true);
    void HideButton(bool valid) => takeoffButton.SetActive(false);
}