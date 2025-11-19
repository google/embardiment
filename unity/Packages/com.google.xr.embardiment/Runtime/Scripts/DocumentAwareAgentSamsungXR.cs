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
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Google.XR.Embardiment
{
    public class DocumentAwareAgentSamsungXR : MonoBehaviour
    {
        public enum Condition { Baseline, FullContext, EyeTracking }

        private const float _fixationTimeThreshold = 0.120f;
        private const int _fixationWordBudget = 250;

        [InfoBox("To use:\n\n" +
            "1. Play the scene\n\n" +
            "2. Read various screens with eye gaze to fill the agent's context\n\n" +
            "3. Press a button TODO to record a message to send to Gemini" +
            "4. Hear the LLM's response read back out to you", EInfoBoxType.Normal)]
        public List<OcrTargetAndroid> Screens;
        public List<GameObject> LastGazedLines;

        private AndroidOcr _androidOcr;
        private AndroidAsr _androidAsr;
        private GeminiLlm _geminiLlm;
        private AndroidTts _androidTts;
        private float _fixationStart;
        private GameObject _fixationLineCandidate;
        private Condition _condition = Condition.Baseline;

        public void SetCondition(Int32 formCondition)
        {
            _condition = (Condition)formCondition;
            if (_condition != Condition.EyeTracking)
            {
                ClearTrackedData();
            }
        }

        public void ProcessRay(Ray ray)
        {
            if (_condition != Condition.EyeTracking)
            {
                return;
            }
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (RaycastHit hit in hits)
            {
                // Is a line:
                if (hit.collider.transform.parent != null && hit.collider.transform.parent.name.StartsWith("screen-"))
                {
                    GameObject line = hit.collider.gameObject;

                    bool newFixation = line != _fixationLineCandidate;
                    if (newFixation)
                    {
                        _fixationStart = Time.time;
                        _fixationLineCandidate = line;
                        return;
                    }

                    if (Time.time <= _fixationStart + _fixationTimeThreshold)
                    {
                        return;
                    }

                    if (!LastGazedLines.Contains(line))
                    {
                        line.GetComponent<LineRenderer>().enabled = true;
                        LastGazedLines.Add(line);
                    }

                    int totalWords = LastGazedLines.Sum(l => l.name.Split(' ').Length);

                    while (totalWords > _fixationWordBudget && LastGazedLines.Count > 0)
                    {
                        LastGazedLines[0].GetComponent<LineRenderer>().enabled = false;
                        totalWords -= LastGazedLines[0].name.Split(' ').Length;
                        LastGazedLines.RemoveAt(0);
                    }
                }
            }
        }

        private void Start()
        {
            _androidOcr = GetComponentInChildren<AndroidOcr>();

            foreach (OcrTargetAndroid screen in Screens)
            {
                screen.Texture = screen.GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
                _androidOcr.RecognizeText(screen.Texture, (ocrResponse) =>
                {
                    screen.OnOcrResultReceived(ocrResponse);
                });
            }

            _androidAsr = GetComponentInChildren<AndroidAsr>();
            _androidAsr.OnComplete.AddListener(OnAsrReturn);

            _geminiLlm = GetComponentInChildren<GeminiLlm>();
            _geminiLlm.OnComplete.AddListener(OnLlmReturn);

            _androidTts = GetComponentInChildren<AndroidTts>();
        }

        private void Update()
        {
            if (Pointer.current != null)
            {
                ProcessRay(Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue()));
            }
        }

        private void ClearTrackedData()
        {
            foreach (GameObject line in LastGazedLines)
            {
                line.GetComponent<LineRenderer>().enabled = false;
            }
            LastGazedLines.Clear();
        }

        private void OnAsrReturn(string recognizedText)
        {
            Debug.Log("Recording converted to text.  Sending to LLM...");
            string context = "";

            if (_condition == Condition.Baseline)
            {
                context = "Currently unavailable";
            }
            else if (_condition == Condition.FullContext)
            {
                context = "The user is looking at screens with the following text:";
                foreach (var screen in Screens)
                {
                    context += screen.OcrResponse.FullText;
                }
            }
            else if (_condition == Condition.EyeTracking)
            {
                if (LastGazedLines.Any())
                {
                    string gazedText = string.Join(" ", LastGazedLines.Select(line => line.name));
                    context = $"The user was recently looking at the following lines of text:\n\n{gazedText}";
                }
                else
                {
                    context = "The user's gaze has not been detected on any specific lines of text yet.";
                }
            }

            string request = $"User said: {recognizedText}\n\nContext: {context}";
            _geminiLlm.SendPrompt(request);
            ClearTrackedData();
        }

        private void OnLlmReturn(string responsePrompt)
        {
            _androidTts.Speak(responsePrompt);
        }
    }
}