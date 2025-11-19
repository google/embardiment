// Copyright 2025 The Embardiment Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Google.XR.Embardiment
{
    public class AndroidLlm : MonoBehaviour
    {
        class LlmCallbackProxy : AndroidJavaProxy
        {
            private readonly Action<string> _onSuccess;
            private readonly Action<string> _onFailure;

            public LlmCallbackProxy(Action<string> onSuccess, Action<string> onFailure)
                : base("com.google.xr.embardiment.llm.LlmBridge$LlmCallback")
            {
                _onSuccess = onSuccess;
                _onFailure = onFailure;
            }

            public void onSuccess(string result)
            {
                _onSuccess?.Invoke(result);
            }

            public void onFailure(string errorMessage)
            {
                _onFailure?.Invoke(errorMessage);
            }
        }

        public UnityEvent<string> OnComplete;
        public int MaxOutputTokens = 256;
        public string RecentGeneratedText;
        public string SourcePrompt = "";
        [Range(0.0f, 1.0f)]
        public float Temperature = 0.5f;
        public int TopK = 16;

        private AndroidJavaObject _bridge;

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
            ApplySettings();
            if (!string.IsNullOrEmpty(newSourcePrompt))
            {
                SourcePrompt = newSourcePrompt;
            }
            if (string.IsNullOrEmpty(SourcePrompt))
            {
                Debug.LogError("No prompt was provided.");
                return;
            }

            if (Application.platform != RuntimePlatform.Android)
            {
                Debug.LogWarning("AndroidLLM only works inside an Android context");
                return;
            }

            var callback = new LlmCallbackProxy(
                (responseText) =>
                {
                    RecentGeneratedText = responseText;
                    invocationCallback?.Invoke(responseText);
                    OnComplete?.Invoke(responseText);
                },
                (error) =>
                {
                    Debug.LogError("LLM Failure: " + error);
                }
            );
            _bridge.Call("generateResponse", SourcePrompt, callback);
        }

        private void Start()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                _bridge = new AndroidJavaObject("com.google.xr.embardiment.llm.LlmBridge");
                ApplySettings();
            }
        }

        private void OnDestroy()
        {
            _bridge?.Dispose();
            _bridge = null;
        }

        private void ApplySettings()
        {
            if (_bridge == null)
            {
                return;
            }
            _bridge.Call("updateSettings", MaxOutputTokens, Temperature, TopK);
        }
    }
}