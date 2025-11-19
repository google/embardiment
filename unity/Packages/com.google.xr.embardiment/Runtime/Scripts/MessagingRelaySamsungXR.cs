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

using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Google.XR.Embardiment
{
    public class MessagingRelaySamsungXr : MonoBehaviour
    {
        [InfoBox("To use:\n\n" +
            "1. Play the scene\n\n" +
            "2. Push the \"Push to Talk\" button to ask a question\n\n" +
            "3. Review LLM's response", EInfoBoxType.Normal)]
        public TextMeshProUGUI InfoArea;
        public Transform MessageContainer;

        private GameObject _messageTemplate;
        private AndroidAsr _androidAsr;
        private GeminiLlm _geminiLlm;
        private AndroidTts _androidTts;
        private GameObject _lastUserMessage;
        private GameObject _lastSystemMessage;

        public void StartRecording()
        {
            _androidTts.Stop();
            InfoArea.text = "Recording and processing voice...";
            _lastUserMessage = AddMessage("Processing audio...", false);
            _androidAsr.OpenRecognitionStream();
        }

        private void Start()
        {
            _messageTemplate = MessageContainer.Find("Message Template").gameObject;

            _androidAsr = GetComponentInChildren<AndroidAsr>();
            _androidAsr.OnComplete.AddListener(OnAsrReturn);

            _geminiLlm = GetComponentInChildren<GeminiLlm>();
            _geminiLlm.OnComplete.AddListener(OnLlmReturn);

            _androidTts = GetComponentInChildren<AndroidTts>();
            _androidTts.gameObject.SetActive(true);
        }

        private void OnAsrReturn(string recognizedText)
        {
            UpdateMessage(_lastUserMessage, recognizedText);
            InfoArea.text = "ASR Recognized.  Sending text to LLM...";

            _lastSystemMessage = AddMessage("Processing request...", true);
            _geminiLlm.SendPrompt(recognizedText);
        }

        private void OnLlmReturn(string responseText)
        {
            UpdateMessage(_lastSystemMessage, responseText);
            InfoArea.text = "Presenting LLM response";
            _androidTts.Speak(responseText);
        }

        private GameObject AddMessage(string initialText, bool isSystemMessage)
        {
            GameObject go = Instantiate(_messageTemplate, MessageContainer);
            go.SetActive(true);
            if (isSystemMessage)
            {
                go.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 75, 0, 0);
            }
            UpdateMessage(go, initialText);
            return go;
        }

        private void UpdateMessage(GameObject message, string text)
        {
            message.GetComponentInChildren<TextMeshProUGUI>().text = text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(message.transform.GetChild(0) as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(message.transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(message.transform.parent as RectTransform);
            message.transform.GetComponentInParent<ScrollRect>().normalizedPosition = Vector3.zero;
        }
    }
}