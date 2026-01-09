using UnityEngine;

/// <summary>
/// Minimal helper to "respawn" the same Cup by teleporting it back to a spawn point.
/// Use it from UnityEvents (e.g., dispenser button, delivery success).
/// </summary>
[DisallowMultipleComponent]
public class CupRespawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Cup root GameObject (must contain the Rigidbody + StateMachine).")]
    public GameObject cup;

    [Tooltip("Where the cup should reappear.")]
    public Transform spawnLocation;

    [Header("State Reset")]
    [Tooltip("If set, this will be called to reset the drink state (e.g., 'Empty').")]
    public StateMachine cupStateMachine;

    [Tooltip("State to apply on respawn.")]
    public string respawnState = "Empty";

    [Header("Physics")]
    [Tooltip("If true, clears linear/angular velocity when respawning.")]
    public bool clearVelocity = true;

    [Tooltip("If true, temporarily makes the Rigidbody kinematic during teleport to avoid physics glitches.")]
    public bool temporarilyKinematic = true;

    private Rigidbody cupRb;

    private void Awake()
    {
        if (cup != null)
        {
            cupRb = cup.GetComponent<Rigidbody>();
            if (cupStateMachine == null)
                cupStateMachine = cup.GetComponent<StateMachine>();
        }
    }

    /// <summary>
    /// Teleports the cup to spawnLocation and resets its state.
    /// Call this from a UnityEvent.
    /// </summary>
    public void RespawnCup()
    {
        if (!enabled) return;
        if (cup == null || spawnLocation == null) return;

        cup.SetActive(true);

        if (cupRb == null)
            cupRb = cup.GetComponent<Rigidbody>();

        bool hadRb = cupRb != null;
        bool wasKinematic = hadRb && cupRb.isKinematic;

        if (hadRb && temporarilyKinematic)
            cupRb.isKinematic = true;

        // Teleport
        cup.transform.SetPositionAndRotation(spawnLocation.position, spawnLocation.rotation);

        if (hadRb)
        {
            if (clearVelocity)
            {
                cupRb.linearVelocity = Vector3.zero;
                cupRb.angularVelocity = Vector3.zero;
            }

            if (temporarilyKinematic)
                cupRb.isKinematic = wasKinematic;
        }

        if (cupStateMachine == null)
            cupStateMachine = cup.GetComponent<StateMachine>();

        if (cupStateMachine != null && !string.IsNullOrWhiteSpace(respawnState))
            cupStateMachine.SetState(respawnState);
    }

    /// <summary>
    /// Convenience: hides the cup without destroying it.
    /// </summary>
    public void HideCup()
    {
        if (cup != null)
            cup.SetActive(false);
    }
}

