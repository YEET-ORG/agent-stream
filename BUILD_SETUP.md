# Building Unity Project with Pump.fun Chat Integration

This guide explains how to build your Unity project with pump.fun chat integration enabled.

## Build Requirements

The pump.fun chat integration uses HTTP networking which works in Unity builds. No special build settings are required, but follow these steps:

## Pre-Build Checklist

### 1. Unity Inspector Settings (Same as Editor)

Before building, ensure in Unity Editor:

- ✅ `YouTubeChatFromSteven.usingYoutubeChatStuff` = **true**
- ✅ `WholeThingManager.youTubeChat` = **assigned**
- ✅ `WholeThingManager.runMainLoop` = **true**
- ✅ `WholeThingManager.useManualInputMode` = **true** (if using manual mode)

### 2. Build Settings

1. **File → Build Settings**
2. Select your target platform (Windows/Mac/Linux)
3. **Player Settings** → **Other Settings**:
   - ✅ **Scripting Backend**: IL2CPP or Mono (both work)
   - ✅ **API Compatibility Level**: .NET Standard 2.1 or .NET Framework
   - ✅ **Allow 'unsafe' Code**: Not required, but can be enabled if needed

### 3. Platform-Specific Notes

#### Windows Build
- No special requirements
- HTTP server on `localhost:9999` works normally
- Make sure Windows Firewall allows the application

#### macOS Build
- May need to allow network access in System Preferences → Security & Privacy
- HTTP server on `localhost:9999` works normally

#### Linux Build
- No special requirements
- HTTP server on `localhost:9999` works normally

## Building the Project

1. **File → Build Settings**
2. Click **Build** (or **Build and Run**)
3. Choose output directory
4. Wait for build to complete

## Running the Build

### Step 1: Start the Pump.fun Bridge

**Before** launching your Unity build, start the bridge:

```bash
npm run pump-chat
```

The bridge must be running for chat messages to flow into Unity.

### Step 2: Launch Your Unity Build

1. Run the built executable
2. The HTTP server will start automatically on port 9999
3. Check the console/logs to verify:
   - HTTP server started
   - Receiving messages from bridge

### Step 3: Verify Connection

1. Send a test message in pump.fun chat
2. Check Unity build logs/console
3. You should see: `"recieved: [message]"`

## Build Configuration Tips

### For Distribution

If distributing your build:

1. **Include instructions** for users to:
   - Install Node.js
   - Run `npm install` in the project directory
   - Start the bridge: `npm run pump-chat`
   - Then launch the Unity build

2. **Or bundle the bridge**:
   - Include the `pump-chat-bridge.js` file
   - Include `package.json`
   - Include `pump-fun-credentials.json` (users need to fill in their credentials)
   - Provide instructions to run `npm install` and `npm run pump-chat`

### Logging in Builds

To see debug logs in builds:

1. **Development Build**:
   - In Build Settings, check **Development Build**
   - This enables console logging
   - Use **Deep Profiling** if needed for debugging

2. **Log Files**:
   - Windows: Check `%USERPROFILE%\AppData\LocalLow\[CompanyName]\[ProductName]\Player.log`
   - macOS: Check `~/Library/Logs/[CompanyName]/[ProductName]/Player.log`
   - Linux: Check `~/.config/unity3d/[CompanyName]/[ProductName]/Player.log`

## Troubleshooting Build Issues

### "Port 9999 already in use"
- Make sure the pump.fun bridge is running
- Close other applications using port 9999
- Or change the port in both Unity and bridge

### "No messages received in build"
- Verify bridge is running: `npm run pump-chat`
- Check Unity build logs for HTTP server errors
- Verify `usingYoutubeChatStuff = true` was set before building

### "HTTP server not starting"
- Check Windows Firewall settings
- Verify port 9999 is not blocked
- Check build logs for specific error messages

### "Messages received but scenes not generating"
- Verify `runMainLoop = true` was set before building
- Check that `youTubeChat` was assigned before building
- Review build logs for errors in MainLoop

## Testing Before Building

Always test in Unity Editor first:

1. ✅ Start pump.fun bridge
2. ✅ Enter Play Mode in Unity
3. ✅ Send test message in pump.fun chat
4. ✅ Verify message received and scene generated
5. ✅ Then build

## Build Script Example

You can create a simple batch/shell script to start everything:

**Windows (`start-build.bat`)**:
```batch
@echo off
echo Starting pump.fun bridge...
start cmd /k "npm run pump-chat"
timeout /t 3
echo Starting Unity build...
start YourUnityBuild.exe
```

**macOS/Linux (`start-build.sh`)**:
```bash
#!/bin/bash
echo "Starting pump.fun bridge..."
npm run pump-chat &
sleep 3
echo "Starting Unity build..."
./YourUnityBuild &
```

## Notes

- The HTTP server runs on `localhost:9999` - this works in builds
- No internet connection required (bridge handles pump.fun connection)
- Bridge and Unity build must run on the same machine
- Both can run simultaneously without conflicts

## Current Configuration

Your build will use:
- Token: `EE45Fkh6e5DjWMBS8joq3KF6feddNiDTeMnAyXdxpump`
- Port: `9999`
- Manual input mode with automatic chat message usage

