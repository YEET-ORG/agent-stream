# Pump.fun Chat Unity Configuration

This guide explains how to configure Unity to receive and process pump.fun chat messages.

## Unity Setup

### 1. Scene Configuration

In your Unity scene, ensure you have:

1. **GameObject with `WholeThingManager` script**
   - This is the main controller that orchestrates everything

2. **GameObject with `YouTubeChatFromSteven` script**
   - This receives HTTP requests from the pump.fun chat bridge
   - **Important**: Set `usingYoutubeChatStuff = true` in the Inspector

### 2. Inspector Settings

#### YouTubeChatFromSteven Component

In the Unity Inspector, find the `YouTubeChatFromSteven` component and configure:

- **`usingYoutubeChatStuff`**: ✅ **Set to `true`** (enables the HTTP server)
- **`port`**: `9999` (default, matches the bridge)
- **`maxListSize`**: `1000` (maximum topic suggestions to store)

#### WholeThingManager Component

Ensure the `WholeThingManager` has:

- **`youTubeChat`**: Drag and drop the GameObject with `YouTubeChatFromSteven` script
- **`runMainLoop`**: ✅ Set to `true` (runs the main scene generation loop)
- **`waitForVoting`**: Configure as needed (if you want voting system)

### 3. How It Works

1. **Pump.fun Chat Bridge** (`pump-chat-bridge.js`) connects to pump.fun chat
2. Messages are forwarded to Unity's HTTP API at `http://localhost:9999/`
3. **`YouTubeChatFromSteven`** receives messages and adds them to `topicSuggestions` list
4. **`WholeThingManager`** reads from `topicSuggestions` and creates scenes

### 4. Message Processing

**All messages from pump.fun chat are automatically added as topic suggestions**, except:
- Messages starting with `vote:` are added to vote suggestions
- Messages that are blacklisted or already used are skipped

### 5. Testing

1. Start Unity and enter Play mode
2. Check the Console for: `"recieved: [message]"` when messages arrive
3. Verify `topicSuggestions` list is populated in the Inspector (if visible)
4. The `WholeThingManager` will automatically use these topics to generate scenes

### 6. Troubleshooting

**No messages received:**
- Verify `usingYoutubeChatStuff = true` in `YouTubeChatFromSteven`
- Check that the pump.fun bridge is running: `npm run pump-chat`
- Verify Unity Console for errors

**Messages received but not used:**
- Check that `WholeThingManager.youTubeChat` is assigned
- Verify `runMainLoop = true` in `WholeThingManager`
- Check Console for any errors in the main loop

**Port conflicts:**
- Default port is `9999`
- If changed, update both Unity (`YouTubeChatFromSteven.port`) and bridge (`UNITY_API_PORT` in `pump-chat-bridge.js`)

## Manual Input Mode with Pump.fun Chat

If you're using **Manual Input Mode** (`useManualInputMode = true`):

1. **Pump.fun chat messages are automatically used as prompts**
   - When a scene finishes, the system checks for new pump.fun chat messages
   - If a message is available, it's used immediately to generate the next scene
   - No manual input required if chat messages are available

2. **Fallback to manual input**
   - If no pump.fun chat messages are available, the manual input UI appears
   - You can type a prompt manually as a fallback

3. **Configuration**
   - `useManualInputMode = true` in `WholeThingManager`
   - `usingYoutubeChatStuff = true` in `YouTubeChatFromSteven`
   - `youTubeChat` assigned in `WholeThingManager`

## Current Configuration

The system is now configured to:
- ✅ Accept ALL pump.fun chat messages as topic suggestions
- ✅ Automatically use pump.fun chat messages in manual input mode
- ✅ Process votes (messages starting with "vote:") if voting mode is enabled
- ✅ Forward messages to Unity automatically
- ✅ Work with your token: `EE45Fkh6e5DjWMBS8joq3KF6feddNiDTeMnAyXdxpump`

