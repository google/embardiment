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
using System.Collections.Generic;
using System.Text;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Google.XR.Embardiment
{
    public class GeminiLlm : MonoBehaviour
    {
        private static readonly string _geminiLlmEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public bool UseConversationHistory;
        [ResizableTextArea]
        public string SystemInstruction;
        [ResizableTextArea]
        public string SourcePrompt;
        [ResizableTextArea]
        public string RecentGeneratedText;
        public UnityEvent<string> OnComplete;

        private List<System.Tuple<string, string>> _conversationHistory = new List<System.Tuple<string, string>>();

        [Button]
        public void SendPrompt()
        {
            SendPrompt(null, null);
        }

        public void SendPrompt(string newSourcePrompt)
        {
            SendPrompt(newSourcePrompt, null);
        }

        public void SendPrompt(string newSourcePrompt, Action<string> invocationCallback)
        {
            if (!string.IsNullOrEmpty(newSourcePrompt))
            {
                SourcePrompt = newSourcePrompt;
            }
            if (string.IsNullOrEmpty(SourcePrompt))
            {
                Debug.LogError("No source prompt provided");
                return;
            }
            StartCoroutine(SendRequest(SourcePrompt, invocationCallback));
        }

        private IEnumerator SendRequest(string promptText, Action<string> invocationCallback)
        {
            if (GeminiKey.Instance == null)
            {
                Debug.LogError("Error: no API key.  Please consult readme for instructions");
                yield return null;
            }
            if (UseConversationHistory)
            {
                _conversationHistory.Add(System.Tuple.Create("user", promptText));
            }
            JObject postData = new JObject();
            if (!string.IsNullOrEmpty(SystemInstruction))
            {
                postData["system_instruction"] = new JObject();
                postData["system_instruction"]["parts"] = new JArray { new JObject() };
                postData["system_instruction"]["parts"][0]["text"] = SystemInstruction;
            }

            if (UseConversationHistory && _conversationHistory.Count > 0)
            {
                JArray contentsArray = new JArray();
                foreach (var message in _conversationHistory)
                {
                    JObject contentEntry = new JObject();
                    contentEntry["role"] = message.Item1;
                    JObject part = new JObject();
                    part["text"] = message.Item2;
                    contentEntry["parts"] = new JArray { part };
                    contentsArray.Add(contentEntry);
                }
                postData["contents"] = contentsArray;
            }
            else
            {
                postData["contents"] = new JArray { new JObject() };
                postData["contents"][0]["parts"] = new JArray { new JObject() };
                postData["contents"][0]["parts"][0]["text"] = promptText;
            }
            postData["generationConfig"] = new JObject();
            postData["generationConfig"]["thinkingConfig"] = new JObject();
            postData["generationConfig"]["thinkingConfig"]["thinkingBudget"] = 0;

            string postDataString = JsonConvert.SerializeObject(postData);
            byte[] postDataRaw = Encoding.UTF8.GetBytes(postDataString);

            using (UnityWebRequest request = new UnityWebRequest(_geminiLlmEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(postDataRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", GeminiKey.Instance.Key);

                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log("Error: " + request.error);
                }
                else
                {
                    JObject response = JObject.Parse(request.downloadHandler.text);
                    JObject firstCandidate = (JObject)response["candidates"][0];
                    RecentGeneratedText = (string)firstCandidate["content"]["parts"][0]["text"];

                    if (UseConversationHistory)
                    {
                        _conversationHistory.Add(System.Tuple.Create("model", RecentGeneratedText));
                    }
                    invocationCallback?.Invoke(RecentGeneratedText);
                    OnComplete?.Invoke(RecentGeneratedText);
                }
            }
        }
    }
}