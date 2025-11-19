# Prefab (and Script) Reference

These prefabs can be found in `Packages/Embardiment/Runtime/Prefabs`.


## `Android ASR` (Automatic Speech Recognition)

**Location:** `Prefabs/Android ASR.prefab` <br />
**Script:** `Scripts/AndroidAsr.cs`

### Fields

* `OnComplete` **UnityEvent&lt;string&gt;** -- an event that fires upon completion of transcription
* `RecentTranscription` **string** -- most recent transcription of what was said

### Functions

* **void** `OpenRecognitionStream()` -- Start a recording for transcriptions.  When the recognition stream detects a pause in the speaker's utterance, this function populates `RecentTranscription` and invokes `OnComplete`
* **void** `OpenRecognitionStream(Action<string> invocationCallback)` -- Same as above, and calls `invocationCallback` just before class's generic `OnComplete`




## `Android LLM` (Large Language Model)

This prefab must be run on an Android device enrolled in Gemini Nano Experimental Access.  See [these instructions](https://developer.android.com/ai/gemini-nano/experimental) for more information.  *Note* - upon first run, AICore needs a few minutes to download the model.  You can run `adb logcat | grep -i "aicore"` to observe the download.

**Location:** `Prefabs/Android LLM.prefab` <br />
**Script:** `Scripts/AndroidLlm.cs`

### Fields

* `MaxOutputTokens` **int** -- maximum number of tokens to generate in response (default of 256)
* `OnComplete` **UnityEvent&lt;string&gt;** -- an event that fires upon completion of LLM response
* `RecentGeneratedText` **string** -- string of the last response from the LLM
* `SourcePrompt` **string** -- prompt for LLM generation
* `Temperature` **float** -- randomness: 1.0 being more random/ creative and 0.0 being more deterministic (default of 0.5f)
* `TopK` **int** -- maximum number of tokens to consider for the next token (default of 16)

### Functions

* **void** `SendPrompt()` Sends out the LLM post based on `SourcePrompt`.  Upon completion populates `RecentGeneratedText` and invokes `OnComplete`
* **void** `SendPrompt(string newSourcePrompt)` updates `SourcePrompt` and runs `SendPrompt()`
* **void** `SendPrompt(string newSourcePrompt, Action<string> invocationCallback)` same as above, and calls `invocationCallback` just before the class's generic `OnComplete`



## `Android OCR` (Optical Character Recognition)

**Location:** `Prefabs/Android OCR.prefab` <br />
**Script:** `Scripts/AndroidOcr.cs`

### Fields

* `OnComplete` **UnityEvent&lt;AndroidOCR.OcrResponse&gt;** -- fires upon completion of OCR.  See `RecentOcrResult` below to learn about `OcrResponse` struct.
* `RecentOcrResult` **AndroidOCR.OcrResponse** -- the result of the most recently resolved `RecognizeText()` call, including:
  * `RecentOcrResult.FullText` -- **string** -- the full concatenated text
  * `RecentOcrResult.TextBlocks` -- **TextBlock[]** -- an array of the text blocks.  Each `TextBlock` member has the following fields:
    * `BoundingBox` -- a struct with 4 **int**s: `Width`, `Height`, `X`, and `Y`
    * `Text` **string** -- text inside block
    * `Lines` **Line[]** -- an array of line blocks.  Each `Line` member has the following fields:
      * `BoundingBox` -- a struct with 4 **int**s: `Width`, `Height`, `X`, and `Y`
      * `Text` **string** -- text inside line
      * `Elements` **Element[]** -- an array of element blocks.  Each `Element` member has the following fields:
        * `BoundingBox` -- a struct with 4 **int**s: `Width`, `Height`, `X`, and `Y`
        * `Text` **string** -- text inside element
* `SourceTexture` **Texture2D** -- texture to perform character recognition against in the next `RecognizeText()` request

### Functions

* **void** `RecognizeText()` -- finds text from `SourceTexture`.  Upon completion invokes `OnComplete` and populates `RecentOcrResult`
* **void** `RecognizeText(Texture2D newSourceTexture)` -- updates `SourceTexture` and calls `RecognizeText()`
* **void** `RecognizeText(Texture2D newSourceTexture, Action<AndroidOCR.OcrResponse> invocationCallback)` --  Same as above, and calls `invocationCallback` just before the class's generic `OnComplete`




## `Android TTS` (Text-to-speech)

**Location:** `Prefabs/Android TTS.prefab` <br />
**Main Script:** `Scripts/AndroidTTS.cs`

### Fields

* `IsSpeaking` **bool** -- whether or not speech is still playing
* `Language` **string** -- locale code to identify language being spoken.  If blank, is set to device's default language.
* `OnSpeechGenerated` **UnityEvent** -- an event that fires upon completion of audio generation
* `OnDoneTalking` **UnityEvent** -- an event that fires upon completion of audio playback
* `Pitch` **float** -- pitch of voice, with 1 being default and higher values being higher pitch.
* `Speed` **float** -- speed of voice, with 1 being default and higher values being faster.
* `SourceText` **string** -- text to be converted into audio for the next `Speak()` request
* `VoiceIndex` **int** -- index of voice, as retrieved from the `GetVoiceList()` list.  If -1 (default value) or null, is set system default voice

### Functions

* **string[]** `GetVoiceList()` retrieve a list of voices from the service
* **void** `Speak()` speaks `SourceText`
* **void** `Speak(string newSourceText)` updates `SourceText` and speaks it
* **void** `Stop()` stop currently speaking audio



## `Desktop OCR` (Optical Character Recognition)


**Location:** `Prefabs/Desktop OCR.prefab` <br />
**Script:** `Scripts/DesktopOcr.cs`

### Fields

* `OnComplete` **UnityEvent&lt;DesktopOCR.OcrResponse&gt;** -- fires upon completion of OCR.  See `RecentOcrResult` below to learn about `OcrRepsonse` struct.
* `RecentOcrResult` **DesktopOCR.OcrResponse** -- the result of the most recently resolved `RecognizeText()` call, including:
  * `RecentOcrResult.FullText` -- **string** -- the full concatenated text
  * `RecentOcrResult.WordBoxes` -- **WordBox[]** -- an array of the word locations.  Each `WordBox` member has the following fields:
    * `X` -- **int** -- pixel x position
    * `Y` -- **int** -- pixel y position
    * `Width` -- **int** -- width in pixels
    * `Height` -- **int** -- height in pixels
    * `Word` -- **string** -- word inside box
* `SourceTexture` **Texture2D** -- texture to perform character recognition against in the next `RecognizeText()` request
* `UseCache` **bool** -- cache OCR results so that future OCR processing of the same textures goes faster

### Functions

* **void** `ClearCache()` -- clears the OCR cache
* **void** `RecognizeText()` -- finds text from `SourceTexture`, consulting cache if present.  Upon completion invokes `OnComplete` and populates `RecentOcrResult`
* **void** `RecognizeText(Texture2D newSourceTexture)` -- updates `SourceTexture` and calls `RecognizeText()`
* **void** `RecognizeText(Texture2D newSourceTexture, Action<DesktopOCR.OcrResponse> invocationCallback)` --  Same as above, and calls `invocationCallback` just before the class's generic `OnComplete`



## `Gemini ASR` (Automatic Speech Recognition)

Requires a ([Gemini API key](../README.md#gemini-api-key))

**Location:** `Prefabs/Gemini ASR.prefab` <br />
**Script:** `Scripts/GeminiAsr.cs`

### Fields

* `OnComplete` **UnityEvent&lt;string&gt;** -- an event that fires upon completion of transcription
* `RecentTranscription` **string** -- most recent transcription of what was said
* `SourceAudio` **AudioClip** -- Audio to process into text

### Functions

* **void** `RequestRecognition()` -- request a transcription of a `SourceAudio`.  Upon completion populates `RecentTranscription` and invokes `OnComplete`
* **void** `RequestRecognition(AudioClip newSourceAudio)` -- updates `SourceAudio` and runs `RequestRecognition()`
* **void** `RequestRecognition(AudioClip newSourceAudio, Action<string> invocationCallback)` -- Same as above, and calls `invocationCallback` just before the class's generic `OnComplete`
* **void** `StartRecording()` -- start a recording for transcription
* **void** `StopRecordingAndSend()` -- stops recording and saves audio to `SourceAudio`, then runs `RequestRecognition()`
* **void** `StopRecordingAndSend(Action<string> invocationCallback)` -- Same as above, and calls `invocationCallback` just before the class's generic `OnComplete`




## `Gemini LLM` (Large Language Model)

Requires a ([Gemini API key](../README.md#gemini-api-key))

**Location:** `Prefabs/Gemini LLM.prefab` <br />
**Script:** `Scripts/GeminiLlm.cs`

### Fields

* `OnComplete` **UnityEvent&lt;string&gt;** -- an event that fires upon completion of LLM response
* `RecentGeneratedText` **string** -- string of the last response from the LLM
* `SourcePrompt` **string** -- prompt for LLM generation
* `SystemInstruction` **string** -- system instruction prepended to each LLM request ([api documentation](https://ai.google.dev/gemini-api/docs/text-generation#system-instructions))
* `UseConversationHistory` **bool** -- save and post a conversation history so LLM sees chat history

### Functions

* **void** `SendPrompt()` Sends out the LLM post based on `SourcePrompt`.  Upon completion populates `RecentGeneratedText` and invokes `OnComplete`
* **void** `SendPrompt(string newSourcePrompt)` updates `SourcePrompt` and runs `SendPrompt()`
* **void** `SendPrompt(string newSourcePrompt, Action<string> invocationCallback)` same as above, and calls `invocationCallback` just before the class's generic `OnComplete`



## `Gemini TTS` (Text-to-speech)

Requires a ([Gemini API key](../README.md#gemini-api-key))

**Location:** `Prefabs/Gemini TTS.prefab` <br />
**Script:** `Scripts/GeminiTts.cs`

### Fields

* `OnDoneTalking` **UnityEvent** -- an event that fires upon completion of audio playback
* `OnSpeechGenerated` **UnityEvent&lt;AudioClip&gt;** -- an event that fires upon completion of audio generation
* `RecentAudioClip` **AudioClip** -- a reference to the last audio clip created
* `SourceText` **string** -- text to be converted into audio for the next `Speak()` request
* `VoiceName` **string** -- name of voice to use ([list of voices to choose from](https://ai.google.dev/gemini-api/docs/speech-generation#voices))


### Functions

* **void** `GenerateAudio()` Generate audio for `SourceText`.  Upon completion populates `RecentAudioClip` and invokes `OnSpeechGenerated`.  NOTE: only generates, does not play.
* **void** `GenerateAudio(string newSourceText)` update `SourceText` and runs GenerateAudio()
* **void** `GenerateAudio(string newSourceText, Action<AudioClip> invocationCallback)` same as above, and calls `invocationCallback` just before the class's generic `OnSpeechGenerated`
* **void** `Speak()` Runs the `GenerateAudio` function and immediately plays the returned audio clip
* **void** `Speak(string newSourceText)` updates `SourceText` and runs Speak()
* **void** `Speak(string newSourceText, Action<AudioClip> invocationCallback)` same as above, and calls `invocationCallback` just before the class's generic `OnSpeechGenerated`
* **void** `Stop()` stop currently speaking audio


# Pipelines

When the above prefabs are orchestrated in sequence, they can create powerful user experiences.  The following prefabs demonstrate a few examples of such pipelines.  They can be found in `Packages/Embardiment/Runtime/Prefabs/Pipelines`, and are also present in the [example Unity projects](../README.md#installation).

## `Camera OCR (Android)`

* Android OCR to detect text on an authenticated webcam feed

## `Document aware agent (Desktop + Gemini)`

* Desktop OCR to read 3 documents
* Gemini ASR to hear user speak
* Gemini LLM to respond to document / utterance combination

## `Document aware agent (Samsung XR)`

* Android OCR to read 3 documents
* Android ASR to hear user speak
* Gemini LLM to respond to document / utterance combination
* Android TTS to speak responses back to the user

## `Message relay (Android)`

* Android ASR to hear user speak
* LLM to respond to user, togglable between:
  * Android LLM: on-device, fast, but lower quality
  * Gemini LLM: higher quality, and uses conversation history
* Android TTS to speak it back to the user

## `Message relay (Gemini)`

* Gemini ASR to hear user speak
* Gemini LLM to respond to user, using conversation history