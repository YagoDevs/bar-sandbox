using TMPro;
using UnityEngine;

/// <summary>
/// Simple on-screen HUD that displays the current StateMachine state of the selected container.
/// Attach this to a Canvas/TextMeshProUGUI object.
/// </summary>
[DisallowMultipleComponent]
public class StateHUD : MonoBehaviour
{
    [Header("References")]
    public CupRespawner cupRespawner;
    public TMP_Text text;

    [Header("Display")]
    public string prefix = "State: ";
    public bool showSelectedName = true;

    private void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (!enabled || text == null || cupRespawner == null)
            return;

        string state = cupRespawner.GetSelectedCupState();
        if (showSelectedName)
        {
            var cup = cupRespawner.GetSelectedCup();
            string name = cup != null ? cup.name : "None";
            text.SetText($"{prefix}{state}\nContainer: {name}");
        }
        else
        {
            text.SetText($"{prefix}{state}");
        }
    }
}

