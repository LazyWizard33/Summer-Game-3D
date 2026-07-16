using UnityEngine;
using UnityEngine.UI;

public class HurdleChoiceDisplay : MonoBehaviour
{
    public HurdleController hurdleController;

    public GameObject leftIcon;
    public Image leftIconImage;

    public GameObject rightIcon;
    public Image rightIconImage;

    public Sprite jumpSprite; // e.g. a checkmark or "JUMP" icon
    public Sprite fakeSprite; // e.g. an X or "FAKE" icon

    void OnEnable()
    {
        hurdleController.OnHurdleChoiceShown += ShowChoice;
        hurdleController.OnHurdleCleared += HideChoice;
        hurdleController.OnHurdleFailed += HideChoice;
    }

    void OnDisable()
    {
        hurdleController.OnHurdleChoiceShown -= ShowChoice;
        hurdleController.OnHurdleCleared -= HideChoice;
        hurdleController.OnHurdleFailed -= HideChoice;
    }

    void ShowChoice(TapSide jumpSide)
    {
        leftIcon.SetActive(true);
        rightIcon.SetActive(true);

        leftIconImage.sprite = jumpSide == TapSide.Left ? jumpSprite : fakeSprite;
        rightIconImage.sprite = jumpSide == TapSide.Right ? jumpSprite : fakeSprite;
    }

    void HideChoice(int index)
    {
        leftIcon.SetActive(false);
        rightIcon.SetActive(false);
    }
}