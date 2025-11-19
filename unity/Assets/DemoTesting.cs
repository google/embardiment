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
    public int SelectedBehaviourIndex = 0;

    private void Start()
    {
        BehaviourList = GetComponentsInChildren<MonoBehaviour>()
            .Where(b => b != null && b.GetType().Namespace == "Google.XR.Embardiment")
            .ToList();
    }

    private void Update()
    {
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            SelectedBehaviourIndex--;
            if (SelectedBehaviourIndex < 0)
            {
                SelectedBehaviourIndex = BehaviourList.Count - 1;
            }
            MyLog($"Changed to {BehaviourList[SelectedBehaviourIndex].GetType().Name}");
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            SelectedBehaviourIndex++;
            if (SelectedBehaviourIndex >= BehaviourList.Count)
            {
                SelectedBehaviourIndex = 0;
            }
            MyLog($"Changed to {BehaviourList[SelectedBehaviourIndex].GetType().Name}");
        }

        if (BehaviourList[SelectedBehaviourIndex] is AndroidAsr androidAsr)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                androidAsr.OnComplete.RemoveAllListeners();
                MyLog("listener added 1");
                androidAsr.OnComplete.AddListener((myString) =>
                {
                    MyLog("from listener1: " + myString);
                    MyLog("recent:" + androidAsr.RecentTranscription);
                });
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                androidAsr.OnComplete.RemoveAllListeners();
                MyLog("listener added 2");
                androidAsr.OnComplete.AddListener((myString) =>
                {
                    MyLog("from listener2: " + myString);
                    MyLog("recent:" + androidAsr.RecentTranscription);
                });
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                androidAsr.OpenRecognitionStream();
                MyLog("no instance callback");
            }
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                androidAsr.OpenRecognitionStream((myString) =>
                {
                    MyLog("callback: " + myString);
                });
            }
        }

        if (BehaviourList[SelectedBehaviourIndex] is AndroidLlm androidLlm)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                MyLog("Listener1");
                androidLlm.OnComplete.RemoveAllListeners();
                androidLlm.OnComplete.AddListener(response =>
                {
                    MyLog("LLM Response 1: " + response);
                });
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                MyLog("Listener2");
                androidLlm.OnComplete.RemoveAllListeners();
                androidLlm.OnComplete.AddListener(response =>
                {
                    MyLog("2 handler" + androidLlm.RecentGeneratedText);
                });
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                MyLog("Setting temperature to 0.1");
                androidLlm.Temperature = 0.1f;
            }
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                MyLog("Setting temperature to 0.9");
                androidLlm.Temperature = 0.9f;
            }
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                MyLog("Setting topK to 1");
                androidLlm.TopK = 1;
            }
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                MyLog("Setting topK to 32");
                androidLlm.TopK = 32;
            }
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                MyLog("Setting maxOutputTokens to 64");
                androidLlm.MaxOutputTokens = 64;
            }
            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                MyLog("Setting maxOutputTokens to 512");
                androidLlm.MaxOutputTokens = 512;
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                MyLog("asking about france");
                androidLlm.SendPrompt("What is the capital of France?  Reply with only 1 word");
            }
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                MyLog("asking for a joke");
                androidLlm.SendPrompt("Tell me a short joke, no longer than 15 words");
            }
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                MyLog("asking about earth");
                androidLlm.SendPrompt("Tell a short trivial fact about the earth");
            }
        }

        if (BehaviourList[SelectedBehaviourIndex] is AndroidOcr androidOcr)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 touchPos = touchscreen.primaryTouch.position.ReadValue();

                // Top third of the screen
                if (touchPos.y > Screen.height * 2 / 3.0f)
                {
                    MyLog("Listener replaced 1");
                    androidOcr.OnComplete.RemoveAllListeners();
                    androidOcr.OnComplete.AddListener((response) =>
                    {
                        MyLog("Listener 1");
                        MyLog("OCR Complete. Full Text: " + response.FullText);

                        foreach (var block in response.TextBlocks)
                        {
                            MyLog("Block: " + block.Text);
                            foreach (var line in block.Lines)
                            {
                                MyLog("  Line: " + line.Text);
                            }
                        }
                    });
                }
                // Middle third of the screen
                else if (touchPos.y > Screen.height / 3.0f)
                {
                    MyLog("Listener replaced 2");
                    androidOcr.OnComplete.RemoveAllListeners();
                    androidOcr.OnComplete.AddListener((response) =>
                    {
                        MyLog("Listener 2");
                    });
                }
                // Bottom third of the screen
                else
                {
                    MyLog("Recognizing text from texture...");
                    androidOcr.RecognizeText(TextureToOcr);
                }
            }
        }
        if (BehaviourList[SelectedBehaviourIndex] is AndroidTts androidTts)
        {
            string sentence1 = "The quick brown fox jumped over the lazy dogs";
            string sentence2 = "Lorem ipsum dolor sit amet";

            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                MyLog("Listeners added");
                androidTts.OnSpeechGenerated.RemoveAllListeners();
                androidTts.OnDoneTalking.RemoveAllListeners();
                androidTts.OnSpeechGenerated.AddListener(() => MyLog("Event: Speech generation complete."));
                androidTts.OnDoneTalking.AddListener(() => MyLog("Event: Finished talking."));
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                MyLog("Stopping speech.");
                androidTts.Stop();
            }

            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                MyLog("sentence 1 loaded");
                androidTts.SourceText = sentence1;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                MyLog("sentence 2 loaded");
                androidTts.SourceText = sentence2;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                MyLog("speaking preloaded text");
                androidTts.Speak();
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                MyLog("AndroidTTS GetVoiceList");
                string[] voiceList = androidTts.GetVoiceList();
                MyLog("AndroidTTS GetVoiceList: " + voiceList[0]);
                MyLog("AndroidTTS GetVoiceList: " + voiceList[1]);
                MyLog("AndroidTTS GetVoiceList: " + voiceList[2]);
                MyLog("AndroidTTS GetVoiceList: " + voiceList.Length);
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                androidTts.VoiceIndex = 0;
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                androidTts.VoiceIndex = 1;
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                androidTts.VoiceIndex = 2;
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                androidTts.VoiceIndex = -1;
                androidTts.Language = "en-US";
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                androidTts.VoiceIndex = -1;
                androidTts.Language = "en-AU";
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                androidTts.VoiceIndex = -1;
                androidTts.Language = "en-GB";
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.digit0Key.isPressed)
            {
                MyLog("AndroidTTS is speaking: " + androidTts.IsSpeaking);
            }

            if (Keyboard.current.zKey.wasPressedThisFrame)
            {
                androidTts.Pitch = 0.5f;
                androidTts.Speed = 0.5f;
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                androidTts.Pitch = 1;
                androidTts.Speed = 1;
                androidTts.Speak(sentence1);
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                androidTts.Pitch = 2;
                androidTts.Speed = 2;
                androidTts.Speak(sentence1);
            }
        }

        if (BehaviourList[SelectedBehaviourIndex] is DesktopOcr desktopOcr)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                MyLog("Listener replaced 1");
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
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                MyLog("Listener replaced 2");
                desktopOcr.OnComplete.RemoveAllListeners();
                desktopOcr.OnComplete.AddListener((response) =>
                {
                    MyLog("Listener 2");
                });
            }
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                MyLog("Recognizing text from texture...");
                desktopOcr.RecognizeText(TextureToOcr);
            }
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                desktopOcr.UseCache = !desktopOcr.UseCache;
                MyLog("Toggled useCache to: " + desktopOcr.UseCache);
            }
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                desktopOcr.ClearCache();
                MyLog("Cache cleared. The 'useCache' setting is currently: " + desktopOcr.UseCache);
            }
        }


        if (BehaviourList[SelectedBehaviourIndex] is GeminiASR geminiAsr)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                MyLog("Resetting onComplete");
                geminiAsr.OnComplete.RemoveAllListeners();
                geminiAsr.OnComplete.AddListener(transcription =>
                {
                    MyLog("Transcription complete: " + transcription);
                });
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                MyLog("Resetting onComplete (2)");
                geminiAsr.OnComplete.RemoveAllListeners();
                geminiAsr.OnComplete.AddListener(transcription =>
                {
                    MyLog("Transcription complete: " + geminiAsr.RecentTranscription);
                    MyLog("2");
                });
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                geminiAsr.RequestRecognition(AudioClipToAsr);
            }

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
        if (BehaviourList[SelectedBehaviourIndex] is GeminiLlm geminiLlm)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                MyLog("Listener1");
                geminiLlm.OnComplete.RemoveAllListeners();
                geminiLlm.OnComplete.AddListener(response =>
                {
                    MyLog("LLM Response 1: " + response);
                });
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                MyLog("Listener2");
                geminiLlm.OnComplete.RemoveAllListeners();
                geminiLlm.OnComplete.AddListener(response =>
                {
                    MyLog("2 handler" + geminiLlm.RecentGeneratedText);
                });
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                geminiLlm.SendPrompt("What is the capital of France?");
            }
        }
        if (BehaviourList[SelectedBehaviourIndex] is GeminiTts geminiTts)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                MyLog("Listener1");
                geminiTts.OnSpeechGenerated.RemoveAllListeners();
                geminiTts.OnSpeechGenerated.AddListener(clip =>
                {
                    MyLog("Audio generated. Playing clip.");
                    AudioSource source = GetComponent<AudioSource>();
                    if (source == null)
                    {
                        MyLog("Adding temporary AudioSource to play clip.");
                        source = gameObject.AddComponent<AudioSource>();
                    }
                    if (clip != null)
                    {
                        source.PlayOneShot(clip);
                    }
                });
                geminiTts.OnDoneTalking.RemoveAllListeners();
                geminiTts.OnDoneTalking.AddListener(() =>
                {
                    MyLog("Event: done talking");
                });
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                MyLog("Listener2");
                geminiTts.OnSpeechGenerated.RemoveAllListeners();
                geminiTts.OnSpeechGenerated.AddListener(clip =>
                {
                    MyLog("Returned!");
                });
                geminiTts.OnDoneTalking.RemoveAllListeners();
                geminiTts.OnDoneTalking.AddListener(() =>
                {
                    MyLog("Event: done talking from listener 2");
                });
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                geminiTts.Stop();
                MyLog("stopping speech");
            }


            string sentence = "The quick brown fox jumped over the lazy dogs.";

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                geminiTts.VoiceName = "Orus";
                MyLog("Generating Audio");
                geminiTts.GenerateAudio(sentence);
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                geminiTts.VoiceName = "Gacrux";
                MyLog("Requesting speech");
                geminiTts.Speak(sentence);
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                geminiTts.VoiceName = "Leda";
                MyLog("Requesting speech");
                geminiTts.Speak(sentence);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                geminiTts.VoiceName = "Leda";
                MyLog("Requesting speech");
                geminiTts.Speak("Say with a drippingly sarcastic tone: Sure, yeah right!");
            }

        }
    }

    private void MyLog(string message)
    {
        string behaviourName = BehaviourList[SelectedBehaviourIndex].GetType().Name;
        Debug.Log($"DEMO TESTING: {behaviourName} - {message}");
    }
}
