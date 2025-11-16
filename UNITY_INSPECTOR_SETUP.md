# Unity Inspector Setup Guide for Pump.fun Chat

This guide shows exactly what to configure in Unity's Inspector to get pump.fun chat working.

## Step-by-Step Inspector Configuration

### 1. Find Your GameObjects

In your Unity scene hierarchy, locate:
- **GameObject with `WholeThingManager` script** (usually the main controller)
- **GameObject with `YouTubeChatFromSteven` script** (handles HTTP API)

### 2. Configure `YouTubeChatFromSteven` Component

1. **Select the GameObject** with `YouTubeChatFromSteven` script
2. In the **Inspector** panel, find the `YouTubeChatFromSteven` component
3. Configure these settings:

```
✅ usingYoutubeChatStuff = true  (CHECK THIS BOX)
   port = 9999                    (default, should be 9999)
   maxListSize = 1000             (optional, default is fine)
```

**Important**: The `usingYoutubeChatStuff` checkbox MUST be checked (true) for the HTTP server to start.

### 3. Configure `WholeThingManager` Component

1. **Select the GameObject** with `WholeThingManager` script
2. In the **Inspector** panel, find the `WholeThingManager` component
3. Find the `youTubeChat` field (it's a public field)
4. **Drag and drop** the GameObject with `YouTubeChatFromSteven` script into this field
5. Configure these settings:

```
✅ useManualInputMode = true      (CHECK THIS if using manual input)
   runMainLoop = true              (CHECK THIS - runs the main loop)
   youTubeChat = [Drag YouTubeChatFromSteven GameObject here]
```

### 4. Verify the Connection

After setting up:

1. **Enter Play Mode** in Unity
2. Check the **Console** window
3. You should see: `"recieved: [message]"` when pump.fun messages arrive
4. If you see errors about port 9999, make sure nothing else is using that port

### 5. Testing

1. **Start the pump.fun bridge** in terminal:
   ```bash
   npm run pump-chat
   ```

2. **Start Unity** and enter Play Mode

3. **Send a test message** in pump.fun chat

4. **Check Unity Console** - you should see:
   ```
   recieved: [your message]
   Added topic: [your message] by [username]
   ```

5. **In manual input mode**, the message should automatically be used for the next scene

## Common Issues

### "No messages received"
- ✅ Check `usingYoutubeChatStuff = true` in `YouTubeChatFromSteven`
- ✅ Verify `youTubeChat` is assigned in `WholeThingManager`
- ✅ Make sure pump.fun bridge is running (`npm run pump-chat`)
- ✅ Check Unity Console for errors

### "Port already in use"
- Close other applications using port 9999
- Or change the port in both:
  - `YouTubeChatFromSteven.port` in Unity
  - `UNITY_API_PORT` in `pump-chat-bridge.js`

### "Messages received but not used"
- ✅ Check `runMainLoop = true` in `WholeThingManager`
- ✅ Verify `useManualInputMode = true` if using manual mode
- ✅ Check that `youTubeChat` field is properly assigned

## Quick Checklist

Before running:
- [ ] `YouTubeChatFromSteven.usingYoutubeChatStuff` = **true**
- [ ] `WholeThingManager.youTubeChat` = **assigned** (drag GameObject)
- [ ] `WholeThingManager.runMainLoop` = **true**
- [ ] `WholeThingManager.useManualInputMode` = **true** (if using manual mode)
- [ ] Pump.fun bridge is running (`npm run pump-chat`)
- [ ] Unity is in Play Mode

## Visual Guide

```
Unity Hierarchy:
├── MainController (GameObject)
│   └── WholeThingManager (Script)
│       ├── youTubeChat → [Drag ChatManager here]
│       ├── useManualInputMode ✓
│       └── runMainLoop ✓
│
└── ChatManager (GameObject)
    └── YouTubeChatFromSteven (Script)
        ├── usingYoutubeChatStuff ✓
        └── port = 9999
```

That's it! Once configured, pump.fun chat messages will automatically flow into Unity.

