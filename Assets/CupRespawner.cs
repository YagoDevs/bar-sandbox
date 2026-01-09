using UnityEngine;

/// <summary>
/// Minimal helper to "respawn" a selected container (Cup/Mug/Bottle) by teleporting it back to a spawn point.
/// Use it from UnityEvents (e.g., dispenser buttons, delivery success).
/// </summary>
[DisallowMultipleComponent]
public class CupRespawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Back-compat: single cup reference. If 'cups' is filled, this can be left empty.")]
    public GameObject cup;

    [Tooltip("Optional: list of container variants (e.g., Cup/Mug/Bottle). " +
             "If provided, only the selected one will be active/used.")]
    public GameObject[] cups;

    [Tooltip("Index into 'cups' used by RespawnCup/HideCup/SetSelectedState.")]
    public int selectedIndex = 0;

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

    private GameObject CurrentCup
    {
        get
        {
            if (cups != null && cups.Length > 0)
            {
                if (selectedIndex < 0) selectedIndex = 0;
                if (selectedIndex >= cups.Length) selectedIndex = cups.Length - 1;
                return cups[selectedIndex];
            }
            return cup;
        }
    }

    private void Awake()
    {
        RefreshCachedReferences();
    }

    private void RefreshCachedReferences()
    {
        var active = CurrentCup;
        if (active == null)
        {
            cupRb = null;
            return;
        }

        cupRb = active.GetComponent<Rigidbody>();

        // If user didn't explicitly assign a StateMachine reference, try to find it on the active container.
        if (cupStateMachine == null)
            cupStateMachine = active.GetComponent<StateMachine>();
    }

    /// <summary>
    /// Selects which container variant is active (0-based). Safe to call from UnityEvents.
    /// </summary>
    public void SelectCupIndex(int index)
    {
        selectedIndex = index;

        // Ensure only selected container is active (if using variants list)
        if (cups != null && cups.Length > 0)
        {
            for (int i = 0; i < cups.Length; i++)
            {
                if (cups[i] != null)
                    cups[i].SetActive(i == selectedIndex);
            }
        }

        // Rebind cached references to the newly selected container
        cupStateMachine = null;
        RefreshCachedReferences();
    }

    // Convenience methods (nice for UnityEvent dropdowns)
    public void SelectCup0() => SelectCupIndex(0);
    public void SelectCup1() => SelectCupIndex(1);
    public void SelectCup2() => SelectCupIndex(2);

    /// <summary>
    /// Teleports the cup to spawnLocation and resets its state.
    /// Call this from a UnityEvent.
    /// </summary>
    public void RespawnCup()
    {
        if (!enabled) return;
        var active = CurrentCup;
        if (active == null || spawnLocation == null) return;

        // If using variants list, keep only the selected one active.
        if (cups != null && cups.Length > 0)
        {
            for (int i = 0; i < cups.Length; i++)
            {
                if (cups[i] != null)
                    cups[i].SetActive(i == selectedIndex);
            }
        }

        active.SetActive(true);

        if (cupRb == null)
            cupRb = active.GetComponent<Rigidbody>();

        bool hadRb = cupRb != null;
        bool wasKinematic = hadRb && cupRb.isKinematic;

        if (hadRb && temporarilyKinematic)
            cupRb.isKinematic = true;

        // Teleport
        active.transform.SetPositionAndRotation(spawnLocation.position, spawnLocation.rotation);

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
            cupStateMachine = active.GetComponent<StateMachine>();

        if (cupStateMachine != null && !string.IsNullOrWhiteSpace(respawnState))
            cupStateMachine.SetState(respawnState);
    }

    /// <summary>
    /// Convenience: hides the cup without destroying it.
    /// </summary>
    public void HideCup()
    {
        var active = CurrentCup;
        if (active != null)
            active.SetActive(false);
    }

    /// <summary>
    /// Convenience: sets the selected container state (useful to rewire machine timers to target the selected container).
    /// </summary>
    public void SetSelectedState(string state)
    {
        var active = CurrentCup;
        if (active == null) return;

        if (cupStateMachine == null)
            cupStateMachine = active.GetComponent<StateMachine>();

        if (cupStateMachine != null && !string.IsNullOrWhiteSpace(state))
            cupStateMachine.SetState(state);
    }
}

