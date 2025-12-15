using UnityEngine;
using PassthroughCameraSamples.MultiObjectDetection;

/// <summary>
/// Manages the 3-UI workflow: Marker UI → Speaker UI → Writer UI
/// Handles transitions and cleanup
/// </summary>
public class ObjectOverlayManager : MonoBehaviour
{
    public static ObjectOverlayManager Instance { get; private set; }

    [Header("UI Prefabs")]
    [SerializeField] private GameObject markerUIPrefab;
    [SerializeField] private GameObject speakerUIPrefab;
    [SerializeField] private GameObject writerUIPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 uiSpawnOffset = new Vector3(0, 0.2f, 0);

    [Header("System References")]
    [SerializeField] private MXInkStylusHandler stylusHandler;
    [SerializeField] private DetectionManager detectionManager;
    [SerializeField] private WordDrawingManager drawingManager;

    [Header("Debug")]
    [SerializeField] private bool logWorkflow = true;

    // Current workflow state
    private GameObject currentUI;
    private string currentObjectName;
    private string currentSpanishWord;
    private string currentEnvironment;
    private Vector3 currentUIPosition;
    private Quaternion currentUIRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-find references
        if (stylusHandler == null)
            stylusHandler = FindFirstObjectByType<MXInkStylusHandler>();
        if (detectionManager == null)
            detectionManager = FindFirstObjectByType<DetectionManager>();
        if (drawingManager == null)
            drawingManager = FindFirstObjectByType<WordDrawingManager>();
    }

    #region Workflow Start

    /// <summary>
    /// Start the overlay workflow when a marker is clicked
    /// </summary>
    public void StartWorkflow(string objectName, string environment, Vector3 markerPosition)
    {
        if (logWorkflow)
        {
            Debug.Log($"[ObjectOverlay] Starting workflow for: {objectName}");
        }

        // Store context
        currentObjectName = objectName;
        currentEnvironment = environment;
        currentUIPosition = markerPosition + uiSpawnOffset;

        // Calculate rotation to face camera
        var cam = FindFirstObjectByType<OVRCameraRig>();
        if (cam != null)
        {
            Vector3 directionToCamera = cam.centerEyeAnchor.position - currentUIPosition;
            directionToCamera.y = 0; // Keep UI upright
            currentUIRotation = Quaternion.LookRotation(directionToCamera);
            currentUIRotation *= Quaternion.Euler(0, 180, 0); // Rotate 180° to face user
        }
        else
        {
            currentUIRotation = Quaternion.identity;
        }

        // Transition to overlay state
        GameStateManager.Instance.StartObjectOverlay();

        // Hide all markers
        if (detectionManager != null)
        {
            detectionManager.HideAllMarkers();
        }

        // Show Marker UI
        ShowMarkerUI();
    }

    #endregion

    #region UI Transitions

    /// <summary>
    /// Show Marker UI (first step)
    /// </summary>
    private void ShowMarkerUI()
    {
        if (logWorkflow)
        {
            Debug.Log("[ObjectOverlay] Step 1: Showing Marker UI");
        }

        // Destroy previous UI if any
        if (currentUI != null)
        {
            Destroy(currentUI);
        }

        // Spawn Marker UI
        currentUI = Instantiate(markerUIPrefab, currentUIPosition, currentUIRotation);
        currentUI.SetActive(true); // Ensure UI is enabled

        var markerController = currentUI.GetComponent<MarkerUIController>();
        if (markerController != null)
        {
            markerController.Initialize(currentObjectName, currentEnvironment);
            markerController.OnNextClicked += ShowSpeakerUI; // Subscribe to next button
        }
        else
        {
            Debug.LogError("[ObjectOverlay] MarkerUIController not found on prefab!");
        }
    }

    /// <summary>
    /// Show Speaker UI (second step)
    /// </summary>
    public void ShowSpeakerUI()
    {
        if (logWorkflow)
        {
            Debug.Log("[ObjectOverlay] Step 2: Showing Speaker UI");
        }

        // Get Spanish word from Marker UI before destroying
        var markerController = currentUI?.GetComponent<MarkerUIController>();
        if (markerController != null)
        {
            currentSpanishWord = markerController.SpanishTranslation ?? currentObjectName;
            markerController.OnNextClicked -= ShowSpeakerUI; // Unsubscribe
        }

        // Destroy Marker UI
        if (currentUI != null)
        {
            Destroy(currentUI);
        }

        // Spawn Speaker UI
        currentUI = Instantiate(speakerUIPrefab, currentUIPosition, currentUIRotation);
        currentUI.SetActive(true); // Ensure UI is enabled

        var speakerController = currentUI.GetComponent<PracticeUIController>();
        if (speakerController != null)
        {
            speakerController.Initialize(currentObjectName, currentSpanishWord);
            speakerController.OnNextClicked += ShowWriterUI; // Subscribe to next button
        }
        else
        {
            Debug.LogError("[ObjectOverlay] PracticeUIController not found on prefab!");
        }
    }

    /// <summary>
    /// Show Writer UI (third step)
    /// </summary>
    public void ShowWriterUI()
    {
        if (logWorkflow)
        {
            Debug.Log("[ObjectOverlay] Step 3: Showing Writer UI");
        }

        // Unsubscribe from Speaker UI
        var speakerController = currentUI?.GetComponent<PracticeUIController>();
        if (speakerController != null)
        {
            speakerController.OnNextClicked -= ShowWriterUI;
        }

        // Destroy Speaker UI
        if (currentUI != null)
        {
            Destroy(currentUI);
        }

        // Spawn Writer UI
        currentUI = Instantiate(writerUIPrefab, currentUIPosition, currentUIRotation);
        currentUI.SetActive(true); // Ensure UI is enabled

        var writerController = currentUI.GetComponent<WriterUIController>();
        if (writerController != null)
        {
            writerController.Initialize(currentObjectName, currentSpanishWord, drawingManager);
            writerController.OnCloseClicked += EndWorkflow; // Subscribe to close button
        }
        else
        {
            Debug.LogError("[ObjectOverlay] WriterUIController not found on prefab!");
        }
    }

    #endregion

    #region Workflow End

    /// <summary>
    /// End the overlay workflow and return to gameplay
    /// </summary>
    public void EndWorkflow()
    {
        if (logWorkflow)
        {
            Debug.Log("[ObjectOverlay] Ending workflow, returning to gameplay");
        }

        // Destroy current UI
        if (currentUI != null)
        {
            Destroy(currentUI);
            currentUI = null;
        }

        // Disable drawing
        if (drawingManager != null)
        {
            drawingManager.SetDrawingEnabled(false);
        }

        // Show all markers
        if (detectionManager != null)
        {
            detectionManager.ShowAllMarkers();
        }

        // Return to gameplay state
        GameStateManager.Instance.EndObjectOverlay();

        // Clear context
        currentObjectName = null;
        currentSpanishWord = null;
        currentEnvironment = null;
    }

    #endregion
}
