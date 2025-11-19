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
using System.Text;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Google.XR.Embardiment
{
    public class DocumentAwareAgentDesktop : MonoBehaviour
    {
        [InfoBox("To use:\n\n" +
            "1. Play the scene\n\n" +
            "2. Click and drag your mouse in the game window to fill the agent's context\n\n" +
            "3. Hold down space to ask a question, and release to send" +
            "4. Review LLM's response in console", EInfoBoxType.Normal)]
        public OcrTargetDesktop LastGazedScreen;
        public List<OcrTargetDesktop> Screens;
        public List<string> LastGazedWords;
        public int WordLimit = 20;

        private DesktopOcr _desktopOcr;
        private GeminiASR _geminiAsr;
        private GeminiLlm _geminiLlm;

        private void Start()
        {
            _desktopOcr = GetComponentInChildren<DesktopOcr>();

            foreach (OcrTargetDesktop screen in Screens)
            {
                screen.Texture = screen.GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
                _desktopOcr.RecognizeText(screen.Texture, (ocrResponse) =>
                {
                    screen.SpawnWordBoxes(ocrResponse);
                });
            }

            _geminiAsr = GetComponentInChildren<GeminiASR>();
            _geminiAsr.OnComplete.AddListener(OnAsrReturn);

            _geminiLlm = GetComponentInChildren<GeminiLlm>();
            _geminiLlm.OnComplete.AddListener(OnLlmReturn);
        }

        private void Update()
        {
            if (Pointer.current.press.isPressed)
            {
                Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
                RaycastHit[] hits = Physics.RaycastAll(ray);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.name.StartsWith("screen-"))
                    {
                        LastGazedScreen = hit.collider.gameObject.GetComponent<OcrTargetDesktop>();
                    }
                    if (hit.collider.transform.parent != null && hit.collider.transform.parent.name.StartsWith("screen-"))
                    {
                        string word = hit.collider.gameObject.name;
                        if (LastGazedWords.Count == 0 || !LastGazedWords.TakeLast(5).Contains(word))
                        {
                            LastGazedWords.Add(word);
                        }
                        if (LastGazedWords.Count > WordLimit)
                        {
                            LastGazedWords.RemoveAt(0);
                        }
                    }
                }
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _geminiAsr.StartRecording();
                Debug.Log("Starting a recording for Gemini");
            }
            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            {
                _geminiAsr.StopRecordingAndSend();
                Debug.Log("Saving and sending recording...");
            }
        }

        private void OnAsrReturn(string recognizedText)
        {
            Debug.Log("Recording converted to text.  Sending to LLM...");
            StringBuilder context = new();
            if (LastGazedScreen == null)
            {
                context.AppendLine("A user was looking around, but at what you are not quite sure.");
            }
            else
            {
                context.Append("A user is looking at a screen.  The screen has the following text on it (as gathered from an OCR):\n\n");
                context.Append(LastGazedScreen.OcrResponse.FullText);
                if (LastGazedWords.Count > 0)
                {
                    context.Append("\n\n");
                    context.Append($"The last {LastGazedWords.Count} words that the user looked (in order from oldest to newest) were: ");
                    context.Append(string.Join(", ", LastGazedWords));
                }
            }
            StringBuilder request = new();
            request.Append("User said: ");
            request.AppendLine(recognizedText);
            request.Append("Context: ");
            request.Append(context.ToString());
            _geminiLlm.SendPrompt(request.ToString());
        }

        private void OnLlmReturn(string responsePrompt)
        {
            Debug.Log(responsePrompt);
        }
    }
}
