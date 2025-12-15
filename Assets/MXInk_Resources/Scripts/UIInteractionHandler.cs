using UnityEngine;

/// <summary>
/// Handles UI button interactions with MX Ink stylus
/// Always active - independent of DetectionManager
/// </summary>
public class UIInteractionHandler : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private MXInkStylusHandler stylusHandler;

    [Header("Debug")]
    [SerializeField] private bool logInteractions = true;

    private void Start()
    {
        // Auto-find stylus handler if not assigned
        if (stylusHandler == null)
        {
            stylusHandler = FindFirstObjectByType<MXInkStylusHandler>();
            if (stylusHandler == null)
            {
                Debug.LogError("[UIInteractionHandler] MXInkStylusHandler not found in scene!");
            }
            else
            {
                Debug.Log("[UIInteractionHandler] ✓ MXInkStylusHandler auto-found");
            }
        }
    }

    private void Update()
    {
        if (stylusHandler == null)
            return;

        // Check for front button press to click UI buttons
        if (stylusHandler.FrontButtonReleasedThisFrame)
        {
            if (logInteractions)
            {
                Debug.Log("[UIInteractionHandler] Front button released - checking for UI button");
            }

            // Try to click any hovered UI button
            if (stylusHandler.TriggerUIButtonClick())
            {
                if (logInteractions)
                {
                    Debug.Log("[UIInteractionHandler] ✓ UI button clicked successfully");
                }
            }
            else
            {
                if (logInteractions)
                {
                    Debug.Log("[UIInteractionHandler] No UI button to click");
                }
            }
        }
    }
}
