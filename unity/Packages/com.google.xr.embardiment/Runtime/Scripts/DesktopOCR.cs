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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Google.XR.Embardiment
{
    [ExecuteInEditMode]
    public class DesktopOcr : MonoBehaviour
    {
        [System.Serializable]
        public struct OcrResponse
        {
            [JsonProperty("fullText")]
            public string FullText;
            [JsonProperty("wordBoxes")]
            public WordBox[] WordBoxes;
        }

        [System.Serializable]
        public struct WordBox
        {
            [JsonProperty("word")]
            public string Word;
            [JsonProperty("w")]
            public int Width;
            [JsonProperty("h")]
            public int Height;
            [JsonProperty("x")]
            public int X;
            [JsonProperty("y")]
            public int Y;
        }

        public UnityEvent<OcrResponse> OnComplete;
        public bool UseCache = true;
        [ShowAssetPreview]
        public Texture2D SourceTexture;
        public OcrResponse RecentOcrResult;

        private TesseractDriver _tesseractDriver;
        private bool _tesseractIsSetup = false;
        private Dictionary<string, OcrResponse> _cachedResults;
        private string _cachePath;

        private Queue<(Texture2D Texture, Action<OcrResponse> Callback)> _ocrQueue = new Queue<(Texture2D, Action<OcrResponse>)>();
        private bool _isProcessing = false;

        [Button]
        public void RecognizeText()
        {
            RecognizeText(null, null);
        }

        public void RecognizeText(Texture2D newSourceTexture)
        {
            RecognizeText(newSourceTexture, null);
        }

        public void RecognizeText(Texture2D newSourceTexture, Action<OcrResponse> invocationCallback)
        {
            if (newSourceTexture != null)
            {
                SourceTexture = newSourceTexture;
            }
            if (SourceTexture == null)
            {
                Debug.LogError("No source texture provided");
                return;
            }
            if (UseCache)
            {
                string cacheId = CalculateCacheId(SourceTexture);
                if (_cachedResults != null && _cachedResults.ContainsKey(cacheId))
                {
                    RecentOcrResult = _cachedResults[cacheId];
                    invocationCallback?.Invoke(RecentOcrResult);
                    OnComplete?.Invoke(RecentOcrResult);
                    return;
                }
            }

            if (_tesseractIsSetup == false)
            {
                Debug.LogError("Tesseract isn't yet set up");
                return;
            }

            _ocrQueue.Enqueue((this.SourceTexture, invocationCallback));
            if (_ocrQueue.Count > 1)
            {
                Debug.Log($"Request queued behind {_ocrQueue.Count - 1} others");
            }

            if (!_isProcessing)
            {
                ProcessQueueAsync();
            }
        }

        [Button]
        public void ClearCache()
        {
            if (UseCache && File.Exists(_cachePath))
            {
                File.Delete(_cachePath);
                _cachedResults = new Dictionary<string, OcrResponse>();
            }
        }

        private void OnEnable()
        {
            if (UseCache)
            {
                _cachePath = Path.Combine(Application.persistentDataPath, "ocr_results_cache.json");
                try
                {
                    var json = File.ReadAllText(_cachePath);
                    _cachedResults = JsonConvert.DeserializeObject<Dictionary<string, OcrResponse>>(json);
                    if (_cachedResults == null) _cachedResults = new Dictionary<string, OcrResponse>();
                }
                catch
                {
                    _cachedResults = new Dictionary<string, OcrResponse>();
                }
            }
            if (_tesseractDriver == null)
            {
                _tesseractDriver = new TesseractDriver();
                _tesseractDriver.Setup(() => _tesseractIsSetup = true);
            }
        }

        private string CalculateCacheId(Texture2D texture)
        {
            byte[] textureBytes = texture.GetRawTextureData();
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(textureBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private async void ProcessQueueAsync()
        {
            _isProcessing = true;
            while (_ocrQueue.Count > 0)
            {
                (Texture2D Texture, Action<OcrResponse> Callback) requestToProcess = _ocrQueue.Dequeue();
                await RunTesseractAsync(requestToProcess);
            }
            _isProcessing = false;
        }

        private async Task RunTesseractAsync((Texture2D Texture, Action<OcrResponse> Callback) request)
        {
            Texture2D textureToProcess = request.Texture;
            int width = textureToProcess.width;
            int height = textureToProcess.height;
            Color32[] colors = textureToProcess.GetPixels32();

            Debug.Log("Performing OCR analysis (cache disabled or not found)");
            JObject result = await _tesseractDriver.RecognizeAsync(colors, width, height);
            RecentOcrResult = result.ToObject<OcrResponse>();

            if (UseCache)
            {
                string cacheId = CalculateCacheId(textureToProcess);
                _cachedResults[cacheId] = RecentOcrResult;
                var json = JsonConvert.SerializeObject(_cachedResults, Formatting.Indented);
                File.WriteAllText(_cachePath, json);
            }

            request.Callback?.Invoke(RecentOcrResult);
            OnComplete?.Invoke(RecentOcrResult);
        }
    }
}
