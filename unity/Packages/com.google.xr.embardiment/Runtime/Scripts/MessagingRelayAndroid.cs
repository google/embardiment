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
    public class MessagingRelayAndroid : MonoBehaviour
    {
        [InfoBox("To use:\n\n" +
            "1. Play the scene\n\n" +
            "2. Hold down \"Push to Talk\" to ask a question, and release to send\n\n" +
            "3. Review LLM's response", EInfoBoxType.Normal)]
        public bool UseGeminiLlm;
        public TextMeshProUGUI InfoArea;
        public Transform MessageContainer;

        private GameObject _messageTemplate;
        private AndroidAsr _androidAsr;
        private AndroidLlm _androidLlm;
        private GeminiLlm _geminiLlm;
        private AndroidTts _androidTts;
        private GameObject _lastUserMessage;
        private GameObject _lastSystemMessage;

        public void StartRecording()
        {
            _androidTts.Stop();
            InfoArea.text = "Recording and processing voice...";
            _lastUserMessage = AddMessage("Processing audio...", false);
            Debug.Log("Opening speech recognition stream to send for text recognition...");
            _androidAsr.OpenRecognitionStream();
        }

        private void Start()
        {
            _messageTemplate = MessageContainer.Find("Message Template").gameObject;

            _androidAsr = GetComponentInChildren<AndroidAsr>();
            _androidAsr.OnComplete.AddListener(OnAsrReturn);

            _androidLlm = GetComponentInChildren<AndroidLlm>();
            _androidLlm.OnComplete.AddListener(OnLlmReturnAndroid);

            _geminiLlm = GetComponentInChildren<GeminiLlm>();
            _geminiLlm.OnComplete.AddListener(OnLlmReturnGemini);

            _androidTts = GetComponentInChildren<AndroidTts>();
            _androidTts.gameObject.SetActive(true);
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

        private void OnAsrReturn(string recognizedText)
        {
            InfoArea.text = "ASR Recognized.  Sending text to LLM...";
            UpdateMessage(_lastUserMessage, recognizedText);
            _lastSystemMessage = AddMessage("Processing request...", true);
            if (UseGeminiLlm)
            {
                Debug.Log("Recording converted to text.  Sending to Gemini LLM...");
                _geminiLlm.SendPrompt(recognizedText);
            }
            else
            {
                Debug.Log("Recording converted to text.  Sending to Android LLM...");
                _androidLlm.SendPrompt(
"You are a concise AI assistant. You respond to the user with a single, direct sentence. You must not continue the conversation by writing for the User.  You must respond with at least some text.\n\n" +

"User: Hello there how are you?\n" +
"Assistant: I'm doing fine thanks.  How can I help you?\n\n" +

"User: Can I ask you some questions?\n" +
"Assistant: Sure thing.  What can I help you with today?\n\n" +

"User: " + recognizedText + "\n" +
"Assistant: "
                );
            }
        }

        private void OnLlmReturnAndroid(string responseText)
        {
            string assistantResponse = responseText.Split("\nUser:")[0].Trim();
            if (string.IsNullOrEmpty(assistantResponse))
            {
                assistantResponse = "**There was an error processing the LLM's response.  Full response:";
                assistantResponse += responseText;
            }
            _androidLlm.RecentGeneratedText = assistantResponse;
            UpdateMessage(_lastSystemMessage, assistantResponse);
            InfoArea.text = "Presenting LLM response";
            _androidTts.Speak(assistantResponse);
        }

        private void OnLlmReturnGemini(string responseText)
        {
            string assistantResponse = responseText.Split("\nUser:")[0].Trim();
            if (string.IsNullOrEmpty(assistantResponse))
            {
                assistantResponse = "**There was an error processing the LLM's response.  Full response:";
                assistantResponse += responseText;
            }
            _androidLlm.RecentGeneratedText = assistantResponse;
            UpdateMessage(_lastSystemMessage, assistantResponse);
            InfoArea.text = "Presenting LLM response";
            _androidTts.Speak(assistantResponse);
        }
    }
}