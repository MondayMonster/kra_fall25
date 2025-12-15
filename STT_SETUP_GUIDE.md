# Speech-to-Text (STT) Setup Guide
## OpenAI Whisper API Integration for Spanish Language Learning

---

## 🎯 Overview
This guide explains how to set up the Speech-to-Text system in your Unity scene for Spanish pronunciation practice.

**New Workflow:**
1. **Marker UI** → User sees Spanish word + TTS (front button)
2. **Practice UI** → User practices saying the Spanish word (STT recording)
3. **Writer UI** → User practices writing the Spanish word (drawing)

---

## 📦 Components Created

### 1. **STTManager.cs**
- Handles microphone recording and OpenAI Whisper API calls
- Records 5 seconds of audio at 44.1kHz
- Converts audio to WAV format
- Sends to Whisper API for Spanish transcription
- Returns transcribed text via callback

### 2. **PracticeUIController.cs**
- UI for speech practice between Marker UI and Writer UI
- Displays "Practice saying: [Spanish word]"
- Shows transcribed result
- Validates pronunciation (optional matching)
- Double-click back button → transitions to Writer UI

### 3. **DetectionManager.cs** (Updated)
- New field: `m_practiceUIPrefab`
- Flow: Marker UI → Practice UI → Writer UI
- Handles UI transitions and cleanup

---

## 🛠️ Scene Setup Instructions

### Step 1: Create STTManager GameObject

1. **Create GameObject:**
   - Right-click in Hierarchy → Create Empty
   - Name: `STTManager`

2. **Add Component:**
   - Add Component → `STTManager` script

3. **Configure Inspector:**
   ```
   Open AI Api Key: [Your OpenAI API Key] (same key as GPT/TTS)
   Whisper Model: "whisper-1"
   Language: "es" (Spanish)
   Recording Length Seconds: 5
   Recording Frequency: 44100
   Log STT Events: ✓ (checked for debugging)
   ```

---

### Step 2: Create Practice UI Prefab

1. **Create Canvas:**
   - GameObject → UI → Canvas
   - Name: `PracticeUI`
   - Canvas Component:
     - Render Mode: `World Space`
     - Width: 800
     - Height: 600
     - Scale: 0.001, 0.001, 0.001

2. **Add Background Panel:**
   - Right-click Canvas → UI → Panel
   - Name: `Background`
   - Color: Semi-transparent (RGBA: 0, 0, 0, 200)

3. **Add Instruction Text:**
   - Right-click Canvas → UI → Text - TextMeshPro
   - Name: `InstructionText`
   - Text: "Practice saying:"
   - Font Size: 48
   - Alignment: Center
   - Position: Top of canvas

4. **Add Spanish Word Text:**
   - Right-click Canvas → UI → Text - TextMeshPro
   - Name: `SpanishWordText`
   - Text: "[Spanish Word]"
   - Font Size: 72
   - Alignment: Center
   - Color: Yellow or accent color
   - Position: Center of canvas

5. **Add Transcribed Text:**
   - Right-click Canvas → UI → Text - TextMeshPro
   - Name: `TranscribedText`
   - Text: "Press record to start..."
   - Font Size: 36
   - Alignment: Center
   - Position: Below Spanish word

6. **Add Recording Indicator (Optional):**
   - Right-click Canvas → UI → Image
   - Name: `RecordingIndicator`
   - Sprite: Red circle (or animated mic icon)
   - Position: Top-right corner
   - **Disable by default** (unchecked)

7. **Add Record Button (Optional):**
   - Right-click Canvas → UI → Button - TextMeshPro
   - Name: `RecordButton`
   - Text: "🎤 Record"
   - Position: Bottom-left

8. **Add Stop Button (Optional):**
   - Right-click Canvas → UI → Button - TextMeshPro
   - Name: `StopButton`
   - Text: "⏹ Stop"
   - Position: Bottom-right
   - **Disable by default** (unchecked)

9. **Add Box Collider (IMPORTANT):**
   - Select `PracticeUI` Canvas
   - Add Component → Box Collider
   - Size: Adjust to cover entire canvas area
   - This allows stylus ray interaction

10. **Add PracticeUIController Script:**
    - Select `PracticeUI` Canvas
    - Add Component → `PracticeUIController`
    - **Assign References:**
      ```
      Instruction Text: InstructionText
      Spanish Word Text: SpanishWordText
      Transcribed Text: TranscribedText
      Recording Indicator: RecordingIndicator
      Record Button: RecordButton
      Stop Button: StopButton
      Stt Manager: (Leave empty - will auto-find)
      Stylus Handler: (Leave empty - will auto-find)
      ```

11. **Save as Prefab:**
    - Drag `PracticeUI` from Hierarchy to Assets folder
    - Delete from Hierarchy

---

### Step 3: Update DetectionManager

1. **Find DetectionManager:**
   - In Hierarchy, find your `DetectionManager` GameObject

2. **Assign Practice UI Prefab:**
   - Inspector → Marker Interaction section
   - `M Practice UI Prefab`: Drag your `PracticeUI` prefab here

---

### Step 4: Verify Existing Setup

**Ensure these are already configured:**

✅ **TTSManager:**
- GameObject exists in scene
- OpenAI API Key set
- Voice Model: "tts-1"
- Voice Name: "nova"

✅ **Marker UI Prefab:**
- Has `MarkerUIController` component
- TTS Manager reference assigned (or auto-finds)

✅ **Writer UI Prefab:**
- Has `WriterUIController` component
- Assigned to DetectionManager

✅ **DetectionManager:**
- `m_markerUIPrefab`: Assigned
- `m_practiceUIPrefab`: **NEW - Assign Practice UI**
- `m_writerUIPrefab`: Assigned
- `m_wordDrawingManager`: Assigned

---

## 🎮 User Interaction Flow

### 1. **Detect & Spawn Markers**
- Front button → Spawn markers on detected objects

### 2. **Marker UI** (TTS Active)
- Back button (single) on marker → Open Marker UI
- Shows: Object name, Spanish translation, example sentences
- **Front button → Speak Spanish texts** (pronunciation, simple sentence, positional sentence)
- Back button (double-click) → Close Marker UI, Open Practice UI

### 3. **Practice UI** (STT Active) ⭐ NEW
- Shows: "Practice saying: [Spanish word]"
- **Record Options:**
  - Click Record button
  - OR press front button (auto-implemented in future)
- User says Spanish word
- Click Stop button or auto-stop after 5 seconds
- STT transcribes audio via Whisper API
- Shows: "You said: [transcribed text]"
- ✓ or ✗ feedback if pronunciation matches
- **Back button (double-click) → Transition to Writer UI**

### 4. **Writer UI** (Drawing Active)
- Shows: Spanish word to practice writing
- Middle button → Draw in 3D space
- Back button (double-click) → Close UI, show all markers

---

## 🔑 API Configuration

### OpenAI API Key
You need **ONE** API key for all three services:
1. GPT-4o-mini (language data)
2. TTS (text-to-speech)
3. Whisper (speech-to-text)

**Get your key:**
- Visit: https://platform.openai.com/api-keys
- Create new secret key
- Copy and paste into:
  - `GPTLanguageService.openAIApiKey`
  - `TTSManager.openAIApiKey`
  - `STTManager.openAIApiKey` ⭐ NEW

### Cost Estimate (OpenAI)
- **GPT-4o-mini**: ~$0.15 per 1M input tokens (~$0.0001 per request)
- **TTS (tts-1)**: ~$15 per 1M characters (~$0.0003 per 3 sentences)
- **Whisper**: ~$0.006 per minute (~$0.0005 per 5-second recording)
- **Total per interaction**: ~$0.001 (less than 1 cent)

---

## 🐛 Debugging

### Enable Logs
All three managers have debug logging:
- `GPTLanguageService.logApiCalls`: ✓
- `TTSManager.logSpeechEvents`: ✓
- `STTManager.logSTTEvents`: ✓ ⭐ NEW

### Common Issues

**❌ "OpenAI API Key not set!"**
- Solution: Add your API key to STTManager Inspector

**❌ "No microphone detected!"**
- Solution: Ensure Quest 3 has microphone permissions
- Check: OVR → Permission Requests → Microphone

**❌ "Whisper API Error: 401 Unauthorized"**
- Solution: Invalid API key, check for typos

**❌ "Whisper API Error: 400 Bad Request"**
- Solution: Audio file issue, check recording length > 0

**❌ STT doesn't start**
- Solution: Ensure Practice UI is active (check gameObject.activeInHierarchy)
- Solution: Check microphone permissions in Quest settings

**❌ "Transcription failed!"**
- Check Console for API error details
- Verify API key is valid
- Check internet connection
- Ensure audio was recorded (length > 0)

---

## 🎤 Microphone Permissions (Quest 3)

### Enable in Unity:
1. Edit → Project Settings → XR Plug-in Management → Oculus
2. Check: `Record Audio Permission`

### Enable on Device:
1. Settings → Apps → Your App → Permissions
2. Enable: Microphone

---

## 🧪 Testing Workflow

1. **Test TTS First:**
   - Spawn marker → Open Marker UI
   - Press front button → Hear Spanish audio
   - ✓ Verify audio plays

2. **Test STT (NEW):**
   - From Marker UI → Double-click back button
   - Practice UI appears
   - Click Record button (or press front button)
   - Say Spanish word clearly
   - Click Stop (or wait 5 seconds)
   - ✓ Verify transcription appears

3. **Test Full Flow:**
   - Marker UI (TTS) → Practice UI (STT) → Writer UI (Drawing)
   - Double-click back button at each stage
   - ✓ Verify transitions work smoothly

---

## 📋 Checklist

Before testing, verify:

- [ ] STTManager GameObject created with script
- [ ] OpenAI API Key added to STTManager
- [ ] Practice UI prefab created with all text fields
- [ ] PracticeUIController script added to Practice UI Canvas
- [ ] Box Collider added to Practice UI Canvas
- [ ] Practice UI saved as prefab
- [ ] Practice UI prefab assigned to DetectionManager.m_practiceUIPrefab
- [ ] Microphone permissions enabled in Project Settings
- [ ] STTManager.logSTTEvents enabled for debugging
- [ ] All UI text references assigned in PracticeUIController

---

## 🎯 Next Steps

After completing this setup:
1. Test the full workflow: Marker → Practice → Writer
2. Adjust recording length in STTManager if needed (default: 5 seconds)
3. Customize Practice UI appearance (colors, fonts, layout)
4. Optional: Add visual feedback for pronunciation match/mismatch
5. Optional: Add retry button in Practice UI
6. Optional: Implement front button to start recording (currently button-only)

---

## 📚 API Documentation

- **OpenAI Whisper API**: https://platform.openai.com/docs/guides/speech-to-text
- **Supported Languages**: https://platform.openai.com/docs/guides/speech-to-text/supported-languages
- **Audio Format Requirements**: WAV, MP3, M4A (max 25 MB)

---

**Setup Complete! 🎉**

Your Spanish language learning app now has full TTS and STT capabilities!
