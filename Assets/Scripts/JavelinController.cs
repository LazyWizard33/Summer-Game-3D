using System;
using System.Collections;
using UnityEngine;

public class JavelinController : MonoBehaviour
{
    [Header("References")]
    public RunningController runningController;
    public GameObject javelinPrefab;
    public Transform throwPoint;

    [Header("Held Javelin (visible while running)")]
    public GameObject heldJavelin;

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
    public float slideDistance = 2f;
    public float slideDuration = 0.3f;

    [Header("Javelin Flight")]
    public float flightDuration = 1.4f;
    public float flightHeight = 4f;

    [Header("Run Phase")]
    public float minimumRunTime = 5f;

    private bool inApproachWindow = false;
    private bool isHolding = false;
    private bool hasThrown = false;
    private float holdStartTime;

    public event Action OnApproachWindowOpen;
    public event Action OnHoldStarted;
    public event Action<float> OnHoldReleased;
    public event Action OnFoul;



    void Update()
    {
        if (hasThrown)
            return;

        float z = transform.position.z;
        float speed = runningController.currentSpeed;

        if (!inApproachWindow &&
            speed > 0.1f &&
            runningController.RaceTimer >= minimumRunTime)
        {
            inApproachWindow = true;

            runningController.FreezeRunningInput();

            OnApproachWindowOpen?.Invoke();
        }

        // RESTORED — crossing the foul line before releasing (or even while holding) is an automatic foul
        if (!hasThrown && z >= foulLineZ)
        {
            hasThrown = true;
            isHolding = false; // cancel any in-progress hold, since it's now invalid
            OnFoul?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Keypad5))
            OnHoldButtonDown();

        if (Input.GetKeyUp(KeyCode.Keypad5))
            OnHoldButtonUp();
    }

    public void OnHoldButtonDown()
    {
        if (!inApproachWindow || hasThrown || isHolding)
            return;

        isHolding = true;
        holdStartTime = Time.time;
        OnHoldStarted?.Invoke();
    }

    public void OnHoldButtonUp()
    {
        if (!isHolding || hasThrown)
            return;

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

        float speedQuality = Mathf.InverseLerp(
            runningController.BaseSpeed,
            runningController.MaxSpeed,
            runningController.currentSpeed);

        speedQuality = Mathf.Clamp01(speedQuality);

        if (runningController.currentSpeed < runningController.BaseSpeed)
        {
            float lowSpeedT = Mathf.InverseLerp(
                0f,
                runningController.BaseSpeed,
                runningController.currentSpeed);

            return Mathf.Lerp(worstThrowDistance, minThrowDistance, lowSpeedT)
                   * (0.5f + releaseQuality * 0.5f);
        }

        float combinedQuality = (releaseQuality + speedQuality) * 0.5f;
        return Mathf.Lerp(minThrowDistance, maxThrowDistance, combinedQuality);
    }

    IEnumerator SlideThenThrow(float distance)
    {
        Vector3 startPos = transform.position;
        Vector3 slideEnd = startPos + new Vector3(0f, 0f, slideDistance);

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / slideDuration);
            transform.position = Vector3.Lerp(startPos, slideEnd, t);

            yield return null;
        }

        transform.position = slideEnd;

        yield return StartCoroutine(ThrowJavelin(distance));
    }

    IEnumerator ThrowJavelin(float distance)
    {
        Vector3 spawnPos = throwPoint != null
            ? throwPoint.position
            : transform.position + Vector3.up * 1.5f;

        if (heldJavelin != null)
            heldJavelin.SetActive(false);

        GameObject javelin = Instantiate(
            javelinPrefab,
            spawnPos,
            Quaternion.Euler(90f, 0f, 0f));

        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();

        if (cam != null)
        {
            Transform camTarget = javelin.transform.Find("CameraTarget");

            if (camTarget != null)
                cam.SetTarget(camTarget);
            else
                cam.SetTarget(javelin.transform);
        }

        Vector3 startPos = spawnPos;
        Vector3 endPos = new Vector3(
            startPos.x,
            0.1f,
            startPos.z + distance);

        float elapsed = 0f;
        Vector3 previousPos = startPos; // NEW — tracks last frame's position to compute direction of travel

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / flightDuration);

            float z = Mathf.Lerp(startPos.z, endPos.z, t);
            float height = Mathf.Sin(t * Mathf.PI) * flightHeight;

            Vector3 newPos = new Vector3(
                startPos.x,
                Mathf.Lerp(startPos.y, endPos.y, t) + height,
                z);

            javelin.transform.position = newPos;

            // CHANGED — rotate to face the actual direction of movement, instead of hardcoded pitch values
            Vector3 velocity = newPos - previousPos;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                javelin.transform.rotation = lookRot * Quaternion.Euler(90f, 0f, 0f); // combines aim direction with the mesh's own 90° "lying flat" correction
            }

            previousPos = newPos; // NEW

            yield return null;
        }

        javelin.transform.position = endPos;

        float measuredDistance = Mathf.Max(0f, endPos.z - foulLineZ);

        OnHoldReleased?.Invoke(measuredDistance);
    }
}