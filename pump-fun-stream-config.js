// Pump.fun Streaming Configuration
// This script helps configure OBS/streaming software to stream to pump.fun
// Run this to get your RTMP settings

const fs = require('fs');
const path = require('path');

const CREDENTIALS_FILE = path.join(__dirname, 'pump-fun-credentials.json');

function loadCredentials() {
  try {
    if (fs.existsSync(CREDENTIALS_FILE)) {
      const credentialsData = fs.readFileSync(CREDENTIALS_FILE, 'utf8');
      return JSON.parse(credentialsData);
    }
    return null;
  } catch (error) {
    console.error('Error loading credentials:', error.message);
    return null;
  }
}

function displayStreamingInfo() {
  const credentials = loadCredentials();
  
  if (!credentials || !credentials.pumpFun) {
    console.log('❌ No credentials found in pump-fun-credentials.json');
    console.log('\nTo set up streaming:');
    console.log('1. Log into pump.fun');
    console.log('2. Navigate to your token page');
    console.log('3. Get your RTMP Server URL and Stream Key');
    console.log('4. Add "ticker", "rtmpUrl", and "rtmpKey" to pump-fun-credentials.json');
    return;
  }

  const ticker = credentials.pumpFun.ticker;
  const rtmpUrl = credentials.pumpFun.rtmpUrl;
  const rtmpKey = credentials.pumpFun.rtmpKey;

  if (!ticker || !rtmpUrl || !rtmpKey) {
    console.log('❌ Missing credentials in pump-fun-credentials.json');
    console.log('Required fields:');
    console.log('  - ticker: Your token address');
    console.log('  - rtmpUrl: Your RTMP server URL');
    console.log('  - rtmpKey: Your RTMP stream key');
    return;
  }

  console.log('📺 Pump.fun Streaming Configuration');
  console.log('=====================================\n');
  console.log('Token Address (Ticker):', ticker);
  console.log('\nOBS Studio / Streaming Software Settings:');
  console.log('-------------------------------------------');
  console.log('Service: Custom');
  console.log('Server:', rtmpUrl);
  console.log('Stream Key:', rtmpKey);
  console.log('\nYour stream will appear on:');
  console.log(`https://pump.fun/${ticker}`);
  console.log('\n📝 Copy the Server URL and Stream Key into your streaming software (OBS, Streamlabs, etc.)');
}

// Export for use in other scripts
module.exports = { loadCredentials, displayStreamingInfo };

// Run if called directly
if (require.main === module) {
  displayStreamingInfo();
}

