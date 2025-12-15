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
        [SerializeField] private GameObject m_practiceUIPrefab; // Practice UI for speech recognition
        [SerializeField] private GameObject m_writerUIPrefab; // Writer UI to spawn after closing practice UI
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
            
            // Start the object overlay workflow via ObjectOverlayManager
            var overlayManager = FindFirstObjectByType<ObjectOverlayManager>();
            if (overlayManager != null)
            {
                overlayManager.StartWorkflow(objectName, m_currentEnvironment, markerPosition);
            }
            else
            {
                Debug.LogError("[DetectionManager] ObjectOverlayManager not found! Cannot start workflow.");
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
        public void HideAllMarkers()
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
