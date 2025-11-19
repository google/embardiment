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
using AndroidXRUnitySamples.Gemini;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

namespace Google.XR.Embardiment
{
    public class AndroidAsr : MonoBehaviour
    {
        public UnityEvent<string> OnComplete;
        public string RecentTranscription;

        private SpeechToTextBridge _sttBridge;
        private bool _permissionRequested;
        private Action<string> _invocationCallback;

        public void OpenRecognitionStream(Action<string> invocationCallback)
        {
            if (_sttBridge != null)
            {
                _invocationCallback = invocationCallback;
                _sttBridge.StartRecognition();
            }
            else
            {
                Debug.LogWarning("SpeechToTextBridge not initialized. Cannot start recognition.");
            }
        }

        public void OpenRecognitionStream()
        {
            OpenRecognitionStream(null);
        }

        private void Start()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                _permissionRequested = true;
            }
            else
            {
                InitializeBridge();
            }
        }

        private void Update()
        {
            if (_permissionRequested && Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                _permissionRequested = false;
                InitializeBridge();
            }
        }

        private void OnDestroy()
        {
            if (_sttBridge != null)
            {
                _sttBridge.OnResult -= SttBridgeOnResult;
                _sttBridge.Dispose();
                _sttBridge = null;
            }
        }

        private void InitializeBridge()
        {
            if (UnityMainThreadDispatcher.Instance == null)
            {
                gameObject.AddComponent<UnityMainThreadDispatcher>();
            }

            if (_sttBridge != null) return;

            _sttBridge = new SpeechToTextBridge();
            _sttBridge.OnResult += SttBridgeOnResult;
            Debug.Log("SpeechToTextBridge initialized.");
        }

        private void SttBridgeOnResult(SpeechToTextResult obj)
        {
            _invocationCallback?.Invoke(obj.Text);
            _invocationCallback = null;
            RecentTranscription = obj.Text;
            OnComplete?.Invoke(obj.Text);
        }
    }
}