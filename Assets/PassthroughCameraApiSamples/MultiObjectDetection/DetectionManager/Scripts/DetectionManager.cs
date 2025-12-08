// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionManager : MonoBehaviour
    {
        [SerializeField] private WebCamTextureManager m_webCamTextureManager;

        [Header("Controls configuration")]
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;
            
        [Header("MX Ink stylus")]
        [SerializeField] private MXInkStylusHandler m_stylusHandler;

        [Header("Ui references")]
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;

        [Header("Placement configureation")]
        [SerializeField] private GameObject m_spwanMarker;
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private float m_spawnDistance = 0.25f;
        [SerializeField] private AudioSource m_placeSound;
        
        [Header("Marker Interaction")]
        [SerializeField] private GameObject m_markerUIPrefab; // UI to spawn at marker location
        [SerializeField] private GameObject m_writerUIPrefab; // Writer UI to spawn after closing marker UI
        [SerializeField] private WordDrawingManager m_wordDrawingManager; // Reference to drawing manager
        [SerializeField] private Vector3 m_uiSpawnOffset = new Vector3(0, 0.2f, 0);

        [Header("Sentis inference ref")]
        [SerializeField] private SentisInferenceRunManager m_runInference;
        [SerializeField] private SentisInferenceUiManager m_uiInference;
    [Header("Environment classification")]
    [SerializeField] private OVRInput.RawButton m_classifyButton = OVRInput.RawButton.B;
    [SerializeField] private EnvironmentClassifierConfig m_environmentClassifier;
        [Space(10)]
        public UnityEvent<int> OnObjectsIdentified;

        private bool m_isPaused = true;
        private List<GameObject> m_spwanedEntities = new();
        private GameObject m_currentSpawnedUI; // Track the currently spawned UI (Marker or Writer)
        private string m_currentObjectName; // Track the current object name
        private string m_currentSpanishWord; // Track the current Spanish translation
        private bool m_isStarted = false;
        private bool m_isSentisReady = false;
        private float m_delayPauseBackTime = 0;
    private string m_currentEnvironment = string.Empty;

        #region Unity Functions
        private void Awake()
        {
            OVRManager.display.RecenteredPose += CleanMarkersCallBack;
            InitializeEnvironmentLabel();
        }

        private IEnumerator Start()
        {
            // Wait until Sentis model is loaded
            var sentisInference = FindAnyObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_isSentisReady = true;
        }

        private void Update()
        {
            // Get the WebCamTexture CPU image
            var hasWebCamTextureData = m_webCamTextureManager.WebCamTexture != null;

            if (!m_isStarted)
            {
                // Manage the Initial Ui Menu
                if (hasWebCamTextureData && m_isSentisReady)
                {
                    //m_uiMenuManager.OnInitialMenu(m_environmentRaycast.HasScenePermission());
                    m_isStarted = true;
                }
            }
            else
            {
                // Press A button to spawn 3d markers
            bool aButtonReleased = OVRInput.GetUp(m_actionButton);

            bool stylusFrontReleased = 
                m_stylusHandler != null && m_stylusHandler.FrontButtonReleasedThisFrame;

            if ((aButtonReleased || stylusFrontReleased) && m_delayPauseBackTime <= 0)
            {                
                // Debug.Log($"A Button Released: {aButtonReleased}, Stylus Front Released: {stylusFrontReleased}, Delay Time: {m_delayPauseBackTime}");

                // Priority 1: Try to click UI button with front button
                if (m_stylusHandler != null && stylusFrontReleased && m_stylusHandler.TriggerUIButtonClick())
                {
                    Debug.Log("[DetectionManager] UI button clicked via front button");
                }
                // Priority 2: Spawn markers
                else
                {
                    SpwanCurrentDetectedObjects();
                }
            }
            
            // Check if back button pressed while hovering a marker or UI
            if (m_stylusHandler != null && m_stylusHandler.BackButtonPressedThisFrame)
            {
                // Debug.Log($"[DetectionManager] Back button pressed. Hovered marker: {(m_stylusHandler.CurrentHoveredMarker != null ? m_stylusHandler.CurrentHoveredMarker.GetYoloClassName() : "none")}, Hovered UI: {(m_stylusHandler.CurrentHoveredUI != null ? m_stylusHandler.CurrentHoveredUI.name : "none")}");
                
                // Priority 1: Try to click UI button
                if (m_stylusHandler.TriggerUIButtonClick())
                {
                    Debug.Log("[DetectionManager] UI button clicked via back button");
                }
                // Priority 2: Interact with marker
                else if (m_stylusHandler.CurrentHoveredMarker != null)
                {
                    OnMarkerInteraction(m_stylusHandler.CurrentHoveredMarker);
                }
                // else
                // {
                //     Debug.Log("[DetectionManager] Back button pressed but no marker or UI hovered");
                // }
            }
            
            // Check for back button double-click to close UI and show markers
            if (m_stylusHandler != null && m_stylusHandler.BackButtonDoubleClickedThisFrame)
            {
                if (m_currentSpawnedUI != null)
                {
                    // Check what type of UI is currently showing
                    var markerUI = m_currentSpawnedUI.GetComponent<MarkerUIController>();
                    var writerUI = m_currentSpawnedUI.GetComponent<WriterUIController>();
                    
                    if (markerUI != null)
                    {
                        // Close Marker UI and open Writer UI
                        Debug.Log("[DetectionManager] Back button double-clicked - closing Marker UI and opening Writer UI");
                        Vector3 uiPosition = m_currentSpawnedUI.transform.position;
                        Quaternion uiRotation = m_currentSpawnedUI.transform.rotation;
                        
                        // Get Spanish translation before destroying
                        m_currentSpanishWord = markerUI.SpanishTranslation ?? "unknown";
                        
                        // Stop any TTS speech before transitioning to Writer UI
                        var ttsManager = FindFirstObjectByType<TTSManager>();
                        if (ttsManager != null)
                        {
                            ttsManager.StopSpeaking();
                            Debug.Log("[DetectionManager] Stopped TTS speech before opening Writer UI");
                        }
                        
                        Destroy(m_currentSpawnedUI);
                        m_currentSpawnedUI = null;
                        
                        // Spawn Writer UI at the same location
                        SpawnWriterUI(uiPosition, uiRotation);
                        m_stylusHandler.TriggerHapticClick();
                    }
                    else if (writerUI != null)
                    {
                        // Close Writer UI and show all markers
                        Debug.Log("[DetectionManager] Back button double-clicked - closing Writer UI and showing markers");
                        
                        // Disable drawing
                        if (m_wordDrawingManager != null)
                        {
                            m_wordDrawingManager.SetDrawingEnabled(false);
                        }
                        
                        Destroy(m_currentSpawnedUI);
                        m_currentSpawnedUI = null;
                        ShowAllMarkers();
                        
                        // Re-enable stylus ray
                        if (m_stylusHandler != null)
                        {
                            m_stylusHandler.SetUIActive(false);
                        }
                        
                        m_stylusHandler.TriggerHapticClick();
                    }
                }
            }
                if (OVRInput.GetUp(m_classifyButton) || Input.GetKeyUp(KeyCode.B))
                {
                    ClassifyCurrentEnvironment();
                }
                // Cooldown for the A button after return from the pause menu
                m_delayPauseBackTime -= Time.deltaTime;
                if (m_delayPauseBackTime <= 0)
                {
                    m_delayPauseBackTime = 0;
                }
            }

            // Not start a sentis inference if the app is paused or we don't have a valid WebCamTexture
            if (m_isPaused || !hasWebCamTextureData)
            {
                if (m_isPaused)
                {
                    // Set the delay time for the A button to return from the pause menu
                    m_delayPauseBackTime = 0.1f;
                }
                return;
            }

            // Run a new inference when the current inference finishes
            if (!m_runInference.IsRunning())
            {
                m_runInference.RunInference(m_webCamTextureManager.WebCamTexture);
            }
        }
        #endregion

        #region Environment Classification Functions
        private void InitializeEnvironmentLabel()
        {
            SetEnvironmentLabel(GetUnknownEnvironmentLabel());
        }

        private void ClassifyCurrentEnvironment()
        {
            if (m_uiInference == null)
            {
                return;
            }

            var labels = new List<string>();
            foreach (var box in m_uiInference.BoxDrawn)
            {
                if (string.IsNullOrWhiteSpace(box.ClassName))
                {
                    continue;
                }

                labels.Add(box.ClassName.Trim());
            }

            if (labels.Count == 0)
            {
                SetEnvironmentLabel(GetUnknownEnvironmentLabel());
                return;
            }

            if (m_environmentClassifier != null && m_environmentClassifier.TryClassify(labels, out var environment, out _))
            {
                SetEnvironmentLabel(environment);
            }
            else
            {
                SetEnvironmentLabel(GetUnknownEnvironmentLabel());
            }
        }

        private string GetUnknownEnvironmentLabel()
        {
            if (m_environmentClassifier != null)
            {
                return m_environmentClassifier.GetUnknownLabel();
            }

            return "Unknown";
        }

        private void SetEnvironmentLabel(string environmentLabel)
        {
            var label = environmentLabel ?? string.Empty;
            if (m_currentEnvironment == label)
            {
                return;
            }

            m_currentEnvironment = label;
            if (m_uiMenuManager != null)
            {
                m_uiMenuManager.SetEnvironment(label);
            }
        }
        #endregion

        #region Marker Functions
        /// <summary>
        /// Clean 3d markers when the tracking space is re-centered.
        /// </summary>
        private void CleanMarkersCallBack()
        {
            foreach (var e in m_spwanedEntities)
            {
                Destroy(e, 0.1f);
            }
            m_spwanedEntities.Clear();
            SetEnvironmentLabel(GetUnknownEnvironmentLabel());
            OnObjectsIdentified?.Invoke(-1);
        }
        /// <summary>
        /// Spwan 3d markers for the detected objects
        /// </summary>
        public void SpwanCurrentDetectedObjects()
        {
            var count = 0;
            foreach (var box in m_uiInference.BoxDrawn)
            {
                if (PlaceMarkerUsingEnvironmentRaycast(box.WorldPos, box.ClassName))
                {
                    count++;
                }
            }
            if (count > 0)
            {
                // Play sound if a new marker is placed.
                m_placeSound.Play();
            }
            if (count == 3){
                m_isPaused = true;
            }

            OnObjectsIdentified?.Invoke(count);
        }

        /// <summary>
        /// Place a marker using the environment raycast
        /// </summary>
        private bool PlaceMarkerUsingEnvironmentRaycast(Vector3? position, string className)
        {
            // Check if the position is valid
            if (!position.HasValue)
            {
                return false;
            }

            // Check if you spanwed the same object before
            var existMarker = false;
            foreach (var e in m_spwanedEntities)
            {
                var markerClass = e.GetComponent<DetectionSpawnMarkerAnim>();
                if (markerClass)
                {
                    var dist = Vector3.Distance(e.transform.position, position.Value);
                    if (dist < m_spawnDistance && markerClass.GetYoloClassName() == className)
                    {
                        existMarker = true;
                        break;
                    }
                }
            }

            if (!existMarker)
            {
                // spawn a visual marker
                var eMarker = Instantiate(m_spwanMarker);
                m_spwanedEntities.Add(eMarker);

                // Update marker transform with the real world transform
                eMarker.transform.SetPositionAndRotation(position.Value, Quaternion.identity);
                eMarker.GetComponent<DetectionSpawnMarkerAnim>().SetYoloClassName(className);
            }

            return !existMarker;
        }
        #endregion

        #region Marker Interaction Functions
        /// <summary>
        /// Called when a marker is interacted with via the stylus back button
        /// </summary>
        private void OnMarkerInteraction(DetectionSpawnMarkerAnim marker)
        {
            string objectName = marker.GetYoloClassName();
            Debug.Log($"[DetectionManager] === Interacting with marker: {objectName} ===");
            
            // Get marker position
            Vector3 markerPosition = marker.transform.position;
            
            // Hide all markers
            HideAllMarkers();
            
            // Spawn UI at marker location
            if (m_markerUIPrefab != null)
            {
                Vector3 spawnPos = markerPosition + m_uiSpawnOffset;
                GameObject ui = Instantiate(m_markerUIPrefab, spawnPos, Quaternion.identity);
                
                // Track the spawned UI
                m_currentSpawnedUI = ui;
                
                // Optional: Make UI face the camera
                var cam = FindFirstObjectByType<OVRCameraRig>();
                if (cam != null)
                {
                    ui.transform.LookAt(cam.centerEyeAnchor);
                    ui.transform.Rotate(0, 180, 0); // Face the camera
                }
                
                // Initialize UI with object name and environment
                var uiController = ui.GetComponent<MarkerUIController>();
                if (uiController != null)
                {
                    Debug.Log($"[DetectionManager] Requesting GPT data: object='{objectName}', environment='{m_currentEnvironment}'");
                    
                    // Store current object info for Writer UI
                    m_currentObjectName = objectName;
                    
                    uiController.Initialize(objectName, m_currentEnvironment);
                    
                    // Disable stylus ray when UI is active
                    if (m_stylusHandler != null)
                    {
                        m_stylusHandler.SetUIActive(true);
                    }
                }
                else
                {
                    Debug.LogWarning("[DetectionManager] MarkerUIController component not found on UI prefab!");
                }
            }
            else
            {
                Debug.LogWarning("[DetectionManager] ✗ Marker UI Prefab is not assigned!");
            }
            
            // Haptic feedback
            if (m_stylusHandler != null)
            {
                m_stylusHandler.TriggerHapticClick();
            }
        }
        
        /// <summary>
        /// Hide all spawned markers
        /// </summary>
        private void HideAllMarkers()
        {
            int hiddenCount = 0;
            foreach (var entity in m_spwanedEntities)
            {
                if (entity != null)
                {
                    entity.SetActive(false);
                    hiddenCount++;
                }
            }
            // Debug.Log($"[DetectionManager] Hidden {hiddenCount} markers");
        }
        
        /// <summary>
        /// Show all spawned markers (optional, for toggling)
        /// </summary>
        public void ShowAllMarkers()
        {
            foreach (var entity in m_spwanedEntities)
            {
                if (entity != null)
                {
                    entity.SetActive(true);
                }
            }
        }
        #endregion

        #region Writer UI Functions
        /// <summary>
        /// Spawn the Writer UI at specified position
        /// </summary>
        private void SpawnWriterUI(Vector3 position, Quaternion rotation)
        {
            if (m_writerUIPrefab == null)
            {
                Debug.LogWarning("[DetectionManager] Writer UI Prefab is not assigned!");
                return;
            }

            GameObject writerUI = Instantiate(m_writerUIPrefab, position, rotation);
            m_currentSpawnedUI = writerUI;

            var writerController = writerUI.GetComponent<WriterUIController>();
            if (writerController != null)
            {
                writerController.Initialize(m_currentObjectName, m_currentSpanishWord, m_wordDrawingManager);
                Debug.Log($"[DetectionManager] Writer UI spawned for: {m_currentObjectName} ({m_currentSpanishWord})");
                
                // Ray stays disabled, but writing with middle button is now enabled
                // The WordDrawingManager will check IsMiddleButtonDown instead of IsTipDown
            }
            else
            {
                Debug.LogWarning("[DetectionManager] WriterUIController component not found on Writer UI prefab!");
            }
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Pause the detection logic when the pause menu is active
        /// </summary>
        public void OnPause(bool pause)
        {
            m_isPaused = pause;
        }
        #endregion
    }
}
