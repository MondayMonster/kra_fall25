# Khoury Research Apparenticeship Fall 2025 - Spanish MR Language Learning 

A mixed reality Spanish language learning prototype that combines spatial computing, AI-driven translation, and multimodal interaction using the Meta Quest 3 and MX Ink stylus.

## Overview

Point at objects in your environment, hear Spanish translations with AI-generated sentences, practice pronunciation, and draw words in 3D space. The app uses Unity Sentis for on-device object detection and OpenAI APIs (GPT-4o-mini, TTS, Whisper) for contextual language learning.

**Key Features:**
- Real-time object detection with YOLO v8 (Unity Sentis)
- AI-generated bilingual content (Spanish/English)
- Voice pronunciation practice with speech recognition
- 3D spatial writing with MX Ink stylus
- Six voices and adjustable speech speed

## Level Setup

### Prerequisites
- Unity 2023.2.5f1 or later
- Meta Quest 3 headset
- MX Ink stylus
- OpenAI API key

### Unity Project Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/MondayMonster/kra_fall25.git
   cd KRA_Fall2025
   ```

2. **Open in Unity 2023.2.5f1+**

3. **Configure OpenAI API Keys:**
   - Locate `GPTLanguageService` GameObject in the scene
   - Add your OpenAI API key to the Inspector
   - Locate `TTSManager` GameObject
   - Add the same API key
   - Locate `STTManager` GameObject  
   - Add the same API key


## Getting It Working on Quest 3

### Build Settings

1. **Switch to Android Platform:**
   - File → Build Settings → Android → Switch Platform

2. **Configure Player Settings:**
   - Edit → Project Settings → Player
   - Minimum API Level: Android 10.0 (API 29)
   - Target API Level: Android 13.0 (API 33)

3. **Build and Deploy:**
   - Connect Quest 3 via USB-C
   - File → Build Settings → Build and Run

### Runtime Setup

1. Enable Passthrough mode
2. Look around to detect objects (tables, chairs, bottles, etc.)
3. Point MX Ink stylus at detected markers
4. Press front button to start learning workflow
5. Follow the three-step UI: Marker → Speaker → Writer

### Controls

- **MX Ink Front Button**: Select markers, click UI buttons
- **MX Ink Middle Button**: Draw in 3D (Writer UI)

## Important Links

### Documentation
- [Unity Sentis Documentation](https://docs.unity3d.com/Packages/com.unity.sentis@latest)
- [Meta Quest 3 Developer Hub](https://developer.oculus.com/quest-3/)
- [OpenXR Unity Plugin](https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest)
- [MX Ink Stylus SDK](https://developer.oculus.com/documentation/unity/unity-mx-ink/)

### APIs Used
- [OpenAI GPT-4o-mini API](https://platform.openai.com/docs/models/gpt-4o-mini) - Translation
- [OpenAI TTS API](https://platform.openai.com/docs/guides/text-to-speech) - Spanish voice
- [OpenAI Whisper API](https://platform.openai.com/docs/guides/speech-to-text) - Speech recognition



