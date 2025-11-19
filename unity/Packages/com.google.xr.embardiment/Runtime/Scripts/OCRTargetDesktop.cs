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
    public class OcrTargetDesktop : MonoBehaviour
    {
        public Texture2D Texture;
        public DesktopOcr.OcrResponse OcrResponse;

        public void SpawnWordBoxes(DesktopOcr.OcrResponse result)
        {
            OcrResponse = result;
            GameObject wordBoxTemplate = transform.Find("WordBox Template").gameObject;

            float screenW = Texture.width;
            float screenH = Texture.height;

            foreach (DesktopOcr.WordBox wordBox in result.WordBoxes)
            {
                GameObject wordBoxObject = Instantiate(wordBoxTemplate, transform);
                float sourceX = wordBox.X;
                float sourceY = wordBox.Y;
                float sourceW = wordBox.Width;
                float sourceH = wordBox.Height;

                float w = sourceW / screenW;
                float h = sourceH / screenH;
                float x = (sourceX / screenW) - 0.5f + (w * 0.5f);
                float y = (sourceY / screenH) - 0.5f + (h * 0.5f);

                wordBoxObject.transform.localPosition = new Vector3(-x, -y, 0);
                wordBoxObject.transform.localScale = new Vector3(w, h, 1);
                wordBoxObject.name = wordBox.Word;
                wordBoxObject.SetActive(true);
            }
        }
    }
}