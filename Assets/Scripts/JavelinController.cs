using System;
using System.Collections;
using UnityEngine;

public class JavelinController : MonoBehaviour
{
    [Header("References")]
    public RunningController runningController;
    public GameObject javelinPrefab; // NEW — drag your Javelin prefab here
    public Transform throwPoint;     // NEW — empty child on the player, roughly hand height, marks spawn position
    [Header("Held Javelin (visible while running)")]
    public GameObject heldJavelin; // NEW — drag the HeldJavelin child object here
    [Header("Foul Line")]
    public float foulLineZ = 21f;

    [Header("Approach Timing")]
    public float approachWindowSeconds = 1.5f;

    [Header("Hold/Release Timing")]
    public float perfectHoldDuration = 1.5f;
    public float releaseTolerance = 1f;

    [Header("Distance Calculation")]
    public float worstThrowDistance = 10f;
    public float minThrowDistance = 30f;
    public float maxThrowDistance = 90f;

    [Header("Slide Animation")]
    public float slideDistance = 2f; // small run-up slide right up to the foul line, NOT the throw distance
    public float slideDuration = 0.3f;

    [Header("Javelin Flight")]
    public float flightDuration = 1.4f; // how long the javelin is in the air, regardless of distance
    public float flightHeight = 4f;     // peak arc height, purely visual

    

    private bool inApproachWindow = false;
    private bool isHolding = false;
    private bool hasThrown = false;
    private float holdStartTime;

    public event Action OnApproachWindowOpen;
    public event Action OnHoldStarted;
    public event Action<float> OnHoldReleased; // final measured distance
    public event Action OnFoul;

    void Update()
    {
        if (hasThrown) return;

        float z = transform.position.z;
        float speed = runningController.currentSpeed;

        if (!inApproachWindow && speed > 0.1f)
        {
            float timeRemaining = (foulLineZ - z) / speed;
            if (timeRemaining <= approachWindowSeconds)
            {
                inApproachWindow = true;
                runningController.FreezeRunningInput();
                OnApproachWindowOpen?.Invoke();
            }
        }

        if (inApproachWindow && !isHolding && z >= foulLineZ)
        {
            hasThrown = true;
            OnFoul?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Keypad5)) OnHoldButtonDown();
        if (Input.GetKeyUp(KeyCode.Keypad5)) OnHoldButtonUp();
    }

    public void OnHoldButtonDown()
    {
        if (!inApproachWindow || hasThrown || isHolding) return;

        isHolding = true;
        holdStartTime = Time.time;
        OnHoldStarted?.Invoke();
    }

    public void OnHoldButtonUp()
    {
        if (!isHolding || hasThrown) return;

        isHolding = false;
        hasThrown = true;

        float holdDuration = Time.time - holdStartTime;
        float distance = CalculateThrowDistance(holdDuration);

        StartCoroutine(SlideThenThrow(distance));
    }

    float CalculateThrowDistance(float holdDuration)
    {
        float diff = Mathf.Abs(holdDuration - perfectHoldDuration);
        float releaseQuality = 1f - Mathf.Clamp01(diff / releaseTolerance);

        float speedQuality = Mathf.InverseLerp(runningController.BaseSpeed, runningController.MaxSpeed, runningController.currentSpeed);
        speedQuality = Mathf.Clamp01(speedQuality);

        if (runningController.currentSpeed < runningController.BaseSpeed)
        {
            float lowSpeedT = Mathf.InverseLerp(0f, runningController.BaseSpeed, runningController.currentSpeed);
            return Mathf.Lerp(worstThrowDistance, minThrowDistance, lowSpeedT) * (0.5f + releaseQuality * 0.5f);
        }

        float combinedQuality = (releaseQuality + speedQuality) / 2f;
        return Mathf.Lerp(minThrowDistance, maxThrowDistance, combinedQuality);
    }

    IEnumerator SlideThenThrow(float distance)
    {
        // Small run-up slide, just positions the player at the foul line — NOT the throw distance
        Vector3 startPos = transform.position;
        Vector3 slideEnd = startPos + new Vector3(0, 0, slideDistance);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            transform.position = Vector3.Lerp(startPos, slideEnd, t);
            yield return null;
        }

        transform.position = slideEnd;

        // NEW — actually spawn and throw the javelin now
        yield return StartCoroutine(ThrowJavelin(distance));
    }

    IEnumerator ThrowJavelin(float distance)
    {
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position + Vector3.up * 1.5f;

        if (heldJavelin != null)
            heldJavelin.SetActive(false); // NEW — hide the held one the instant it's thrown

        GameObject javelin = Instantiate(javelinPrefab, spawnPos, Quaternion.Euler(90, 0, 0));

        Vector3 startPos = spawnPos;
        Vector3 endPos = new Vector3(startPos.x, 0.1f, startPos.z + distance);

        float elapsed = 0f;
        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            float x = Mathf.Lerp(startPos.z, endPos.z, t);
            float height = flightHeight * 4f * t * (1f - t);

            javelin.transform.position = new Vector3(startPos.x, startPos.y + height - (startPos.y * t), x);

            float pitch = Mathf.Lerp(20f, -60f, t);
            javelin.transform.rotation = Quaternion.Euler(pitch, 0, 0);

            yield return null;
        }

        javelin.transform.position = endPos;

        float measuredDistance = Mathf.Max(0f, endPos.z - foulLineZ);
        OnHoldReleased?.Invoke(measuredDistance);
    }
}