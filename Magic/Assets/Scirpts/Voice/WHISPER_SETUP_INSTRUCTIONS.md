# Whisper Unity VR Setup Instructions

## 🎤 Complete Offline Voice Recognition for Meta Quest

### Step 1: Install Whisper Unity Package

**Option A: Package Manager (Recommended)**
1. Open Window → Package Manager
2. Click "+" → Add package from git URL
3. Enter: `https://github.com/Macoron/whisper.unity.git`
4. Click "Add"

**Option B: Manual Installation**
1. Download from: https://github.com/Macoron/whisper.unity
2. Extract to Assets/Plugins/WhisperUnity/
3. Import all files

### Step 2: Download Whisper Model

1. Create folder: `Assets/StreamingAssets/Whisper/`
2. Download tiny model (recommended for Quest): 
   - https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin
3. Place file as: `Assets/StreamingAssets/Whisper/ggml-tiny.en.bin`

**Model Size Options:**
- **ggml-tiny.en.bin** (39 MB) - Fast, Quest optimized ⭐
- **ggml-base.en.bin** (142 MB) - Better accuracy
- **ggml-small.en.bin** (244 MB) - High accuracy (may be slow on Quest)

### Step 3: Configure Unity Settings

**Android Build Settings:**
- Target API Level: 29 or higher
- Scripting Backend: IL2CPP
- Target Architectures: ARM64 ✅
- Internet Permission: Not required! (Offline)

**XR Settings:**
- Initialize XR on Startup: ✅
- Oculus Android: ✅

**Player Settings:**
- Scripting Define Symbols: Add `WHISPER_UNITY`
- Strip Engine Code: ✅ (for performance)

### Step 4: Add VR Voice Components

**Required Components (Auto-created):**
1. `VRVoiceIntegrationManager` - Main controller
2. `WhisperVRManager` - STT processing
3. `OfflineVRTTSManager` - TTS for responses
4. `VoiceInputTester` - Keyboard fallback

**Manual Setup:**
1. Create empty GameObject named "VRVoiceSystem"
2. Add `VRVoiceIntegrationManager` component
3. Configure settings in inspector:
   - ✅ Enable Whisper STT
   - ✅ Enable Offline TTS
   - ✅ Optimize for Quest
   - ✅ Enable Controller Input

### Step 5: Configure Android Permissions

**AndroidManifest.xml Additions:**
```xml
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
```

### Step 6: Test Your Setup

**In Unity Editor:**
- Press Play
- Look for "VR Voice System Ready!" in console
- Use keyboard fallback (T to toggle, V to push-to-talk)
- Test GUI buttons on screen

**On Meta Quest:**
1. Build and install APK
2. Grant microphone permission when prompted
3. Put on headset
4. Use controller inputs:
   - **Right Primary Button**: Toggle listening
   - **Right Grip**: Push-to-talk
   - **Right Secondary Button**: Quick commands

## 🎮 VR Controls

**Controller Input:**
- **Primary Button (A/X)**: Toggle voice listening
- **Grip Button**: Push-to-talk mode
- **Secondary Button (B/Y)**: Quick commands
- **Trigger**: Context-sensitive commands

**Keyboard Fallback (Editor/Testing):**
- **T Key**: Toggle listening
- **V Key**: Push-to-talk
- **Space**: Legacy toggle (VoiceRecognitionManager)

## 🚨 Troubleshooting

### Common Issues

**"Whisper model not found":**
- ✅ Check path: `Assets/StreamingAssets/Whisper/ggml-tiny.en.bin`
- ✅ Verify file size is ~39 MB
- ✅ Ensure StreamingAssets folder is included in build

**"Microphone permission denied":**
- Go to Quest Settings → Apps → Your App → Permissions
- Enable Microphone access
- Restart the app

**"WHISPER_UNITY symbol not defined":**
- Go to Player Settings → Other Settings
- Add `WHISPER_UNITY` to Scripting Define Symbols
- Rebuild the project

**Performance issues on Quest:**
- ✅ Use ggml-tiny.en.bin model only
- ✅ Enable "Strip Engine Code" 
- ✅ Set Quality Level to "Fastest"
- ✅ Close other apps on Quest
- ✅ Ensure Quest has adequate battery/cooling

**Voice not recognized:**
- Speak clearly and loudly
- Hold device closer to mouth
- Check microphone permissions
- Try keyboard fallback first

### Advanced Troubleshooting

**Build Errors:**
```
IL2CPP error for method ... 
```
- Update Unity to 2022.3 LTS or higher
- Update XR Interaction Toolkit
- Clean and rebuild

**Memory Issues:**
- Reduce max concurrent inferences
- Enable adaptive quality
- Monitor performance metrics in GUI

## 🎯 Integration with Luna AI

The VR Voice System automatically integrates with your existing Luna AI:

**Automatic Integration:**
- Voice commands sent to `LunaNPCController`
- Events triggered via `VoiceRecognitionEvents`
- TTS responses from Luna character
- Maintains existing game flow

**Luna-specific Commands:**
- "Hello Luna" - Greeting
- "Help me" - Request assistance
- "Repeat" - Repeat last response
- "Stop listening" - Disable voice input

## ✅ System Ready!

When working correctly, you'll see:
- **Console**: "VR Voice System Ready!"
- **GUI**: Green checkmarks for all components
- **Audio**: "VR voice system is ready! Try saying hello!"
- **Performance**: Processing times under 2 seconds

## 📊 Performance Metrics

Monitor these in the GUI:
- **Processing Time**: < 2s for tiny model
- **Memory Usage**: < 100MB additional
- **CPU Usage**: < 30% on Quest 2
- **Commands Processed**: Running count

The system is optimized for Meta Quest 2/3 and provides:
- ✅ **Completely offline operation**
- ✅ **No internet required**
- ✅ **Quest-optimized performance**
- ✅ **Fallback input methods**
- ✅ **Luna AI integration**
- ✅ **Real-time voice processing**