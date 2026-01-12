using UnityEngine;

/// <summary>
/// Simple "if" router for button presses without relying on ConditionalTrigger auto-trigger behavior.
/// Attach to the same GameObject that has the socket trigger collider (IsTrigger = true).
/// Then hook your button's MouseListener to call Press().
///
/// NOTE: This component supports having multiple instances on the same socket GameObject
/// (e.g., one router per button/flavor).
/// </summary>
public class SocketButtonRouter : MonoBehaviour
{
    [System.Serializable]
    public class Route
    {
        [Tooltip("Optional: require this exact StateMachine state before allowing the correct action.")]
        public string requiredState;

        [Tooltip("Timer to start for the correct action (e.g., Timer_Flavor).")]
        public TimerTrigger correctTimer;

        [Header("Failure path (no/invalid container)")]
        public GameObject spillFx;
        public GameObject wetDecal;
        public AudioSource spillAudio;
        public TimerTrigger wetTimer;
    }

    [Header("Detection")]
    [Tooltip("Only colliders with this tag are considered valid containers.")]
    public string containerTag = "Cup";

    [Tooltip("If assigned, only the currently selected container from CupRespawner is considered valid in the socket.")]
    public CupRespawner cupRespawner;

    [Tooltip("If true, requires the selected container to be physically inside the socket.")]
    public bool requireSelectedContainerInSocket = true;

    [Header("Correct path (has container)")]
    [Tooltip("Optional: require this exact StateMachine state before allowing the correct action.")]
    public string requiredState = "Empty";

    [Tooltip("Timer to start for the correct action (e.g., Timer_Water).")]
    public TimerTrigger correctTimer;

    [Header("Failure path (no container)")]
    public GameObject spillFx;
    public GameObject wetDecal;
    public AudioSource spillAudio;
    public TimerTrigger wetTimer;

    [Header("Optional extra routes (e.g., soda flavors)")]
    public Route lemonRoute;
    public Route orangeRoute;
    public Route passionRoute;

    private GameObject lastContainerInSocket;

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || other == null) return;
        if (!other.CompareTag(containerTag)) return;

        // Track the root container object (collider may be on child)
        var containerRoot = other.GetComponentInParent<StateMachine>()?.gameObject ?? other.gameObject;
        lastContainerInSocket = containerRoot;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled || other == null) return;
        if (!other.CompareTag(containerTag)) return;

        var containerRoot = other.GetComponentInParent<StateMachine>()?.gameObject ?? other.gameObject;
        if (lastContainerInSocket == containerRoot)
            lastContainerInSocket = null;
    }

    private bool HasValidContainerInSocket(string requiredStateOverride)
    {
        if (lastContainerInSocket == null) return false;

        if (requireSelectedContainerInSocket && cupRespawner != null)
        {
            var selected = cupRespawner.GetSelectedCup();
            if (selected == null || selected != lastContainerInSocket)
                return false;
        }

        var stateToRequire = requiredStateOverride;
        if (!string.IsNullOrWhiteSpace(stateToRequire))
        {
            var sm = lastContainerInSocket.GetComponent<StateMachine>();
            if (sm != null && !sm.CurrentState.Equals(stateToRequire, System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Call this from your button (MouseListener.onMouseDownEvent).
    /// If a valid container is in the socket (and optionally in required state), starts correctTimer.
    /// Otherwise triggers spill feedback.
    /// </summary>
    public void Press()
    {
        if (!enabled) return;

        if (HasValidContainerInSocket(requiredState))
        {
            if (correctTimer != null)
            {
                correctTimer.Stop();
                correctTimer.Play();
            }
            return;
        }

        // Failure path: spill
        if (wetDecal != null) wetDecal.SetActive(true);
        if (spillFx != null) spillFx.SetActive(true);
        if (spillAudio != null)
        {
            spillAudio.Stop();
            spillAudio.Play();
        }
        if (wetTimer != null)
        {
            wetTimer.Stop();
            wetTimer.Play();
        }
    }

    private void PressRoute(Route route)
    {
        if (!enabled) return;
        if (route == null) return;

        if (HasValidContainerInSocket(route.requiredState))
        {
            if (route.correctTimer != null)
            {
                route.correctTimer.Stop();
                route.correctTimer.Play();
            }
            return;
        }

        if (route.wetDecal != null) route.wetDecal.SetActive(true);
        if (route.spillFx != null) route.spillFx.SetActive(true);
        if (route.spillAudio != null)
        {
            route.spillAudio.Stop();
            route.spillAudio.Play();
        }
        if (route.wetTimer != null)
        {
            route.wetTimer.Stop();
            route.wetTimer.Play();
        }
    }

    // Convenience methods for UnityEvents (so you don't need multiple components)
    public void PressLemon() => PressRoute(lemonRoute);
    public void PressOrange() => PressRoute(orangeRoute);
    public void PressPassion() => PressRoute(passionRoute);
}

