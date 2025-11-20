// Copyright 2025 The Embardiment Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Google.XR.Embardiment;
using UnityEngine;
using UnityEngine.InputSystem;

public class DemoTesting : MonoBehaviour
{
    public Texture2D TextureToOcr;
    public AudioClip AudioClipToAsr;
    public List<MonoBehaviour> BehaviourList;
    public TMPro.TextMeshProUGUI OutputText;
    private int _selectedBehaviourIndex = 0;

    public MonoBehaviour SelectedBehaviour
    {
        get
        {
            if (BehaviourList != null && BehaviourList.Count > 0)
            {
                return BehaviourList[_selectedBehaviourIndex];
            }
            return null;
        }
    }

    private void Start()
    {
        BehaviourList = GetComponentsInChildren<MonoBehaviour>()
            .Where(b => b != null && b.GetType().Namespace == "Google.XR.Embardiment")
            .ToList();
    }

    private void Update()
    {
        HandleKey(Key.LeftArrow, "Previous prefab", () =>
        {
            _selectedBehaviourIndex--;
            if (_selectedBehaviourIndex < 0)
            {
                _selectedBehaviourIndex = BehaviourList.Count - 1;
            }
            MyLog($"Changed to {SelectedBehaviour.name}");
        });
        HandleKey(Key.RightArrow, "Next prefab", () =>
        {
            _selectedBehaviourIndex++;
            if (_selectedBehaviourIndex >= BehaviourList.Count)
            {
                _selectedBehaviourIndex = 0;
            }
            MyLog($"Changed to {SelectedBehaviour.name}");
        });

        if (SelectedBehaviour is AndroidAsr androidAsr)
        {
            ProcessInputVia(androidAsr);
        }
        else if (SelectedBehaviour is AndroidLlm androidLlm)
        {
            ProcessInputVia(androidLlm);
        }
        else if (SelectedBehaviour is AndroidOcr androidOcr)
        {
            ProcessInputVia(androidOcr);
        }
        else if (SelectedBehaviour is AndroidTts androidTts)
        {
            ProcessInputVia(androidTts);
        }
        else if (SelectedBehaviour is DesktopOcr desktopOcr)
        {
            ProcessInputVia(desktopOcr);
        }
        else if (SelectedBehaviour is GeminiASR geminiAsr)
        {
            ProcessInputVia(geminiAsr);
        }
        else if (SelectedBehaviour is GeminiLlm geminiLlm)
        {
            ProcessInputVia(geminiLlm);
        }
        else if (SelectedBehaviour is GeminiTts geminiTts)
        {
            ProcessInputVia(geminiTts);
        }
    }

    private void HandleKey(Key key, string description, Action action)
    {
        if (Keyboard.current[key].wasPressedThisFrame)
        {
            MyLog(description);
            action.Invoke();
        }
    }

    private void MyLog(string message)
    {
        string outputMessage = $"DEMO TESTING: {SelectedBehaviour.name} - {message}";
        Debug.Log(outputMessage);
        OutputText.text = outputMessage + "\n" + OutputText.text;
    }

    private void ProcessInputVia(AndroidAsr androidAsr)
    {
        HandleKey(Key.Digit1, "Adding listener 1", () =>
        {
            androidAsr.OnComplete.RemoveAllListeners();
            androidAsr.OnComplete.AddListener((myString) =>
            {
                MyLog("From listener1: " + myString);
                MyLog("Recent:" + androidAsr.RecentTranscription);
            });
        });
        HandleKey(Key.Digit2, "Adding listener 2", () =>
        {
            androidAsr.OnComplete.RemoveAllListeners();
            androidAsr.OnComplete.AddListener((myString) =>
            {
                MyLog("From listener2: " + myString);
                MyLog("Recent:" + androidAsr.RecentTranscription);
            });
        });

        HandleKey(Key.A, "Opening Recognition, no instance callback", () => androidAsr.OpenRecognitionStream());
        HandleKey(Key.S, "Opening Recognition with instance callback", () =>
        {
            androidAsr.OpenRecognitionStream((myString) =>
            {
                MyLog("Callback: " + myString);
            });
        });
    }

    private void ProcessInputVia(AndroidLlm androidLlm)
    {
        HandleKey(Key.Digit1, "Adding listener 1", () =>
        {
            androidLlm.OnComplete.RemoveAllListeners();
            androidLlm.OnComplete.AddListener(response => MyLog("From listener 1: " + response));
        });
        HandleKey(Key.Digit2, "Adding listener 2", () =>
        {
            androidLlm.OnComplete.RemoveAllListeners();
            androidLlm.OnComplete.AddListener(response => MyLog("From listener 2: " + response));
        });

        HandleKey(Key.Q, "Setting Temperature to 0.1", () => { androidLlm.Temperature = 0.1f; });
        HandleKey(Key.W, "Setting Temperature to 0.9", () => { androidLlm.Temperature = 0.9f; });
        HandleKey(Key.E, "Setting TopK to 1", () => { androidLlm.TopK = 1; });
        HandleKey(Key.R, "Setting TopK to 32", () => { androidLlm.TopK = 32; });
        HandleKey(Key.T, "Setting MaxOutputTokens to 64", () => { androidLlm.MaxOutputTokens = 64; });
        HandleKey(Key.Y, "Setting MaxOutputTokens to 512", () => { androidLlm.MaxOutputTokens = 512; });

        HandleKey(Key.A, "Asking about France", () => androidLlm.SendPrompt("What is the capital of France?  Reply with only 1 word"));
        HandleKey(Key.S, "Asking for a joke", () => androidLlm.SendPrompt("Tell me a short joke, no longer than 15 words"));
        HandleKey(Key.D, "Asking about earth", () => androidLlm.SendPrompt("Tell a short trivial fact about the earth"));
    }

    private void ProcessInputVia(AndroidOcr androidOcr)
    {
        HandleKey(Key.Digit1, "Adding listener 1", () =>
        {
            androidOcr.OnComplete.RemoveAllListeners();
            androidOcr.OnComplete.AddListener((response) =>
            {
                MyLog("From listener 1. OCR Complete. Full Text: " + response.FullText);
                foreach (var block in response.TextBlocks)
                {
                    MyLog("Block: " + block.Text);
                    foreach (var line in block.Lines)
                    {
                        MyLog("  Line: " + line.Text);
                    }
                }
            });
        });
        HandleKey(Key.Digit2, "Adding listener 2", () =>
        {
            androidOcr.OnComplete.RemoveAllListeners();
            androidOcr.OnComplete.AddListener((response) => MyLog("Listener 2"));
        });

        HandleKey(Key.A, "Recognizing text from texture...", () => androidOcr.RecognizeText(TextureToOcr));
    }

    private void ProcessInputVia(AndroidTts androidTts)
    {
        HandleKey(Key.Digit1, "Adding listeners 1", () =>
        {
            androidTts.OnSpeechGenerated.RemoveAllListeners();
            androidTts.OnSpeechGenerated.AddListener(() => MyLog("Speech generation complete."));
            androidTts.OnDoneTalking.RemoveAllListeners();
            androidTts.OnDoneTalking.AddListener(() => MyLog("Finished talking."));
        });

        HandleKey(Key.Q, "Sentence 1 loaded", () => androidTts.SourceText = "The quick brown fox jumped over the lazy dogs");
        HandleKey(Key.W, "Sentence 2 loaded", () => androidTts.SourceText = "Lorem ipsum dolor sit amet");
        HandleKey(Key.E, "Voice -1", () => androidTts.VoiceIndex = -1);
        HandleKey(Key.R, "Voice 0", () => androidTts.VoiceIndex = 0);
        HandleKey(Key.T, "Voice 1", () => androidTts.VoiceIndex = 1);
        HandleKey(Key.Y, "Language en-US", () => androidTts.Language = "en-US");
        HandleKey(Key.U, "Language en-AU", () => androidTts.Language = "en-AU");
        HandleKey(Key.I, "Language en-GB", () => androidTts.Language = "en-GB");
        HandleKey(Key.O, "Pitch and speed: 0.5", () =>
        {
            androidTts.Pitch = 0.5f;
            androidTts.Speed = 0.5f;
        });
        HandleKey(Key.P, "Pitch and speed: 1", () =>
        {
            androidTts.Pitch = 1;
            androidTts.Speed = 1;
        });
        HandleKey(Key.LeftBracket, "Pitch and speed: 2", () =>
        {
            androidTts.Pitch = 2;
            androidTts.Speed = 2;
        });

        HandleKey(Key.A, "Speaking preloaded text", () => androidTts.Speak());
        HandleKey(Key.S, "Calling IsSpeaking", () => MyLog("Speaking state: " + androidTts.IsSpeaking));
        HandleKey(Key.D, "Stopping speech", () => androidTts.Stop());
        HandleKey(Key.F, "Retriving voice list", () =>
        {
            string[] voiceList = androidTts.GetVoiceList();
            MyLog("GetVoiceList 0: " + voiceList[0]);
            MyLog("GetVoiceList 1: " + voiceList[1]);
            MyLog("GetVoiceList 2: " + voiceList[2]);
            MyLog("GetVoiceList length: " + voiceList.Length);
        });
    }

    private void ProcessInputVia(DesktopOcr desktopOcr)
    {
        HandleKey(Key.Digit1, "Adding listener 1", () =>
        {
            desktopOcr.OnComplete.RemoveAllListeners();
            desktopOcr.OnComplete.AddListener((response) =>
            {
                MyLog("Listener 1");
                MyLog("OCR Complete. Full Text: " + response.FullText);
                if (response.WordBoxes != null)
                {
                    MyLog("Word Boxes Found: " + response.WordBoxes.Length);
                    MyLog("X: " + response.WordBoxes[0].X);
                    MyLog("Y: " + response.WordBoxes[0].Y);
                    MyLog("W: " + response.WordBoxes[0].Width);
                    MyLog("H: " + response.WordBoxes[0].Height);
                    MyLog("Word: " + response.WordBoxes[0].Word);
                }
            });
        });
        HandleKey(Key.Digit2, "Adding listener 2", () =>
        {
            desktopOcr.OnComplete.RemoveAllListeners();
            desktopOcr.OnComplete.AddListener((response) =>
            {
                MyLog("Listener 2");
            });
        });

        HandleKey(Key.Q, "UseCache = true", () => desktopOcr.UseCache = true);
        HandleKey(Key.W, "UseCache = false", () => desktopOcr.UseCache = false);

        HandleKey(Key.A, "Recognizing text from texture...", () => desktopOcr.RecognizeText(TextureToOcr));
        HandleKey(Key.S, "Clearing cache", () => desktopOcr.ClearCache());
    }

    private void ProcessInputVia(GeminiASR geminiAsr)
    {
        HandleKey(Key.Digit1, "Adding listener 1", () =>
        {
            geminiAsr.OnComplete.RemoveAllListeners();
            geminiAsr.OnComplete.AddListener(transcription =>
            {
                MyLog("From listener 1: " + transcription);
            });
        });
        HandleKey(Key.Digit2, "Adding listener 2", () =>
        {
            geminiAsr.OnComplete.RemoveAllListeners();
            geminiAsr.OnComplete.AddListener(transcription =>
            {
                MyLog("From listener 2: " + transcription);
            });
        });

        HandleKey(Key.A, "Requesting recognition", () => geminiAsr.RequestRecognition(AudioClipToAsr));

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            MyLog("Starting recording...");
            geminiAsr.StartRecording();
        }
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            MyLog("Stopping recording and sending for transcription...");
            geminiAsr.StopRecordingAndSend();
        }
    }

    private void ProcessInputVia(GeminiLlm geminiLlm)
    {
        HandleKey(Key.Digit1, "Adding listener 1", () =>
        {
            geminiLlm.OnComplete.RemoveAllListeners();
            geminiLlm.OnComplete.AddListener(response =>
            {
                MyLog("LLM Response 1: " + response);
            });
        });
        HandleKey(Key.Digit2, "Adding listener 2", () =>
        {
            geminiLlm.OnComplete.RemoveAllListeners();
            geminiLlm.OnComplete.AddListener(response =>
            {
                MyLog("LLM Response 2: " + response);
            });
        });
        HandleKey(Key.A, "Sending LLM Prompt", () => geminiLlm.SendPrompt("What is the capital of France?"));
    }

    private void ProcessInputVia(GeminiTts geminiTts)
    {
        HandleKey(Key.Digit1, "Adding listeners 1", () =>
        {
            geminiTts.OnSpeechGenerated.RemoveAllListeners();
            geminiTts.OnSpeechGenerated.AddListener(clip =>
            {
                MyLog("Audio generated. Playing clip manually.");
                geminiTts.GetComponent<AudioSource>().PlayOneShot(clip);
            });
            geminiTts.OnDoneTalking.RemoveAllListeners();
            geminiTts.OnDoneTalking.AddListener(() => MyLog("Event: done talking"));
        });
        HandleKey(Key.Digit2, "Adding listeners 2", () =>
        {
            geminiTts.OnSpeechGenerated.RemoveAllListeners();
            geminiTts.OnSpeechGenerated.AddListener(clip =>
            {
                MyLog("Returned but not playing yet");
            });
            geminiTts.OnDoneTalking.RemoveAllListeners();
            geminiTts.OnDoneTalking.AddListener(() =>
            {
                MyLog("Event: done talking from listener 2");
            });
        });

        HandleKey(Key.Q, "Set voice: Orus", () => geminiTts.VoiceName = "Orus");
        HandleKey(Key.W, "Set voice: Gacrux", () => geminiTts.VoiceName = "Gacrux");
        HandleKey(Key.E, "Set voice: Leda", () => geminiTts.VoiceName = "Leda");

        string sentence = "The quick brown fox jumped over the lazy dogs.";
        HandleKey(Key.A, "Generate audio only", () => geminiTts.GenerateAudio(sentence));
        HandleKey(Key.S, "Requesting speech", () => geminiTts.Speak(sentence));
        HandleKey(Key.D, "Requesting styled speech", () => geminiTts.Speak("Say with dripping sarcasm: yeah right"));
        HandleKey(Key.F, "Stop speech", () => geminiTts.Stop());
    }
}