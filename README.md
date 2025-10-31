## agentic-stream

agentic-stream is a Unity (Unity 6) project that generates character-driven scenes with AI-assisted dialogue and high-quality Text-to-Speech (TTS). It integrates a modern TTS backend, proxy support, Cinemachine camera work, TextMesh Pro UI, and a scene director to orchestrate multi-character performances.

### Highlights
- **Cuai TTS integration** via `CuaiTTSAPIManager.cs` (concurrent requests, automatic queuing)
- **Configurable concurrency**: up to 5 parallel TTS requests, excess are queued
- **Proxy support**: per-request proxy rotation with credentials
- **Scene orchestration**: `SceneDirector` and `WholeThingManager` coordinate characters and playback
- **Unity-native AudioClips**: WAV bytes → `AudioClip` conversion with cleanup

---

### Requirements
- **Unity**: 6000.2.6f2 (Unity 6)
- **Platform**: macOS/Windows (project developed on macOS)
- **Packages**: TextMesh Pro, Cinemachine (bundled with the project)

---

### Project Structure (selected)
- `Assets/Scripts/CuaiTTSAPIManager.cs` — TTS pipeline, concurrency, proxy handling
- `Assets/Scripts/WholeThingManager.cs` — high-level flow: dialogue → TTS → playback
- `Assets/` — scenes, audio, materials, prefabs, third-party assets
- `Packages/` and `ProjectSettings/` — Unity configuration

---

### Getting Started
1. Open Unity Hub → Add the project folder `/Users/aditya/SolStream` → Open with Unity 6000.2.6f2
2. Let Unity import assets and resolve packages
3. Open a sample scene under `Assets/Scenes/` (e.g., a nightclub or office scene)
4. Press Play to run a demo; or wire your own flow via `WholeThingManager`

---

### TTS Configuration
The TTS integration is handled by `CuaiTTSAPIManager`.

Key constants (see `Assets/Scripts/CuaiTTSAPIManager.cs`):
- API base URL: `https://audio.yeetlabs.fun`
- Retries: `c_MaxRetryAttempts = 5`
- Timeout: `c_RequestTimeoutSeconds = 60` seconds
- Max concurrency: `c_MaxParallelRequests = 5` (excess requests are queued automatically)

Inspector fields:
- `usingProxies` (bool): enable per-request proxy rotation
- `proxyTextFile` (TextAsset): newline-delimited proxies
- `sceneDirector` (SceneDirector): character list and defaults
- `defaultSound` (AudioClip): fallback sound per line until TTS arrives
- `statisText` (TMP_Text): progress display (optional)

Proxy file format (`proxyTextFile`): one per line
```
host:port:username:password
```

Text normalization:
- Unicode quotes/dashes/ellipsis are normalized to ASCII
- Control characters are removed; text is trimmed
- If input has fewer than 3 non-space chars, it is padded to meet TTS service requirements

---

### Using the API in Code
`CuaiTTSAPIManager.GenerateTTS(...)` accepts parallel lists of lines, voice models, and character names, updating a UI text as it progresses. Example:

```csharp
// Assuming you have a reference to CuaiTTSAPIManager as cuai
var lines = new List<string> { "Hello there!", "We are live." };
var voices = new List<string> { "rick", "morty" }; // service voice names
var characters = new List<string> { "Rick", "Morty" }; // must match SceneDirector character names

TMP_Text statusLabel = /* assign a TextMeshProUGUI */ null;
string statusPrefix = "Dialogue";

var audioClips = await cuai.GenerateTTS(lines, voices, characters, statusLabel, statusPrefix);

// audioClips[i] will be an AudioClip for lines[i] (defaultSound if generation failed)
```

Notes:
- Concurrency is automatically limited to 5; you do not need to batch your calls
- If proxies are configured, each request rotates to the next proxy
- The API currently returns WAV bytes directly; clips are loaded via `UnityWebRequestMultimedia.GetAudioClip`

---

### Scenes and Playback
- `SceneDirector` retains a `characterList` with per-character `defaultSound`
- `WholeThingManager` coordinates when to request TTS and when to play clips
- Camera behaviors (e.g., Cinemachine) and VFX are configured per scene

---

### Building
1. File → Build Settings
2. Choose platform (macOS/Windows)
3. Add your main scene(s)
4. Build

Troubleshooting builds:
- If TextMesh Pro prompts to import essentials, accept it
- Clear `Library/` if stuck on import (re-import on next open)

---

### Troubleshooting
- **No audio or short sounds**: Ensure the service returned non-empty data; check Console warnings
- **Timeouts**: Increase `c_RequestTimeoutSeconds` or verify connectivity
- **Proxy failures**: Validate `proxyTextFile` lines, format, and credentials; try disabling proxies
- **Character voice mismatch**: Ensure `characterName` entries match `SceneDirector.characterList` names
- **Rate limiting**: Concurrency is capped at 5; server-side rate limiting may still apply

---

### Contributing
PRs are welcome. Please favor readable code with descriptive names and minimal deep nesting. Keep platform-specific logic isolated where possible.

---

### License
TBD. If unspecified, treat as all rights reserved by the repository owner until clarified.

---

### Acknowledgements
- Cuai TTS backend
- Unity, TextMesh Pro, Cinemachine, and included third-party assets in `Assets/ThirdPartyCode`
