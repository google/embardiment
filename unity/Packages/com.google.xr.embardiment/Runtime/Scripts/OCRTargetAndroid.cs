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

using System.Collections.Concurrent;
using UnityEngine;

namespace Google.XR.Embardiment
{
    public class OcrTargetAndroid : MonoBehaviour
    {
        private static GameObject _lineBoxTemplate;
        private static GameObject _wordBoxTemplate;

        public Texture2D Texture;
        public AndroidOcr.OcrResponse OcrResponse;

        private readonly ConcurrentQueue<AndroidOcr.OcrResponse> _ocrResultsQueue = new();

        public void OnOcrResultReceived(AndroidOcr.OcrResponse result)
        {
            _ocrResultsQueue.Enqueue(result);
        }

        private void Awake()
        {
            if (_lineBoxTemplate == null)
            {
                _lineBoxTemplate = transform.parent.Find("LineBox Template").gameObject;
            }
            if (_wordBoxTemplate == null)
            {
                _wordBoxTemplate = transform.parent.Find("WordBox Template").gameObject;
            }
        }

        private void Update()
        {
            if (_ocrResultsQueue.TryDequeue(out AndroidOcr.OcrResponse result))
            {
                ProcessOcrResult(result);
            }
        }

        private void ProcessOcrResult(AndroidOcr.OcrResponse result)
        {
            OcrResponse = result;
            float screenW = Texture.width;
            float screenH = Texture.height;

            foreach (AndroidOcr.TextBlock textBlock in result.TextBlocks)
            {
                foreach (AndroidOcr.Line line in textBlock.Lines)
                {
                    GameObject lineBoxObject = Instantiate(_lineBoxTemplate, transform);
                    float lineX = line.BoundingBox.X;
                    float lineY = line.BoundingBox.Y;
                    float lineW = line.BoundingBox.Width;
                    float lineH = line.BoundingBox.Height;

                    float w = lineW / screenW;
                    float h = lineH / screenH;
                    float x = (lineX / screenW) - 0.5f + (w * 0.5f);
                    float y = (lineY / screenH) - 0.5f + (h * 0.5f);
                    lineBoxObject.transform.localPosition = new Vector3(-x, -y, 0);
                    lineBoxObject.transform.localScale = new Vector3(w, h, 1);
                    lineBoxObject.name = line.Text;
                    lineBoxObject.SetActive(true);

                    foreach (AndroidOcr.Element word in line.Elements)
                    {
                        GameObject wordBoxObject = Instantiate(_wordBoxTemplate, transform);

                        float wordX = word.BoundingBox.X;
                        float wordY = word.BoundingBox.Y;
                        float wordW = word.BoundingBox.Width;
                        float wordH = word.BoundingBox.Height;

                        w = wordW / screenW;
                        h = wordH / screenH;
                        x = (wordX / screenW) - 0.5f + (w * 0.5f);
                        y = (wordY / screenH) - 0.5f + (h * 0.5f);
                        wordBoxObject.transform.localPosition = new Vector3(-x, -y, 0);
                        wordBoxObject.transform.localScale = new Vector3(w, h, 1);
                        wordBoxObject.name = word.Text;
                        wordBoxObject.SetActive(true);
                        wordBoxObject.transform.SetParent(lineBoxObject.transform);
                    }
                }
            }
        }
    }
}