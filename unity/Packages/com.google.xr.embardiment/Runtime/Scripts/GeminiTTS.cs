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
using System.Collections;
using System.Text;
using NaughtyAttributes;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Google.XR.Embardiment
{
    [RequireComponent(typeof(AudioSource))]
    [ExecuteInEditMode]
    public class GeminiTts : MonoBehaviour
    {
        private static readonly string _geminiTtsEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public string VoiceName = "Zephyr";
        public string SourceText;
        public UnityEvent OnDoneTalking;
        public UnityEvent<AudioClip> OnSpeechGenerated;
        public AudioClip RecentAudioClip;

        private AudioSource _audioSource;
        private Coroutine _endWatcher;

        [Button("Generate Audio (But don't speak)")]
        public void GenerateAudio()
        {
            GenerateAudio(null, null);
        }

        public void GenerateAudio(string newSourceText)
        {
            GenerateAudio(newSourceText, null);
        }

        public void GenerateAudio(string newSourceText, Action<AudioClip> invocationCallback)
        {
            if (!string.IsNullOrEmpty(newSourceText))
            {
                SourceText = newSourceText;
            }
            if (string.IsNullOrEmpty(SourceText))
            {
                Debug.LogError("No source text provided");
                return;
            }
            StartCoroutine(GenerateAudioAsync(SourceText, invocationCallback));
        }

        [Button]
        public void Speak()
        {
            Speak(null, null);
        }

        public void Speak(string newSourceText)
        {
            Speak(newSourceText, null);
        }

        public void Speak(string newSourceText, Action<AudioClip> invocationCallback)
        {
            GenerateAudio(newSourceText, invocationCallback + ((clip) =>
            {
                _audioSource.clip = clip;
                _audioSource.Play();
                if (_endWatcher == null)
                {
                    _endWatcher = StartCoroutine(WatchForEnd());
                }
                else
                {
                    StopCoroutine(_endWatcher);
                    _endWatcher = StartCoroutine(WatchForEnd());
                }
            }));
        }

        public void Stop()
        {
            _audioSource.Stop();
        }

        private void OnEnable()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private IEnumerator WatchForEnd()
        {
            while (_audioSource.isPlaying)
            {
                yield return null;
            }
            OnDoneTalking?.Invoke();
            _endWatcher = null;
        }

        private IEnumerator GenerateAudioAsync(string sourceText, Action<AudioClip> invocationCallback = null)
        {
            if (GeminiKey.Instance == null)
            {
                Debug.LogError("Error: no API key.  Please consult readme for instructions");
                yield return null;
            }

            JObject postData = new JObject
            {
                ["model"] = "gemini-2.5-flash-preview-tts",
                ["contents"] = new JArray {
                    new JObject {
                        ["parts"] = new JArray {
                            new JObject {
                                ["text"] = sourceText
                            }
                        }
                    }
                },
                ["generationConfig"] = new JObject
                {
                    ["responseModalities"] = new JArray { "AUDIO" },
                    ["speechConfig"] = new JObject
                    {
                        ["voiceConfig"] = new JObject
                        {
                            ["prebuiltVoiceConfig"] = new JObject
                            {
                                ["voiceName"] = VoiceName
                            }
                        }
                    }
                }
            };

            string jsonBody = postData.ToString();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(_geminiTtsEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("x-goog-api-key", GeminiKey.Instance.Key);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error: " + request.error);
                    Debug.LogError("Response: " + request.downloadHandler.text);
                    yield break;
                }

                JObject responseJson = JObject.Parse(request.downloadHandler.text);
                string base64Audio = (string)responseJson.SelectToken("candidates[0].content.parts[0].inlineData.data");

                if (string.IsNullOrEmpty(base64Audio))
                {
                    Debug.LogError("Could not find audio data in response.");
                    yield break;
                }

                byte[] audioBytes = Convert.FromBase64String(base64Audio);

                const int sampleRate = 24000;
                const int channels = 1;

                float[] floatSamples = new float[audioBytes.Length / 2];
                for (int i = 0; i < floatSamples.Length; i++)
                {
                    short sample = BitConverter.ToInt16(audioBytes, i * 2);
                    floatSamples[i] = sample / 32768.0f;
                }

                RecentAudioClip = AudioClip.Create("GeneratedSpeech", floatSamples.Length, channels, sampleRate, false);
                RecentAudioClip.SetData(floatSamples, 0);
                invocationCallback?.Invoke(RecentAudioClip);
                OnSpeechGenerated?.Invoke(RecentAudioClip);
            }
        }
    }
}