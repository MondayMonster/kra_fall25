# Text-to-Speech Setup for Marker UI

## Overview
Spanish text-to-speech has been added to the Marker UI using Meta's Voice SDK TTS Building Block. Users can click speaker buttons to hear Spanish translations and example sentences.

## Setup Steps

### 1. Install Voice SDK Package
1. Open Unity Package Manager (Window > Package Manager)
2. Click the **+** button and select "Add package by name"
3. Enter: `com.meta.xr.sdk.voice`
4. Click **Add**

### 2. Add TTS Building Block to Scene
1. In Unity, go to: **Meta > Building Blocks > Add Building Block**
2. Find and add: **Text-to-Speech**
3. This creates two GameObjects in your scene:
   - `TTSWitService` (TTS service component)
   - `TTSSpeaker` (audio playback component)

### 3. Configure TTSSpeaker for Spanish
1. Select the `TTSSpeaker` GameObject in the hierarchy
2. In the Inspector, find the **TTSSpeaker** component
3. Set the following:
   - **Voice Preset Name**: `es_ES` (for Spanish)
   - Or create a custom preset with Spanish voice settings
   - Keep other settings at default

### 4. Add TTSManager to Scene
1. Create an empty GameObject called "TTSManager"
2. Add the `TTSManager` component script
3. In the Inspector:
   - **TTS Speaker**: Drag the `TTSSpeaker` GameObject
   - **Voice Locale**: Set to `es_ES`
   - **Log Speech Events**: Check for debugging

### 5. Update Marker UI Prefab
1. Open your Marker UI prefab
2. Add **3 speaker buttons** next to the Spanish text fields:
   - One next to "Spanish Translation" text
   - One next to "Simple Sentence Spanish" text
   - One next to "Positional Sentence Spanish" text

3. Select the Marker UI root GameObject
4. In the `MarkerUIController` component, assign:
   - **Speak Translation Button**: Drag the translation speaker button
   - **Speak Simple Sentence Button**: Drag the simple sentence speaker button
   - **Speak Positional Sentence Button**: Drag the positional sentence speaker button
   - **TTS Manager**: Drag the TTSManager GameObject from the scene

### 6. (Optional) Design Speaker Button Icons
- Use a speaker/sound wave icon (🔊)
- Recommended size: 40x40 pixels for UI buttons
- Color: White or light blue for visibility
- Add hover state for visual feedback

## Testing

1. **Build and deploy** to Quest 3
2. **Click on a marker** → Marker UI appears with GPT data
3. **Click speaker buttons** → Should hear Spanish audio
4. Check console logs for TTS events:
   - "Speech queued"
   - "Speech started playing"
   - "Speech finished"

## How It Works

### Code Flow
```
User clicks speaker button 
  → MarkerUIController.OnSpeakXXXClicked()
  → TTSManager.SpeakSpanish(spanishText)
  → TTSSpeaker.Speak(text)
  → Meta Voice SDK synthesizes audio
  → Audio plays through TTSSpeaker AudioSource
```

### Speaker Buttons
- **Translation Button**: Speaks single Spanish word (e.g., "laptop" → "portátil")
- **Simple Sentence Button**: Speaks Spanish sentence without preposition
- **Positional Sentence Button**: Speaks Spanish sentence with positional preposition

## Troubleshooting

### No Audio Playing
- Check that Voice SDK package is installed
- Verify TTSSpeaker has an AudioSource component
- Check device volume is not muted
- Look for errors in Console

### Wrong Language
- Verify Voice Locale is set to `es_ES` in TTSManager
- Check TTSSpeaker voice preset matches Spanish

### Button Not Responding
- Verify buttons are assigned in MarkerUIController Inspector
- Check button has EventSystem in scene for clicks
- Look for warning logs about missing TTS Manager

### Audio Cuts Off
- Check if multiple TTS calls are conflicting
- TTSManager.StopSpeaking() can be called before new speech

## Performance Notes

✅ **Native Quest Integration** - Runs on-device, no latency
✅ **Offline Support** - Works without internet after initial setup
✅ **Low Overhead** - TTS is hardware accelerated on Quest
✅ **No API Costs** - Free with Meta Voice SDK

## Files Modified

1. **TTSManager.cs** (NEW)
   - Wraps Meta Voice SDK TTSSpeaker
   - SpeakSpanish(text) method
   - Event handling for speech lifecycle

2. **MarkerUIController.cs** (UPDATED)
   - Added 3 speaker button references
   - Added TTSManager reference
   - OnSpeakXXXClicked() button handlers

## Next Steps (Optional Enhancements)

- Add visual feedback animation to speaker buttons while playing
- Show waveform or audio visualization during speech
- Add "Stop Speaking" button to interrupt long sentences
- Cache audio clips to avoid re-generating same text
- Add different voice options (male/female, different accents)
