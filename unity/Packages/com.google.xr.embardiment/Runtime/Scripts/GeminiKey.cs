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

namespace Google.XR.Embardiment
{
    [CreateAssetMenu(fileName = "GeminiKey", menuName = "Scriptable Objects/Gemini Key")]
    public class GeminiKey : ScriptableObject
    {
        private static GeminiKey _instance;

        public static GeminiKey Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GeminiKey>("GeminiKey");

                    if (_instance == null)
                    {
                        Debug.LogError("GeminiKey asset not found in a Resources folder. Please create one and name it 'GeminiKey'.");
                    }
                }
                return _instance;
            }
        }

        public string Key;
    }
}