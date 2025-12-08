# Writer UI Setup Guide

## 📋 Overview
The Writer UI appears after double-clicking back button on Marker UI. It shows:
1. Fixed text box with practice prompt
2. Pronunciation text box

## 🎯 Flow:
1. Click marker → Marker UI appears (with GPT data)
2. Double-click back → Marker UI closes, Writer UI opens
3. Double-click back again → Writer UI closes, markers reappear

---

## 🛠️ Create Writer UI Prefab

### **1. Create Canvas** (5 minutes)

1. GameObject → UI → Canvas
2. Name: "WriterUI"
3. Set Canvas properties:
   - Render Mode: **World Space**
   - Width: 400
   - Height: 250
   - Scale: 0.001, 0.001, 0.001

### **2. Add Background Panel**

1. Right-click Canvas → UI → Panel
2. Name: "Background"
3. Set color (e.g., dark blue/purple for differentiation from Marker UI)

### **3. Add Text Elements**

**Fixed Text (Practice Prompt):**
```
Name: txt_FixedText
Font Size: 24
Alignment: Center Top
Text: "Practice writing:\nel portátil"
Position: (0, 60, 0)
Width: 350
Height: 100
Color: White
```

**Pronunciation Text:**
```
Name: txt_Pronunciation
Font Size: 20
Alignment: Center
Text: "Pronunciation: el por-tah-til"
Position: (0, -20, 0)
Width: 350
Color: Light Yellow
```

### **4. Add Close Button (Optional)**

1. Right-click Canvas → UI → Button - TextMeshPro
2. Name: "btn_Close"
3. Position: Top right (150, 80, 0)
4. Text: "✕"
5. **Add BoxCollider** to button

### **5. Add WriterUIController**

1. Select Canvas GameObject
2. Add Component → `WriterUIController`
3. **Assign References:**
   ```
   Fixed Text: txt_FixedText
   Pronunciation Text: txt_Pronunciation
   Close Button: btn_Close (if added)
   ```

### **6. Save as Prefab**

1. Drag "WriterUI" from Hierarchy → Assets/Prefabs/
2. Delete from scene

---

## 🔗 Connect to DetectionManager

1. Select DetectionManager in scene
2. Find "Marker Interaction" section
3. **Assign Writer UI:**
   ```
   Writer UI Prefab: [drag WriterUI prefab here]
   ```

---

## ✅ Testing

1. Build and run on Quest
2. Spawn markers
3. Click marker → Marker UI shows
4. **Double-click back button** → Writer UI appears
5. **Double-click back button again** → Writer UI closes, markers show

---

## 🎨 Customization Ideas

**For pronunciation text, you could:**
- Add syllable breakdown
- Show phonetic spelling
- Add audio playback button
- Show stress patterns

**For fixed text, you could:**
- Add writing practice area
- Show example sentences
- Add difficulty levels

---

## 📝 Current Behavior

**Fixed Text shows:**
```
Practice writing:
el portátil
```

**Pronunciation shows:**
```
Pronunciation: el portátil
```

You can customize these in `WriterUIController.Initialize()` method!

---

## 🔄 Flow Summary

```
Marker (hidden) → [click] → Marker UI
                              ↓
                         [double-click back]
                              ↓
                          Writer UI
                              ↓
                         [double-click back]
                              ↓
                    All markers reappear
```

---

Good luck! Let me know if you need help customizing the Writer UI content.
