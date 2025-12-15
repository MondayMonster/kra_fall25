using System.Collections.Generic;
using UnityEngine;

public class WordDrawingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MXInkStylusHandler stylus;
    [SerializeField] private Transform wordsRoot;     // Parent for all words
    [SerializeField] private GameObject anchorPrefab; // 3D anchor visual
    [SerializeField] private LayerMask anchorLayer;   // Layer mask used to detect anchors

    [Header("Stroke Settings")]
    [SerializeField] private float strokeWidth = 0.0025f;
    [SerializeField] private Color strokeColor = Color.white;

    [Header("Grab Settings")]
    [SerializeField] private float grabSphereRadius = 0.08f; // radius around tip to search for anchors

    // Drawing control
    private bool drawingEnabled = false;
    
    /// <summary>
    /// Enable or disable drawing functionality. Should only be enabled when Writer UI is active.
    /// </summary>
    public void SetDrawingEnabled(bool enabled)
    {
        drawingEnabled = enabled;
        Debug.Log($"[WordDrawingManager] Drawing {(enabled ? "ENABLED" : "DISABLED")}");
        
        // If disabling mid-stroke, end it
        if (!enabled && isStrokeActive)
        {
            EndStroke();
        }
    }

    /// <summary>
    /// Finalize and anchor the current drawing (called when closing Writer UI)
    /// </summary>
    public void FinalizeCurrentDrawing()
    {
        if (currentWord != null && currentWord.strokes.Count > 0)
        {
            Debug.Log("[WordDrawingManager] Finalizing current drawing from UI close button");
            TryFinalizeCurrentWord();
        }
        else
        {
            Debug.Log("[WordDrawingManager] No drawing to finalize");
        }
    }

    // --- Data class for a single word ---
    [System.Serializable]
    public class WordDrawing
    {
        public GameObject root;                   // Parent GO for this word
        public List<LineRenderer> strokes = new();
        public GameObject anchor;                 // Anchor object
        public string recognizedText = "";        // For future OCR/use
    }

    // Current drawing state
    private WordDrawing currentWord;
    private LineRenderer currentLine;
    private bool isStrokeActive;

    // Anchored words in the scene
    private readonly List<WordDrawing> anchoredWords = new();

    // Grab/move state
    private WordDrawing grabbedWord;
    private bool isGrabbingWord;
    private Vector3 grabbedAnchorLocalOffset; // anchorWorldPos - rootWorldPos at grab time

    private void Update()
    {
        if (stylus == null) return;

        // Only allow drawing when Writer UI is active
        if (drawingEnabled)
        {
            HandleDrawing();
            HandleWordFinalize();
        }

        // Grabbing words is always available (even when not in Writer UI)
        HandleWordGrab();

        // Debug: visualize ray in Scene view
        Debug.DrawRay(
            stylus.RayOrigin,
            stylus.RayRotation * Vector3.forward * 0.3f,
            Color.red
        );
    }

    // ==========================
    //  DRAWING A WORD
    // ==========================

    private void HandleDrawing()
    {
        // Changed from tip press to middle button press
        if (stylus.IsMiddleButtonDown)
        {
            EnsureCurrentWord();

            if (!isStrokeActive)
            {
                StartNewStroke();
            }

            AddPointToStroke();
        }
        else if (isStrokeActive)
        {
            EndStroke();
        }
    }

    private void EnsureCurrentWord()
    {
        if (currentWord != null) return;

        currentWord = new WordDrawing();
        currentWord.root = new GameObject("Word");

        if (wordsRoot != null)
            currentWord.root.transform.SetParent(wordsRoot, false);
    }

    private void StartNewStroke()
    {
        var lineGO = new GameObject("LineStroke");
        lineGO.transform.SetParent(currentWord.root.transform, false);

        currentLine = lineGO.AddComponent<LineRenderer>();
        currentLine.positionCount = 0;
        currentLine.material = new Material(Shader.Find("Sprites/Default"));
        currentLine.startColor = strokeColor;
        currentLine.endColor = strokeColor;
        currentLine.startWidth = strokeWidth;
        currentLine.endWidth = strokeWidth;

        // Use local space so moving the word root moves entire stroke
        currentLine.useWorldSpace = false;

        currentWord.strokes.Add(currentLine);
        isStrokeActive = true;
    }

    private void AddPointToStroke()
    {
        // Drawing uses stylus pose; you can swap to stylus.RayOrigin if you prefer tip-based drawing
        Vector3 localPos = currentLine.transform.InverseTransformPoint(stylus.StylusPosition);
        currentLine.positionCount++;
        currentLine.SetPosition(currentLine.positionCount - 1, localPos);
    }

    private void EndStroke()
    {
        isStrokeActive = false;
        currentLine = null;
    }

    // ==========================
    //  FINALIZE WORD (BACK BUTTON)
    // ==========================

    private void HandleWordFinalize()
    {
        if (stylus.BackButtonPressedThisFrame)
        {
            TryFinalizeCurrentWord();
        }
    }

    private void TryFinalizeCurrentWord()
    {
        if (currentWord == null || currentWord.strokes.Count == 0)
        {
            return; // nothing drawn
        }

        // Compute bounds of all strokes in world space
        bool hasPoint = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        foreach (var stroke in currentWord.strokes)
        {
            for (int i = 0; i < stroke.positionCount; i++)
            {
                Vector3 worldPos = stroke.transform.TransformPoint(stroke.GetPosition(i));
                if (!hasPoint)
                {
                    bounds = new Bounds(worldPos, Vector3.zero);
                    hasPoint = true;
                }
                else
                {
                    bounds.Encapsulate(worldPos);
                }
            }
        }

        if (!hasPoint)
        {
            currentWord = null;
            return;
        }

        Vector3 anchorPos = bounds.center + Vector3.up * 0.02f; // a bit above the word

        // Spawn anchor
        if (anchorPrefab != null)
        {
            GameObject anchorGO = Instantiate(anchorPrefab, anchorPos, Quaternion.identity);
            anchorGO.transform.SetParent(currentWord.root.transform, true);
            currentWord.anchor = anchorGO;

            // Ensure collider for selection (if prefab doesn’t have one)
            Collider col = anchorGO.GetComponent<Collider>();
            if (col == null)
            {
                var sphere = anchorGO.AddComponent<SphereCollider>();
                sphere.radius = 0.03f;
                col = sphere;
            }

            // Ensure it's on the anchor layer (assumes single-bit layer mask)
            if (anchorLayer.value != 0)
            {
                int layerIndex = Mathf.RoundToInt(Mathf.Log(anchorLayer.value, 2));
                anchorGO.layer = layerIndex;
            }

            Debug.Log(
                $"WordDrawingManager: Created anchor '{anchorGO.name}' at {anchorGO.transform.position}, " +
                $"layer={anchorGO.layer}, colliderBounds={col.bounds}"
            );
        }

        // Store this word and clear current
        anchoredWords.Add(currentWord);
        Debug.Log($"WordDrawingManager: anchoredWords.Count = {anchoredWords.Count}");

        currentWord = null;

        // Little haptic to confirm
        stylus.TriggerHapticClick();
    }

    // ==========================
    //  GRAB / MOVE WORD (MIDDLE BUTTON)
    // ==========================

    private void HandleWordGrab()
    {
        // Start grab when MIDDLE button pressed & not already grabbing
        // Note: This conflicts with drawing when Writer UI is active, so grabbing only works when drawing is disabled
        if (stylus.MiddleButtonPressedThisFrame && !isGrabbingWord && !drawingEnabled)
        {
            Debug.Log("WordDrawingManager: Middle button press detected, trying to grab...");
            TryStartGrabWord();
        }

        // Update grabbed word while holding middle button
        if (isGrabbingWord && grabbedWord != null)
        {
            UpdateGrabbedWordTransform();
        }

        // Stop grab on middle button release
        if (stylus.MiddleButtonReleasedThisFrame && isGrabbingWord)
        {
            isGrabbingWord = false;
            grabbedWord = null;
        }
    }

    private void TryStartGrabWord()
    {
        Vector3 origin = stylus.RayOrigin;

        // If no mask set in inspector, query against all layers
        int maskToUse = (anchorLayer.value == 0)
            ? Physics.DefaultRaycastLayers
            : anchorLayer.value;

        Debug.Log($"WordDrawingManager: OverlapSphere origin={origin}, radius={grabSphereRadius}, mask={maskToUse}");

        // Find all colliders near the tip
        Collider[] hits = Physics.OverlapSphere(origin, grabSphereRadius, maskToUse);

        if (hits.Length == 0)
        {
            Debug.Log("WordDrawingManager: OverlapSphere found NO colliders near tip.");
            return;
        }

        Debug.Log($"WordDrawingManager: OverlapSphere found {hits.Length} collider(s).");

        float bestDist = float.MaxValue;
        WordDrawing bestWord = null;

        foreach (var hit in hits)
        {
            Debug.Log($"WordDrawingManager: Overlap hit {hit.name} on layer {hit.gameObject.layer}");

            foreach (var word in anchoredWords)
            {
                if (word.anchor == null) continue;

                if (hit.gameObject == word.anchor ||
                    hit.transform.IsChildOf(word.anchor.transform))
                {
                    float dist = Vector3.Distance(origin, word.anchor.transform.position);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestWord = word;
                    }
                }
            }
        }

        if (bestWord != null)
        {
            grabbedWord = bestWord;
            isGrabbingWord = true;

            Transform root   = grabbedWord.root.transform;
            Transform anchor = grabbedWord.anchor.transform;

            // How far is the anchor from the root in world space?
            grabbedAnchorLocalOffset = anchor.position - root.position;

            Debug.Log(
                $"WordDrawingManager: Started grabbing word '{root.name}' at distance {bestDist}, " +
                $"anchorOffset={grabbedAnchorLocalOffset}"
            );

            stylus.TriggerHapticClick();
        }
        else
        {
            Debug.Log("WordDrawingManager: OverlapSphere hit something, but no anchors matched anchoredWords.");
        }
    }

    private void UpdateGrabbedWordTransform()
    {
        if (grabbedWord == null) return;

        Transform root = grabbedWord.root.transform;

        // Move root so that the anchor ends up at the tip
        // anchorPos' = rootPos' + (anchorPos - rootPos)  => rootPos' = tipPos - (anchorPos - rootPos)
        root.position = stylus.RayOrigin - grabbedAnchorLocalOffset;

        // Keep rotation as-is for now (simpler & less “spinning”)
        // If you later want word to follow stylus rotation, we can add that.
    }
}
