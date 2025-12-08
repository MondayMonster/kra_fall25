using UnityEngine;

public class UICanvasManager : MonoBehaviour
{
    [Header("References")]
    public LanguageSelector languageSelector;      // your existing script
    public GameObject languageSelectionPanel;      // panel with radio buttons
    public GameObject mainUIPanel;                 // panel to show after selection

    private bool hasSwitched = false;

    private void Start()
    {
        // Ensure initial state
        if (languageSelectionPanel != null)
            languageSelectionPanel.SetActive(true);

        if (mainUIPanel != null)
            mainUIPanel.SetActive(false);
    }

    // Call this from the toggles' OnValueChanged events
    public void OnLanguageSelected()
    {
        // Avoid doing it multiple times
        if (hasSwitched)
            return;

        hasSwitched = true;

        if (languageSelectionPanel != null)
            languageSelectionPanel.SetActive(false);

        if (mainUIPanel != null)
            mainUIPanel.SetActive(true);

        if (languageSelector != null)
        {
            Debug.Log("[UICanvasManager] Language chosen: " + languageSelector.CurrentLanguage);
        }
    }
}
