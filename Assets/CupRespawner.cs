using UnityEngine;

/// <summary>
/// Minimal helper to "respawn" a selected container (Cup/Mug/Bottle) by teleporting it back to a spawn point.
/// Use it from UnityEvents (e.g., dispenser buttons, delivery success).
/// </summary>
[DisallowMultipleComponent]
public class CupRespawner : MonoBehaviour
{
    public enum SodaFlavor
    {
        Lemon,
        Orange,
        Passion
    }

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

    [Header("Soda Flavor Selection")]
    [Tooltip("Used by ApplySelectedFlavorUnmixed() to decide which '_Unmixed' state to set.")]
    public SodaFlavor selectedSodaFlavor = SodaFlavor.Lemon;

    [Header("Mix (optional helper)")]
    [Tooltip("Optional: assign your Timer_Mix here so the Mix button can call MixIfReady() without ConditionalTriggers.")]
    public TimerTrigger mixTimer;

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

    /// <summary>
    /// Returns the currently selected container GameObject (Cup/Mug/Bottle), or null.
    /// Useful for UI/HUD scripts.
    /// </summary>
    public GameObject GetSelectedCup() => CurrentCup;

    /// <summary>
    /// Returns the current state string of the selected container's StateMachine, or "None".
    /// </summary>
    public string GetSelectedCupState()
    {
        var active = CurrentCup;
        if (active == null) return "None";
        var sm = active.GetComponent<StateMachine>();
        return sm != null ? sm.CurrentState : "None";
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

    // --- Flavor selection helpers (for a single shared Flavor timer) ---
    public void SelectSodaFlavorLemon() => selectedSodaFlavor = SodaFlavor.Lemon;
    public void SelectSodaFlavorOrange() => selectedSodaFlavor = SodaFlavor.Orange;
    public void SelectSodaFlavorPassion() => selectedSodaFlavor = SodaFlavor.Passion;

    /// <summary>
    /// Sets the selected container to the corresponding Soda_*_Unmixed state based on selectedSodaFlavor.
    /// Intended to be called at the end of a single shared Flavor timer.
    /// </summary>
    public void ApplySelectedSodaFlavorUnmixed()
    {
        switch (selectedSodaFlavor)
        {
            case SodaFlavor.Orange:
                SetSelectedState("Soda_Orange_Unmixed");
                break;
            case SodaFlavor.Passion:
                SetSelectedState("Soda_Passion_Unmixed");
                break;
            default:
                SetSelectedState("Soda_Lemon_Unmixed");
                break;
        }
    }

    /// <summary>
    /// Sets the selected container to the corresponding final Soda_* state based on selectedSodaFlavor.
    /// Intended to be called at the end of a single shared Mix timer.
    /// </summary>
    public void ApplySelectedSodaFinal()
    {
        switch (selectedSodaFlavor)
        {
            case SodaFlavor.Orange:
                SetSelectedState("Soda_Orange");
                break;
            case SodaFlavor.Passion:
                SetSelectedState("Soda_Passion");
                break;
            default:
                SetSelectedState("Soda_Lemon");
                break;
        }
    }

    /// <summary>
    /// Optional convenience for a single Mix button:
    /// - Checks the selected container's current state
    /// - If it's Soda_*_Unmixed, derives the flavor and starts mixTimer (Stop+Play)
    /// This removes the need for ConditionalTrigger gating/selection logic for Mix.
    /// </summary>
    public void MixIfReady()
    {
        if (!enabled) return;
        if (mixTimer == null) return;

        var active = CurrentCup;
        if (active == null) return;

        var sm = active.GetComponent<StateMachine>();
        if (sm == null) return;

        string state = sm.CurrentState;
        if (string.IsNullOrEmpty(state)) return;

        // Derive flavor from the unmixed state name
        if (state.Equals("Soda_Lemon_Unmixed", System.StringComparison.OrdinalIgnoreCase))
            selectedSodaFlavor = SodaFlavor.Lemon;
        else if (state.Equals("Soda_Orange_Unmixed", System.StringComparison.OrdinalIgnoreCase))
            selectedSodaFlavor = SodaFlavor.Orange;
        else if (state.Equals("Soda_Passion_Unmixed", System.StringComparison.OrdinalIgnoreCase))
            selectedSodaFlavor = SodaFlavor.Passion;
        else
            return; // Not ready to mix

        mixTimer.Stop();
        mixTimer.Play();
    }

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

        // Always resolve the StateMachine from the currently selected container.
        // This avoids stale references when switching between Cup/Mug/Bottle.
        var sm = active.GetComponent<StateMachine>();
        if (sm != null && !string.IsNullOrWhiteSpace(state))
            sm.SetState(state);
    }
}

