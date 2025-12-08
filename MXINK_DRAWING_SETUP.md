# MX Ink Drawing Setup - Updated Behavior

## What Changed

### 1. Ray Pointer Disabled During UI
- The stylus ray (line + reticle) is now **disabled** when either Marker UI or Writer UI is open
- Ray automatically re-enables when UI is closed
- This prevents unwanted UI interactions while viewing/practicing

### 2. Drawing Trigger Changed: Tip → Middle Button
- **OLD**: Drawing activated by pressing the stylus tip down
- **NEW**: Drawing activated by pressing the **middle button** on MX Ink
- Drawing only works when **Writer UI is active**

### 3. Drawing Enable/Disable Flow
```
No UI Active → Ray ENABLED, Drawing DISABLED, Grab word with middle button
  ↓ (back button press on marker)
Marker UI Open → Ray DISABLED, Drawing DISABLED
  ↓ (back button double-click)
Writer UI Open → Ray DISABLED, Drawing ENABLED (middle button)
  ↓ (back button double-click)
Back to markers → Ray ENABLED, Drawing DISABLED
```

## Unity Inspector Setup Required

### DetectionManager Component
You need to assign the **Word Drawing Manager** reference:
1. Select the GameObject with `DetectionManager` component
2. Find the "Word Drawing Manager" field in the Inspector
3. Drag the GameObject that has the `WordDrawingManager` component into this field

This allows DetectionManager to enable/disable drawing when Writer UI opens/closes.

## Testing Checklist

- [ ] **Start**: Ray visible, can point at markers/UI, turning yellow/cyan/green
- [ ] **Click marker with back button**: Ray disappears, Marker UI shows GPT data
- [ ] **Try drawing with middle button**: Nothing happens (correct - drawing disabled)
- [ ] **Double-click back button**: Marker UI closes → Writer UI appears with Spanish word
- [ ] **Ray still gone**: Correct - ray disabled during UI
- [ ] **Press and hold middle button**: Draw strokes in 3D space
- [ ] **Release middle button**: Stroke completes
- [ ] **Double-click back button again**: Writer UI closes → all markers reappear
- [ ] **Ray comes back**: Can point at markers again
- [ ] **Try middle button now**: Grabs previously drawn words (if any), doesn't draw new ones

## Technical Summary

### Modified Files
1. **MXInkStylusHandler.cs**
   - Added `SetUIActive(bool)` method to control ray visibility
   - Added `IsMiddleButtonDown` property for drawing system
   - Ray disabled when `isUIActive = true`

2. **DetectionManager.cs**
   - Calls `SetUIActive(true)` when spawning Marker UI
   - Calls `SetUIActive(false)` when closing Writer UI
   - Passes `WordDrawingManager` reference to Writer UI

3. **WordDrawingManager.cs**
   - Changed drawing trigger from `IsTipDown` to `IsMiddleButtonDown`
   - Added `SetDrawingEnabled(bool)` method
   - Drawing only works when `drawingEnabled = true`
   - Grabbing words only works when `drawingEnabled = false` (no conflict)

4. **WriterUIController.cs**
   - Calls `SetDrawingEnabled(true)` when initialized
   - Calls `SetDrawingEnabled(false)` when closed

## User Controls Summary

| Button | No UI | Marker UI | Writer UI |
|--------|-------|-----------|-----------|
| **Back (single)** | Spawn Marker UI on hovered marker | - | - |
| **Back (double)** | - | Close Marker UI → Open Writer UI | Close Writer UI → Show markers |
| **Middle (press)** | Grab existing drawn words | - | Draw strokes in 3D |
| **Tip (hover)** | Ray pointer (yellow/cyan/green) | - | - |

## Notes
- Ray pointer only appears when NO UI is active
- Drawing only available during Writer UI (practice mode)
- Word grabbing only available when NOT in Writer UI
- All UI transitions preserve the Spanish word data for handoff
