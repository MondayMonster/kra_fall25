# GPT Language Learning Integration - Setup Guide

## 📋 Overview
This system integrates OpenAI's GPT API to provide:
1. Spanish translations of detected objects
2. Simple descriptive sentences in Spanish
3. Simple descriptive sentences in English
4. Positional/prepositional sentences in Spanish
5. Positional/prepositional sentences in English

For detected objects in your AR environment.

---

## 🛠️ What You Need

### 1. **OpenAI API Key**
- Sign up at: https://platform.openai.com/signup
- Get API key: https://platform.openai.com/api-keys
- **Cost**: ~$0.15-0.60 per 1000 calls (using gpt-4o-mini)

### 2. **Unity Components Created**
✅ `GPTLanguageService.cs` - API service manager
✅ `MarkerUIController.cs` - UI display controller
✅ Updated `DetectionManager.cs` - Integration with markers

### 3. **UI Elements Needed**
- World Space Canvas
- TextMeshPro text fields (6):
  - Object Name
  - Spanish Translation
  - Simple Sentence (Spanish)
  - Simple Sentence (English)
  - Positional Sentence (Spanish)
  - Positional Sentence (English)
- Loading indicator (optional)
- Close button with collider

---

## 📝 Step-by-Step Setup

### **STEP 1: Get Your OpenAI API Key**

1. Go to https://platform.openai.com/api-keys
2. Click "Create new secret key"
3. Name it "Unity-VR-App"
4. **Copy the key immediately** (you won't see it again!)
5. Keep it secure - don't commit to Git

---

### **STEP 2: Add GPT Service to Scene**

1. In Unity Hierarchy, find or create an empty GameObject named "Services"
2. Add Component → `GPTLanguageService`
3. **Configure in Inspector:**
   ```
   API Key: [paste your OpenAI key]
   Model: gpt-4o-mini (recommended) or gpt-4o
   Max Tokens: 150
   Temperature: 0.7
   Enable Debug Logs: ✓ (for testing)
   ```

---

### **STEP 3: Create UI Prefab**

#### A. Create Canvas:
1. GameObject → UI → Canvas
2. Name it "MarkerInfoUI"
3. Set Canvas properties:
   - Render Mode: **World Space**
   - Width: 400
   - Height: 300
   - Scale: 0.001, 0.001, 0.001

#### B. Add Background Panel:
1. Right-click Canvas → UI → Panel
2. Name: "Background"
3. Set color with some transparency

#### C. Add Text Elements:
Create 6 TextMeshPro text objects:

**Object Name (Header):**
```
Name: txt_ObjectName
Font Size: 36
Alignment: Center Top
Text: "LAPTOP"
Position: (0, 130, 0)
```

**Spanish Translation:**
```
Name: txt_Spanish
Font Size: 28
Alignment: Center
Text: "el portátil"
Position: (0, 80, 0)
Color: Yellow/Gold
```

**Simple Sentence (Spanish):**
```
Name: txt_SimpleSentenceSpanish
Font Size: 20
Alignment: Center Left
Text: "El portátil es negro."
Position: (-150, 30, 0)
Width: 350
Color: Light Blue
```

**Simple Sentence (English):**
```
Name: txt_SimpleSentenceEnglish
Font Size: 18
Alignment: Center Left
Text: "The laptop is black."
Position: (-150, 0, 0)
Width: 350
Color: Gray (translation)
```

**Positional Sentence (Spanish):**
```
Name: txt_PositionalSentenceSpanish
Font Size: 20
Alignment: Center Left
Text: "El portátil está sobre el escritorio."
Position: (-150, -40, 0)
Width: 350
Color: Light Green
```

**Positional Sentence (English):**
```
Name: txt_PositionalSentenceEnglish
Font Size: 18
Alignment: Center Left
Text: "The laptop is on the desk."
Position: (-150, -70, 0)
Width: 350
Color: Gray (translation)
```

#### D. Add Loading Indicator:
1. Create a simple rotating icon or text "Loading..."
2. Name: "LoadingIndicator"

#### E. Add Close Button:
1. Right-click Canvas → UI → Button - TextMeshPro
2. Name: "btn_Close"
3. Position: Top right corner (150, -120, 0)
4. Text: "✕" or "Close"
5. **Add BoxCollider** to button GameObject
6. Set collider size to match button

#### F. Add MarkerUIController:
1. Select the Canvas GameObject
2. Add Component → `MarkerUIController`
3. **Assign References in Inspector:**
   ```
   Object Name Text: txt_ObjectName
   Spanish Translation Text: txt_Spanish
   Simple Sentence Spanish Text: txt_SimpleSentenceSpanish
   Simple Sentence English Text: txt_SimpleSentenceEnglish
   Positional Sentence Spanish Text: txt_PositionalSentenceSpanish
   Positional Sentence English Text: txt_PositionalSentenceEnglish
   Loading Indicator: LoadingIndicator GameObject
   Close Button: btn_Close
   GPT Service: (drag Services/GPTLanguageService)
   ```

#### G. Save as Prefab:
1. Drag "MarkerInfoUI" from Hierarchy to Project → Assets/Prefabs/
2. Delete from scene

---

### **STEP 4: Configure Detection Manager**

1. Select DetectionManager in Hierarchy
2. In Inspector, find "Marker Interaction" section:
   ```
   Marker UI Prefab: [drag MarkerInfoUI prefab]
   UI Spawn Offset: (0, 0.2, 0)
   ```

---

### **STEP 5: Test the System**

#### Quick Test (in Editor):
1. Select the Services GameObject with GPTLanguageService
2. In Inspector, right-click on script → **Test API Call**
3. Check Console for response

#### Full Flow Test:
1. Build and run on Quest
2. Detect objects with stylus (front button)
3. Point at a marker, press back button
4. UI should appear and populate with GPT data

---

## 🧪 Testing Checklist

### API Test:
- [ ] API key is set
- [ ] Test API Call works in Editor
- [ ] Check Unity Console for GPT response
- [ ] Verify JSON parsing works

### UI Test:
- [ ] UI spawns at marker location
- [ ] UI faces camera
- [ ] Loading indicator shows
- [ ] Text fields populate correctly
- [ ] Close button has collider
- [ ] Close button works with stylus ray

### Integration Test:
- [ ] Markers spawn correctly
- [ ] Ray detects markers (yellow color)
- [ ] Back button triggers interaction
- [ ] All markers hide
- [ ] UI appears with correct data
- [ ] Spanish translation is accurate
- [ ] Sentences make sense

---

## 🐛 Troubleshooting

### "API Key not configured"
→ Set API key in GPTLanguageService Inspector

### "Service not available"
→ Make sure GPTLanguageService is in scene
→ Assign it to MarkerUIController

### "Parse error"
→ Check Console for raw API response
→ GPT might have returned non-JSON (rare)

### UI doesn't appear
→ Check DetectionManager has UI prefab assigned
→ Check Console for spawn logs

### Button won't click
→ Add BoxCollider to button GameObject
→ Make sure button is on a layer in pointerLayerMask

### Wrong sentences
→ Adjust temperature (lower = more conservative)
→ Try gpt-4o instead of gpt-4o-mini

---

## 💰 Cost Estimation

**Using gpt-4o-mini:**
- Input: ~100 tokens per request
- Output: ~50 tokens per response
- Cost: ~$0.0003 per object
- **1000 objects ≈ $0.30**

**Using gpt-4o:**
- Same tokens
- Cost: ~$0.002 per object
- **1000 objects ≈ $2.00**

---

## 🔒 Security Best Practices

### For Development:
- Store API key in Inspector (for testing)
- Don't commit scenes with API key

### For Production:
1. Create a ScriptableObject to store key
2. Load key from secure server at runtime
3. Or use environment variables
4. Never hardcode in scripts

Example secure loading:
```csharp
private void Start()
{
    // Load from secure source
    string apiKey = LoadAPIKeyFromSecureSource();
    gptService.SetAPIKey(apiKey);
}
```

---

## 📊 Example GPT Responses

**Input:** object="laptop", environment="office"
```json
{
  "spanishTranslation": "el portátil",
  "simpleSentenceSpanish": "El portátil es negro.",
  "simpleSentenceEnglish": "The laptop is black.",
  "positionalSentenceSpanish": "El portátil está sobre el escritorio.",
  "positionalSentenceEnglish": "The laptop is on the desk."
}
```

**Input:** object="book", environment="library"
```json
{
  "spanishTranslation": "el libro",
  "simpleSentenceSpanish": "El libro es grueso.",
  "simpleSentenceEnglish": "The book is thick.",
  "positionalSentenceSpanish": "El libro está en el estante.",
  "positionalSentenceEnglish": "The book is on the shelf."
}
```

---

## 🚀 Next Steps / Enhancements

1. **Cache Responses** - Save common translations locally
2. **Audio Playback** - Text-to-speech for pronunciation
3. **Multiple Languages** - Support French, German, etc.
4. **Difficulty Levels** - Simple vs complex sentences
5. **Quiz Mode** - Test user's learning
6. **Offline Mode** - Fallback to cached data

---

## 📞 Support Resources

- OpenAI API Docs: https://platform.openai.com/docs
- Unity Web Requests: https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html
- GPT Pricing: https://openai.com/api/pricing

---

## ✅ Quick Reference

### Key Files:
- `GPTLanguageService.cs` - API calls
- `MarkerUIController.cs` - UI display
- `DetectionManager.cs` - Integration
- `MarkerInfoUI.prefab` - UI prefab

### Key Methods:
```csharp
// Call from anywhere
gptService.GetLanguageData("laptop", "office", (response) => {
    Debug.Log(response.spanishTranslation);
});

// Initialize UI
uiController.Initialize(objectName, environment);
```

Good luck! 🎉
