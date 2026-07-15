using System;
using System.Collections;
using UnityEngine;

public class LongJumpController : MonoBehaviour
{
    [Header("References")]
    public RunningController runningController;

    [Header("Foul Line")]
    public float foulLineZ = 18f;

    [Header("Takeoff Window")]
    public float windowSecondsBeforeFoul = 2f;

    [Header("Distance Calculation")]
    public float worstJumpDistance = 2f;   // near-zero speed (lots of wrong taps) — a weak, scrappy jump
    public float minJumpDistance = 5.23f;  // achieved at baseSpeed (single tap / minimal effort)
    public float maxJumpDistance = 9.93f;  // achieved at maxSpeed (fast, perfect continuous tapping)

    [Header("Jump Animation")]
    public float jumpDuration = 0.9f;  // how long the visual arc takes, regardless of distance
    public float jumpHeight = 1.5f;    // peak height of the arc, purely visual

    private bool inTakeoffWindow = false;
    private bool hasJumped = false;
    private bool fouled = false;

    public event Action OnTakeoffWindowOpen;
    public event Action<bool> OnJumpResult; // true = valid jump, false = foul
    public event Action<float> OnLandingResult; // final MEASURED distance (from foul line)

    void Update()
    {
        float z = transform.position.z;

        if (!hasJumped)
        {
            float distanceRemaining = foulLineZ - z;

            if (!inTakeoffWindow && runningController.currentSpeed > 0.1f)
            {
                float timeRemaining = distanceRemaining / runningController.currentSpeed;

                if (timeRemaining <= windowSecondsBeforeFoul)
                {
                    inTakeoffWindow = true;
                    runningController.FreezeRunningInput();
                    OnTakeoffWindowOpen?.Invoke();
                }
            }

            if (z >= foulLineZ)
            {
                fouled = true;
                hasJumped = true;
                OnJumpResult?.Invoke(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            OnTakeoffTap();
        }
    }

    public void OnTakeoffTap()
    {
        if (!inTakeoffWindow || hasJumped) return;

        hasJumped = true;
        OnJumpResult?.Invoke(true);

        float takeoffZ = transform.position.z; // NEW — remember exactly where takeoff happened
        float jumpPower = CalculateJumpDistance(); // raw jump power, not the final measured score

        StartCoroutine(JumpArc(takeoffZ, jumpPower)); // CHANGED — pass takeoffZ along
    }

    float CalculateJumpDistance()
    {
        float speed = runningController.currentSpeed;
        float baseSpeed = runningController.BaseSpeed;
        float maxSpeed = runningController.MaxSpeed;

        if (speed >= baseSpeed)
        {
            // Between steady pace and best pace → maps to minJumpDistance...maxJumpDistance
            float t = Mathf.InverseLerp(baseSpeed, maxSpeed, speed);
            return Mathf.Lerp(minJumpDistance, maxJumpDistance, t);
        }
        else
        {
            // Below steady pace (wrong taps dragged speed down) → maps to worstJumpDistance...minJumpDistance
            float t = Mathf.InverseLerp(0f, baseSpeed, speed);
            return Mathf.Lerp(worstJumpDistance, minJumpDistance, t);
        }
    }

    IEnumerator JumpArc(float takeoffZ, float jumpPower) // CHANGED — now takes takeoffZ too
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0, 0, jumpPower);

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            float z = Mathf.Lerp(startPos.z, endPos.z, t);
            float heightArc = jumpHeight * 4f * t * (1f - t); // simple parabola, peaks at t=0.5

            transform.position = new Vector3(startPos.x, startPos.y + heightArc, z);
            yield return null;
        }

        transform.position = endPos; // snap exactly to final landing spot

        // NEW — actual measured/reported distance is landing position minus the foul line,
        // NOT the raw jumpPower. Taking off earlier than the line costs you distance.
        float measuredDistance = Mathf.Max(0f, endPos.z - foulLineZ); // clamped so it can't show negative

        OnLandingResult?.Invoke(measuredDistance);
    }

    void OnEnable()
    {
        OnJumpResult += LogJumpResult;
        OnLandingResult += LogLandingResult;
    }

    void OnDisable()
    {
        OnJumpResult -= LogJumpResult;
        OnLandingResult -= LogLandingResult;
    }

    void LogJumpResult(bool valid)
    {
        if (!valid)
        {
            Debug.Log("FOUL! Crossed the line without jumping.");
        }
    }

    void LogLandingResult(float distance)
    {
        Debug.Log($"Jump distance: {distance:0.00}m");
    }
}