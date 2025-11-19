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

using UnityEditor;
using UnityEngine;

namespace Google.XR.Embardiment
{
    [InitializeOnLoad]
    public class GeminiKeyValidator
    {
        static GeminiKeyValidator()
        {
            EditorApplication.delayCall += ValidateGeminiKeyLocation;
        }

        private static void ValidateGeminiKeyLocation()
        {
            string[] guids = AssetDatabase.FindAssets("t:GeminiKey");
            if (guids.Length == 0)
            {
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (!path.Contains("/Resources/"))
            {
                Debug.LogError("The GeminiKey asset must be in a 'Resources' folder to work correctly. Current path: " + path);
            }

            if (guids.Length > 1)
            {
                Debug.LogWarning("Multiple GeminiKey assets found. The system will only load the first one it finds named 'GeminiKey'.");
            }
        }
    }
}