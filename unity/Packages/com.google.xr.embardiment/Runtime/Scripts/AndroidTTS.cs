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

using UnityEngine;
using UnityEngine.Events;

namespace Google.XR.Embardiment
{
    public class AndroidTts : MonoBehaviour
    {
        public string Language;
        public UnityEvent OnSpeechGenerated;
        public UnityEvent OnDoneTalking;
        [Range(0.0f, 2.0f)]
        public float Pitch = 1f;
        [Range(0.0f, 2.0f)]
        public float Speed = 1f;
        public string SourceText;
        public int VoiceIndex = -1;
        public bool IsSpeaking => _androidJObject != null ? bool.Parse(_androidJObject.Call<string>("GetIsSpeaking")) : false;

        private AndroidJavaObject _androidJObject = null;
        private bool _watchForStart = false;
        private bool _watchForStop = false;

        public string[] GetVoiceList()
        {
            InitializeIfNull();
            string listString = _androidJObject.Call<string>("GetVoiceList");
            return listString.Split(",");
        }

        public void Speak()
        {
            InitializeIfNull();
            if (!string.IsNullOrEmpty(Language))
            {
                _androidJObject.Call("SetLanguage", Language);
            }
            _androidJObject.Call("SetPitch", Pitch);
            _androidJObject.Call("SetSpeechRate", Speed);
            if (VoiceIndex >= 0)
            {
                _androidJObject.Call("SetVoiceIndex", VoiceIndex.ToString());
            }
            _androidJObject.Call("Speak", SourceText);
            _watchForStart = true;
            _watchForStop = false;
        }

        public void Speak(string newSourceText)
        {
            SourceText = newSourceText;
            Speak();
        }

        public void Stop()
        {
            _androidJObject.Call("Stop");
            OnDoneTalking?.Invoke();
            _watchForStart = false;
            _watchForStop = false;
        }

        private void Awake()
        {
            InitializeIfNull();
        }

        private void Update()
        {
            if (_watchForStart)
            {
                if (IsSpeaking)
                {
                    OnSpeechGenerated?.Invoke();
                    _watchForStart = false;
                    _watchForStop = true;
                }
            }
            if (_watchForStop)
            {
                if (!IsSpeaking)
                {
                    OnDoneTalking?.Invoke();
                    _watchForStop = false;
                }
            }
        }

        private void InitializeIfNull()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            Debug.LogWarning("AndroidTTS only works inside an Android context");
#else
            if (_androidJObject == null)
            {
                AndroidJavaObject activityContext = null;
                try
                {
                    using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
                    }
                    using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.example.ttsunityplugin.TTSPluginInstance"))
                    {
                        if (pluginClass != null)
                        {
                            _androidJObject = new AndroidJavaObject("com.example.ttsunityplugin.TTSPluginInstance");
                            _androidJObject.CallStatic("receiveUnityActivity", activityContext);
                            _androidJObject.Call("InitializeTTS");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to initialize TextToSpeech due to error:{e.Message}");
                }
            }
#endif
        }
    }
}