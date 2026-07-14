using System.Collections;
using UnityEngine;
using TMPro;

public class RaceStartSequence : MonoBehaviour
{
    public RunningController runningController;
    public TMP_Text startText; // shows "Get Ready...", "Set...", "GO!"
    public float minDelay = 1f;
    public float maxDelay = 3f; // random delay, like a real starting gun

    void Start()
    {
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        startText.text = "GET READY";
        yield return new WaitForSeconds(1f);

        startText.text = "SET";
        float delay = Random.Range(minDelay, maxDelay); // unpredictable, like real races
        yield return new WaitForSeconds(delay);

        startText.text = "GO!";
        runningController.StartRace();

        yield return new WaitForSeconds(0.6f);
        startText.text = ""; // clear it after a moment
    }
}