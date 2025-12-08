using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PassthroughCameraSamples.MultiObjectDetection;

public class MXInkStylusHandler : MonoBehaviour
{
    [SerializeField] private GameObject mxInkModel;
    [SerializeField] private GameObject tip;
    [SerializeField] private GameObject clusterFront;
    [SerializeField] private GameObject clusterMiddle;
    [SerializeField] private GameObject clusterBack;

    [SerializeField] private GameObject leftController;
    [SerializeField] private GameObject rightController;
    [SerializeField] private DetectionManager detectionManager;

    [Header("UI Pointer Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject reticle;
    [SerializeField] private float maxPointerDistance = 10f;
    [SerializeField] private LayerMask pointerLayerMask = ~0; // Raycast against all layers (UI + Prefabs)
    [SerializeField] private Color pointerColorDefault = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color pointerColorHover = new Color(0f, 1f, 1f, 0.8f);
    [SerializeField] private float reticleHoverScale = 1.5f;

    public Color activeColor = Color.green;
    public Color activeColorFront = Color.red;
    public Color doubleTapActiveColor = Color.cyan;
    public Color defaultColor = Color.white;

    private StylusInputs stylus;

    // UI Pointer state
    private bool isHoveringUI;
    private Vector3 reticleDefaultScale;
    
    // Expose currently hit object for interaction
    public GameObject CurrentHitObject { get; private set; }
    public DetectionSpawnMarkerAnim CurrentHoveredMarker { get; private set; }
    public GameObject CurrentHoveredUI { get; private set; }
    private Button currentHoveredButton;

    // Front button edge detection
    private bool prevClusterFrontValue;
    public bool FrontButtonPressedThisFrame  { get; private set; }
    public bool FrontButtonReleasedThisFrame { get; private set; }

    // Middle button edge detection (from analog force)
    public bool MiddleButtonPressedThisFrame  { get; private set; }
    public bool MiddleButtonReleasedThisFrame { get; private set; }
    private bool prevMiddleDown;
    private const float MiddlePressThreshold   = 0.02f; // pressed above this
    private const float MiddleReleaseThreshold = 0.01f; // released below this

    // Back button edge detection
    private bool prevClusterBackValue;
    public bool BackButtonPressedThisFrame   { get; private set; }
    public bool BackButtonReleasedThisFrame  { get; private set; }

    // Expose stylus pose & tip state for other systems (WordDrawingManager)
    public Vector3 StylusPosition  => stylus.inkingPose.position;
    public Quaternion StylusRotation => stylus.inkingPose.rotation;
    public float TipValue => stylus.tipValue;
    public bool IsTipDown => stylus.tipValue > 0.001f;

    // Ray origin/rotation for interactions (use tip if available)
    public Vector3 RayOrigin =>
        tip != null ? tip.transform.position : stylus.inkingPose.position;

    public Quaternion RayRotation =>
        tip != null ? tip.transform.rotation : stylus.inkingPose.rotation;

    // Defined action names.
    private const string MX_Ink_Pose_Right = "aim_right";
    private const string MX_Ink_Pose_Left = "aim_left";
    private const string MX_Ink_TipForce = "tip";
    private const string MX_Ink_MiddleForce = "middle";
    private const string MX_Ink_ClusterFront = "front";
    private const string MX_Ink_ClusterBack = "back";
    private const string MX_Ink_ClusterBack_DoubleTap = "back_double_tap";
    private const string MX_Ink_ClusterFront_DoubleTap = "front_double_tap";
    private const string MX_Ink_Docked = "docked";
    private const string MX_Ink_Haptic_Pulse = "haptic_pulse";
    private float hapticClickDuration = 0.011f;
    private float hapticClickAmplitude = 1.0f;

    private void Start()
    {
        // Initialize pointer components
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.002f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = pointerColorDefault;
            lineRenderer.endColor = pointerColorDefault;
        }

        if (reticle != null)
        {
            reticleDefaultScale = reticle.transform.localScale;
            reticle.SetActive(false);
        }
    }

    private void UpdatePose()
    {
        var leftDevice = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandLeft);
        var rightDevice = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandRight);

        bool stylusIsOnLeftHand = leftDevice.Contains("logitech");
        bool stylusIsOnRightHand = rightDevice.Contains("logitech");

        stylus.isActive = stylusIsOnLeftHand || stylusIsOnRightHand;
        stylus.isOnRightHand = stylusIsOnRightHand;
        
        string MX_Ink_Pose = stylus.isOnRightHand ? MX_Ink_Pose_Right : MX_Ink_Pose_Left;

        mxInkModel.SetActive(stylus.isActive);
        rightController.SetActive(!stylus.isOnRightHand || !stylus.isActive);
        leftController.SetActive(stylus.isOnRightHand || !stylus.isActive);

        if (OVRPlugin.GetActionStatePose(MX_Ink_Pose, out OVRPlugin.Posef handPose))
        {
            transform.localPosition = handPose.Position.FromFlippedZVector3f();
            transform.localRotation = handPose.Orientation.FromFlippedZQuatf();
            stylus.inkingPose.position = transform.localPosition;
            stylus.inkingPose.rotation = transform.localRotation;
        }
    }

    void Update()
    {
        OVRInput.Update();
        UpdatePose();

        // --- Read MX Ink actions ---

        if (!OVRPlugin.GetActionStateFloat(MX_Ink_TipForce, out stylus.tipValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_TipForce}");
        }

        if (!OVRPlugin.GetActionStateFloat(MX_Ink_MiddleForce, out stylus.clusterMiddleValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_MiddleForce}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterFront, out stylus.clusterFrontValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterFront}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterBack, out stylus.clusterBackValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterBack}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterFront_DoubleTap, out stylus.clusterBackDoubleTapValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterFront_DoubleTap}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterBack_DoubleTap, out stylus.clusterBackDoubleTapValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterBack_DoubleTap}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_Docked, out stylus.docked))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_Docked}");
        }

        // --- Button edge detection (front + back + middle) ---

        // Front
        FrontButtonPressedThisFrame  =  stylus.clusterFrontValue && !prevClusterFrontValue;
        FrontButtonReleasedThisFrame = !stylus.clusterFrontValue &&  prevClusterFrontValue;
        prevClusterFrontValue        =  stylus.clusterFrontValue;

        // Back
        BackButtonPressedThisFrame   =  stylus.clusterBackValue && !prevClusterBackValue;
        BackButtonReleasedThisFrame  = !stylus.clusterBackValue &&  prevClusterBackValue;
        prevClusterBackValue         =  stylus.clusterBackValue;

        // Middle (from analog force, with hysteresis)
        bool middleDown;
        if (prevMiddleDown)
        {
            // Stay down until below release threshold
            middleDown = stylus.clusterMiddleValue > MiddleReleaseThreshold;
        }
        else
        {
            // Only go down after we pass press threshold
            middleDown = stylus.clusterMiddleValue > MiddlePressThreshold;
        }

        MiddleButtonPressedThisFrame  =  middleDown && !prevMiddleDown;
        MiddleButtonReleasedThisFrame = !middleDown &&  prevMiddleDown;
        prevMiddleDown                =  middleDown;

        // DEBUG: middle input
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"MXInkStylusHandler: middle force={stylus.clusterMiddleValue:F3}, middleDown={middleDown}");
        }
        if (MiddleButtonPressedThisFrame)
        {
            Debug.Log("MXInkStylusHandler: MiddleButtonPressedThisFrame TRUE");
        }
        if (MiddleButtonReleasedThisFrame)
        {
            Debug.Log("MXInkStylusHandler: MiddleButtonReleasedThisFrame TRUE");
        }

        // Any input active?
        stylus.any = stylus.tipValue > 0 || stylus.clusterFrontValue ||
                     stylus.clusterMiddleValue > 0 || stylus.clusterBackValue ||
                     stylus.clusterBackDoubleTapValue;

        // --- Visual feedback ---

        tip.GetComponent<MeshRenderer>().material.color =
            stylus.tipValue > 0 ? activeColor : defaultColor;

        clusterFront.GetComponent<MeshRenderer>().material.color =
            stylus.clusterFrontValue ? activeColorFront : defaultColor;

        clusterMiddle.GetComponent<MeshRenderer>().material.color =
            stylus.clusterMiddleValue > 0 ? activeColor : defaultColor;

        if (stylus.clusterBackValue)
        {
            clusterBack.GetComponent<MeshRenderer>().material.color =
                stylus.clusterBackValue ? activeColor : defaultColor;
        }
        else
        {
            clusterBack.GetComponent<MeshRenderer>().material.color =
                stylus.clusterBackDoubleTapValue ? doubleTapActiveColor : defaultColor;
        }

        // Optional: back double-tap haptic
        if (stylus.clusterBackDoubleTapValue)
        {
            TriggerHapticClick();
        }

        // Existing front button → DetectionManager behaviour
        if (stylus.clusterFrontValue)
        {
            if (detectionManager != null)
            {
                detectionManager.SpwanCurrentDetectedObjects();
            }
            else
            {
                TriggerHapticClick();
                Debug.LogWarning("MXInkStylusHandler: DetectionManager reference is not set.");
            }
        }

        // Update UI pointer
        UpdateUIPointer();
    }

    private void UpdateUIPointer()
    {
        if (lineRenderer == null || !stylus.isActive)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            if (reticle != null)
                reticle.SetActive(false);
            CurrentHitObject = null;
            CurrentHoveredMarker = null;
            CurrentHoveredUI = null;
            currentHoveredButton = null;
            return;
        }

        lineRenderer.enabled = true;

        Vector3 rayOrigin = RayOrigin;
        // Use the tip's negative up direction (flip 180 degrees)
        Vector3 rayDirection = (tip != null ? -tip.transform.up : (RayRotation * Vector3.forward));

        // Set line start position
        lineRenderer.SetPosition(0, rayOrigin);

        // Perform raycast for both 3D objects and UI
        RaycastHit hit;
        bool hit3D = Physics.Raycast(rayOrigin, rayDirection, out hit, maxPointerDistance, pointerLayerMask);

        if (hit3D)
        {
            // Hit something - position line and reticle at hit point
            lineRenderer.SetPosition(1, hit.point);
            
            // Track what we hit
            CurrentHitObject = hit.collider.gameObject;
            // Check for marker component on hit object or its parents
            CurrentHoveredMarker = hit.collider.GetComponentInParent<DetectionSpawnMarkerAnim>();
            // Check if we hit a UI element (Canvas with GraphicRaycaster)
            CurrentHoveredUI = hit.collider.GetComponentInParent<Canvas>() != null ? hit.collider.gameObject : null;
            currentHoveredButton = hit.collider.GetComponentInParent<Button>();
            
            // Debug: Log when hovering a marker
            if (CurrentHoveredMarker != null && Time.frameCount % 30 == 0)
            {
                string className = CurrentHoveredMarker.GetYoloClassName();
                Debug.Log($"[MXInkStylusHandler] Hovering marker: '{className}' (empty={string.IsNullOrEmpty(className)}) on GameObject: {hit.collider.gameObject.name}");
            }
            else if (currentHoveredButton != null && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[MXInkStylusHandler] Hovering UI Button: {currentHoveredButton.name}");
            }
            else if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[MXInkStylusHandler] Hit object: {hit.collider.gameObject.name}, marker: {(CurrentHoveredMarker != null)}, UI: {(CurrentHoveredUI != null)}");
            }
            
            if (reticle != null)
            {
                reticle.SetActive(true);
                reticle.transform.position = hit.point;
                reticle.transform.rotation = Quaternion.LookRotation(hit.normal);
                reticle.transform.localScale = reticleDefaultScale * reticleHoverScale;
            }

            // Change color based on what we're hovering
            Color hoverColor;
            if (CurrentHoveredMarker != null)
                hoverColor = Color.yellow; // Marker = yellow
            else if (currentHoveredButton != null)
                hoverColor = Color.green;  // UI Button = green
            else if (CurrentHoveredUI != null)
                hoverColor = Color.cyan;   // Other UI = cyan
            else
                hoverColor = pointerColorHover; // Default hover
                
            lineRenderer.startColor = hoverColor;
            lineRenderer.endColor = hoverColor;
            isHoveringUI = true;
        }
        else
        {
            // No hit - extend line to max distance
            Vector3 endPoint = rayOrigin + rayDirection * maxPointerDistance;
            lineRenderer.SetPosition(1, endPoint);
            
            // Clear tracked objects
            CurrentHitObject = null;
            CurrentHoveredMarker = null;
            CurrentHoveredUI = null;
            currentHoveredButton = null;

            if (reticle != null)
            {
                reticle.SetActive(false);
            }

            // Reset to default color
            lineRenderer.startColor = pointerColorDefault;
            lineRenderer.endColor = pointerColorDefault;
            isHoveringUI = false;
        }
    }

    public void TriggerHapticPulse(float amplitude, float duration)
    {
        OVRPlugin.Hand holdingHand =
            stylus.isOnRightHand ? OVRPlugin.Hand.HandRight : OVRPlugin.Hand.HandLeft;

        OVRPlugin.TriggerVibrationAction(MX_Ink_Haptic_Pulse, holdingHand, duration, amplitude);
    }

    public void TriggerHapticClick()
    {
        TriggerHapticPulse(hapticClickAmplitude, hapticClickDuration);
    }

    /// <summary>
    /// Trigger a UI button click if hovering over one
    /// </summary>
    public bool TriggerUIButtonClick()
    {
        if (currentHoveredButton != null && currentHoveredButton.interactable)
        {
            Debug.Log($"[MXInkStylusHandler] Clicking UI button: {currentHoveredButton.name}");
            currentHoveredButton.onClick.Invoke();
            TriggerHapticClick();
            return true;
        }
        return false;
    }
}
