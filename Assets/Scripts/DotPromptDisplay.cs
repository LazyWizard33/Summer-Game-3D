using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DotPromptDisplay : MonoBehaviour
{
    public RunningController runningController;

    [Header("Dots")]
    public RectTransform dotLeft;   // CHANGED — needs RectTransform now, not just GameObject, to animate position
    public RectTransform dotRight;

    [Header("Wrong Tap Cross Marks")]
    public GameObject wrongMarkLeft;
    public GameObject wrongMarkRight;
    public float wrongMarkDuration = 0.3f;

    [Header("Spawn Animation")] // NEW SECTION
    public float spawnDropOffset = 300f;   // how far below resting position the dot starts
    public float spawnAnimDuration = 1f; // how fast it lerps up into place

    private Vector2 dotLeftRestPos;
    private Vector2 dotRightRestPos;
    private Coroutine dotLeftAnimCoroutine;
    private Coroutine dotRightAnimCoroutine;

    void Awake()
    {
        // Cache each dot's proper resting position once, at startup
        dotLeftRestPos = dotLeft.anchoredPosition;
        dotRightRestPos = dotRight.anchoredPosition;
    }

    void OnEnable()
    {
        runningController.OnNewPrompt += HandleNewPrompt;
        runningController.OnTapResult += HandleTapResult;
        runningController.OnRaceFinished += HandleRaceFinished;
        runningController.OnRunningFrozen += HandleRunningFrozen; // NEW
    }

    void OnDisable()
    {
        runningController.OnNewPrompt -= HandleNewPrompt;
        runningController.OnTapResult -= HandleTapResult;
        runningController.OnRaceFinished -= HandleRaceFinished;
        runningController.OnRunningFrozen -= HandleRunningFrozen; // NEW
    }

    // NEW — same cleanup as race finished, just triggered by freezing instead
    void HandleRunningFrozen()
    {
        StopAllCoroutines();
        dotLeft.gameObject.SetActive(false);
        dotRight.gameObject.SetActive(false);
        wrongMarkLeft.SetActive(false);
        wrongMarkRight.SetActive(false);
    }
    void HandleNewPrompt(TapSide side)
    {
        dotLeft.gameObject.SetActive(side == TapSide.Left);
        dotRight.gameObject.SetActive(side == TapSide.Right);

        // NEW — trigger the pop-up animation for whichever dot just appeared
        if (side == TapSide.Left)
        {
            if (dotLeftAnimCoroutine != null) StopCoroutine(dotLeftAnimCoroutine);
            dotLeftAnimCoroutine = StartCoroutine(AnimateDotIn(dotLeft, dotLeftRestPos));
        }
        else
        {
            if (dotRightAnimCoroutine != null) StopCoroutine(dotRightAnimCoroutine);
            dotRightAnimCoroutine = StartCoroutine(AnimateDotIn(dotRight, dotRightRestPos));
        }
    }

    IEnumerator AnimateDotIn(RectTransform dot, Vector2 restPos)
    {
        Vector2 startPos = restPos + new Vector2(0, -spawnDropOffset); // start below resting position
        float elapsed = 0f;

        dot.anchoredPosition = startPos;

        while (elapsed < spawnAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spawnAnimDuration);
            t = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic — fast start, gentle settle

            dot.anchoredPosition = Vector2.Lerp(startPos, restPos, t);
            yield return null;
        }

        dot.anchoredPosition = restPos; // snap exactly to rest position at the end
    }

    void HandleTapResult(bool correct, TapSide tappedSide)
    {
        if (!correct)
        {
            StartCoroutine(ShowWrongMark(tappedSide));
        }
    }

    void HandleRaceFinished(float finalTime)
    {
        StopAllCoroutines();
        dotLeft.gameObject.SetActive(false);
        dotRight.gameObject.SetActive(false);
        wrongMarkLeft.SetActive(false);
        wrongMarkRight.SetActive(false);
    }

    IEnumerator ShowWrongMark(TapSide side)
    {
        GameObject mark = (side == TapSide.Left) ? wrongMarkLeft : wrongMarkRight;
        mark.SetActive(true);
        yield return new WaitForSeconds(wrongMarkDuration);
        mark.SetActive(false);
    }
}