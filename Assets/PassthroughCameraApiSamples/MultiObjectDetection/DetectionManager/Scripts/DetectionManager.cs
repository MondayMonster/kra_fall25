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
                Debug.Log($"A Button Released: {aButtonReleased}, Stylus Front Released: {stylusFrontReleased}, Delay Time: {m_delayPauseBackTime}");

                SpwanCurrentDetectedObjects();
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
