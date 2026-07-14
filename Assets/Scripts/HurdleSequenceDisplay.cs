using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HurdleSequenceDisplay : MonoBehaviour
{
    public HurdleController hurdleController;
    public Image[] sequenceSlots; // now just 3 slots showing Green/Blue pattern

    public Color greenColor = Color.green;
    public Color blueColor = Color.blue;
    public Color emptyColor = Color.gray;
    public Color wrongFlashColor = Color.black;

    void OnEnable()
    {
        hurdleController.OnSequenceGenerated += ShowSequence;
        hurdleController.OnInputProgress += HandleProgress;
    }

    void OnDisable()
    {
        hurdleController.OnSequenceGenerated -= ShowSequence;
        hurdleController.OnInputProgress -= HandleProgress;
    }

    void ShowSequence(List<TapSide> sequence)
    {
        for (int i = 0; i < sequenceSlots.Length; i++)
        {
            sequenceSlots[i].color = sequence[i] == TapSide.Left ? greenColor : blueColor;
        }
    }

    void HandleProgress(int index, bool correct)
    {
        sequenceSlots[index].color = correct ? emptyColor : wrongFlashColor;
    }
}