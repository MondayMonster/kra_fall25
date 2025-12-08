using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Writer UI that appears after closing the Marker UI
/// Shows a fixed text box and a pronunciation text box
/// </summary>
public class WriterUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI fixedText;
    [SerializeField] private TextMeshProUGUI pronunciationText;
    [SerializeField] private Button closeButton;

    private string currentObjectName;
    private WordDrawingManager drawingManager;

    private void Start()
    {
        // Hook up close button if exists
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    /// <summary>
    /// Initialize the Writer UI with object data and enable drawing
    /// </summary>
    public void Initialize(string objectName, string spanishWord, WordDrawingManager drawingMgr)
    {
        currentObjectName = objectName;
        drawingManager = drawingMgr;

        if (fixedText != null)
        {
            fixedText.text = $"Practice writing:\n{spanishWord}";
        }

        if (pronunciationText != null)
        {
            // Show pronunciation guide (you can customize this)
            pronunciationText.text = $"Pronunciation: {spanishWord}";
        }
        
        // Enable drawing with middle button
        if (drawingManager != null)
        {
            drawingManager.SetDrawingEnabled(true);
        }

        Debug.Log($"[WriterUIController] Initialized for: {objectName} ({spanishWord}), drawing enabled");
    }

    private void OnCloseButtonClicked()
    {
        Debug.Log("[WriterUIController] Close button clicked");
        CloseAndShowMarkers();
    }

    /// <summary>
    /// Close the Writer UI and show all markers
    /// </summary>
    public void CloseAndShowMarkers()
    {
        // Disable drawing
        if (drawingManager != null)
        {
            drawingManager.SetDrawingEnabled(false);
        }
        
        // Show markers again
        var detectionManager = FindFirstObjectByType<PassthroughCameraSamples.MultiObjectDetection.DetectionManager>();
        if (detectionManager != null)
        {
            detectionManager.ShowAllMarkers();
        }

        // Destroy this UI
        Destroy(gameObject);
    }
}
