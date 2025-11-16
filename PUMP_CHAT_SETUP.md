# Pump.fun Chat Integration Setup

This bridge connects pump.fun token chat rooms to your Unity project's HTTP API, allowing pump.fun chat messages to be used as topic/vote suggestions.

## Installation

1. Install the pump-chat-client package:
```bash
npm install
```

## Configuration with Static Credentials

### Step 1: Create Credentials File

Create or edit `pump-fun-credentials.json` with your static credentials:

```json
{
  "pumpFun": {
    "ticker": "YOUR_TOKEN_ADDRESS_HERE",
    "rtmpUrl": "YOUR_RTMP_SERVER_URL_HERE",
    "rtmpKey": "YOUR_RTMP_STREAM_KEY_HERE"
  }
}
```

### Step 2: Get Your Credentials

**Ticker (Token Address):**
- The token address is the Solana address of your token
- Found in the URL: `https://pump.fun/7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU`
- Ticker: `7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU`

**RTMP URL and Key:**
1. Log into [pump.fun](https://pump.fun)
2. Navigate to your token page
3. Go to streaming settings
4. Copy your RTMP Server URL and Stream Key

### Step 3: View Streaming Configuration

Run the stream config helper:
```bash
npm run pump-stream-config
```

This will display your RTMP settings for OBS/streaming software.

## Running the Bridge

The bridge will automatically load credentials from `pump-fun-credentials.json`:

```bash
npm run pump-chat
```

Or directly:
```bash
node pump-chat-bridge.js
```

The bridge will:
- Load token address from credentials file
- Connect to pump.fun chat using static credentials
- Listen for messages starting with `topic:` or `vote:`
- Forward those messages to Unity's HTTP API at `http://localhost:9999/`

## Message Format

Messages in pump.fun chat that start with:
- `topic: <your prompt>` - Will be added to topic suggestions
- `vote: <vote>` - Will be added to vote suggestions

Example messages in pump.fun chat:
```
topic: Rick and Morty explore a Solana validator node
vote: 1
```

## Streaming to Pump.fun

To stream your Unity content to pump.fun:

1. **Configure OBS/Streaming Software:**
   - Run `npm run pump-stream-config` to see your RTMP settings
   - Copy the Server URL and Stream Key
   - In OBS: Settings → Stream → Service: Custom
   - Paste the Server URL and Stream Key

2. **Start Streaming:**
   - Your stream will appear on `https://pump.fun/YOUR_TICKER`
   - Chat messages from viewers will be forwarded to Unity automatically

## Integration with Unity

The Unity project's `YouTubeChatFromSteven.cs` script will automatically receive these messages via the HTTP API endpoint, just like YouTube chat messages.

## Troubleshooting

- **No messages forwarded**: Make sure messages start with `topic:` or `vote:`
- **Connection errors**: Verify the ticker (token address) is correct in `pump-fun-credentials.json`
- **Credentials not loading**: Check that `pump-fun-credentials.json` exists and is valid JSON
- **Unity not receiving**: Ensure Unity is running and `YouTubeChatFromSteven` has `usingYoutubeChatStuff = true`
- **Streaming issues**: Verify RTMP credentials are correct and your token supports streaming

## Based On

- [pump-chat-client](https://github.com/CodingButter/pump-chat-client) - WebSocket client for pump.fun chat rooms

