// Pump.fun Chat Bridge
// Connects to pump.fun token chat rooms and forwards messages to Unity HTTP API
// Based on: https://github.com/CodingButter/pump-chat-client

const { PumpChatClient } = require('pump-chat-client');
const fs = require('fs');
const path = require('path');

// Load credentials from JSON file
let credentials = null;
const CREDENTIALS_FILE = path.join(__dirname, 'pump-fun-credentials.json');

try {
  if (fs.existsSync(CREDENTIALS_FILE)) {
    const credentialsData = fs.readFileSync(CREDENTIALS_FILE, 'utf8');
    credentials = JSON.parse(credentialsData);
  } else {
    console.error('❌ Credentials file not found. Create pump-fun-credentials.json');
    process.exit(1);
  }
} catch (error) {
  console.error('❌ Error loading credentials:', error.message);
  process.exit(1);
}

// Configuration
const UNITY_API_PORT = 9999; // Unity HTTP API port
const UNITY_API_URL = `http://localhost:${UNITY_API_PORT}/`;

// Get token address (ticker) from credentials
const TOKEN_ADDRESS = credentials?.pumpFun?.ticker || '';

// If you want to monitor a single token, set this instead:
const SINGLE_TOKEN_ADDRESS = TOKEN_ADDRESS || ''; // Uses credentials file

// Message filtering - only forward messages that start with "prompt:"
const FORWARD_PREFIXES = ['prompt:']; // Only forward messages starting with "prompt:"

// Track processed message IDs to avoid duplicates
const processedMessageIds = new Set();
const MAX_PROCESSED_IDS = 10000; // Prevent memory leak

// Create clients for each token address
const clients = [];

// Function to forward messages to Unity API
async function forwardToUnity(messages) {
  if (messages.length === 0) return;

  try {
    const response = await fetch(UNITY_API_URL, {
      method: 'POST',
      body: JSON.stringify(messages),
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      // Silent error - Unity might not be running
    }
  } catch (error) {
    // Silent error - Unity might not be running
  }
}

// Function to parse and clean the prompt text
function parsePrompt(messageText) {
  if (!messageText) return null;
  
  let cleaned = messageText.trim();
  
  // Must start with "prompt:" (case insensitive)
  if (!cleaned.toLowerCase().startsWith('prompt:')) {
    return null; // Not a prompt message
  }
  
  // Remove "prompt:" prefix
  cleaned = cleaned.substring(7).trim();
  
  // Remove quotes if the entire message is wrapped in them
  if ((cleaned.startsWith('"') && cleaned.endsWith('"')) || 
      (cleaned.startsWith("'") && cleaned.endsWith("'"))) {
    cleaned = cleaned.slice(1, -1).trim();
  }
  
  // Remove extra whitespace and normalize
  cleaned = cleaned.replace(/\s+/g, ' ').trim();
  
  // Validate minimum length (after removing "prompt:")
  if (cleaned.length < 3) {
    return null;
  }
  
  return cleaned;
}

// Function to filter and format messages
function processMessage(message) {
  // Skip if already processed
  if (message.id && processedMessageIds.has(message.id)) {
    return null;
  }

  // Add to processed set
  processedMessageIds.add(message.id);

  // Clean up old IDs to prevent memory leak
  if (processedMessageIds.size > MAX_PROCESSED_IDS) {
    const idsArray = Array.from(processedMessageIds);
    const toRemove = idsArray.slice(0, idsArray.length - MAX_PROCESSED_IDS);
    toRemove.forEach(id => processedMessageIds.delete(id));
  }

  // Get raw message text
  const rawMessageText = message.message || message.text || '';
  
  if (!rawMessageText || rawMessageText.trim().length === 0) {
    return null;
  }
  
  // Parse and clean the prompt (only accepts messages starting with "prompt:")
  const parsedPrompt = parsePrompt(rawMessageText);
  
  if (!parsedPrompt) {
    // Message doesn't start with "prompt:" or failed validation - silently skip
    return null;
  }

  // Get author
  const author = message.username || message.userAddress || 'anonymous';
  
  // Format message for Unity API (matches YouTubeChatFromSteven format)
  // Send the parsed/cleaned prompt (with "prompt:" prefix already removed)
  return {
    author: author,
    text: parsedPrompt
  };
}

// Function to setup a client for a token address
function setupClient(tokenAddress) {
  const client = new PumpChatClient({
    roomId: tokenAddress,
    username: 'unity-bridge',
    messageHistoryLimit: 0 // Don't load historical messages - only use new/live messages
  });

  // Event: Connected
  client.on('connected', () => {
    // Silent - only show prompts
  });

  // Helper function to format timestamp
  function formatTimestamp(timestamp) {
    if (!timestamp) return 'N/A';
    try {
      const date = new Date(timestamp);
      return date.toLocaleString();
    } catch (e) {
      return timestamp;
    }
  }

  // Event: New message received
  client.on('message', (message) => {
    const username = message.username || message.userAddress || 'anonymous';
    const rawMessageText = message.message || message.text || '';
    
    // Parse the prompt before displaying
    const parsedPrompt = parsePrompt(rawMessageText);
    
    if (parsedPrompt) {
      // Display in format: prompt: '$prompt' by username
      console.log(`prompt: '${parsedPrompt}' by ${username}`);
      
      const processed = processMessage(message);
      if (processed) {
        forwardToUnity([processed]);
      }
    } else {
      // Silently skip messages that don't start with "prompt:"
    }
  });

  // Event: Message history received
  client.on('messageHistory', (messages) => {
    // Don't forward historical messages - only process new messages as they come in
    // This ensures we only use recent/live prompts, not old ones from the past
  });

  // Event: Error
  client.on('error', (error) => {
    // Silent - only show prompts
  });

  // Event: Server error
  client.on('serverError', (error) => {
    // Silent - only show prompts
  });

  // Event: Disconnected
  client.on('disconnected', () => {
    // Silent - only show prompts
  });

  // Event: User left
  client.on('userLeft', (data) => {
    // Silent - only show prompts
  });

  // Event: Max reconnect attempts reached
  client.on('maxReconnectAttemptsReached', () => {
    // Silent - only show prompts
  });

  // Connect to the chat room
  client.connect();

  return client;
}

// Main entry point
async function main() {
  // Determine which tokens to monitor
  const tokensToMonitor = SINGLE_TOKEN_ADDRESS ? [SINGLE_TOKEN_ADDRESS] : [];

  if (tokensToMonitor.length === 0 || !tokensToMonitor[0]) {
    console.error('❌ No token address (ticker) configured!');
    process.exit(1);
  }

  // Setup client for the token
  const tokenAddress = tokensToMonitor[0].trim();
  const client = setupClient(tokenAddress);
  clients.push({ tokenAddress, client });

  // Graceful shutdown
  process.on('SIGINT', () => {
    clients.forEach(({ client }) => {
      if (client.isActive()) {
        client.disconnect();
      }
    });
    process.exit(0);
  });

  process.on('SIGTERM', () => {
    clients.forEach(({ client }) => {
      if (client.isActive()) {
        client.disconnect();
      }
    });
    process.exit(0);
  });
}

// Run the bridge
main().catch(error => {
  console.error('Fatal error:', error);
  process.exit(1);
});

