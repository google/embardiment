// Copyright 2025 The Embardiment Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Google.XR.Embardiment
{
    public class AndroidOcr : MonoBehaviour
    {
        #region Data Structures
        [System.Serializable]
        public struct OcrResponse
        {
            [JsonProperty("fullText")]
            public string FullText;
            [JsonProperty("textBlocks")]
            public TextBlock[] TextBlocks;
        }

        [System.Serializable]
        public struct TextBlock
        {
            [JsonProperty("text")]
            public string Text;
            [JsonProperty("boundingBox")]
            public BoundingBox BoundingBox;
            [JsonProperty("lines")]
            public Line[] Lines;
        }

        [System.Serializable]
        public struct Line
        {
            [JsonProperty("text")]
            public string Text;
            [JsonProperty("boundingBox")]
            public BoundingBox BoundingBox;
            [JsonProperty("elements")]
            public Element[] Elements;
        }

        [System.Serializable]
        public struct Element
        {
            [JsonProperty("text")]
            public string Text;
            [JsonProperty("boundingBox")]
            public BoundingBox BoundingBox;
        }

        [System.Serializable]
        public struct BoundingBox
        {
            [JsonProperty("w")]
            public int Width;
            [JsonProperty("h")]
            public int Height;
            [JsonProperty("x")]
            public int X;
            [JsonProperty("y")]
            public int Y;
        }
        #endregion

        class OcrCallbackProxy : AndroidJavaProxy
        {
            private readonly Action<OcrResponse> _onSuccess;
            private readonly Action<string> _onFailure;

            public OcrCallbackProxy(Action<OcrResponse> onSuccess, Action<string> onFailure)
                : base("com.google.xr.embardiment.ocr.OcrBridge$OcrCallback")
            {
                _onSuccess = onSuccess;
                _onFailure = onFailure;
            }

            public void onSuccess(string jsonResult)
            {
                // Deserialize the JSON string from Kotlin into our C# structs.
                OcrResponse response = JsonConvert.DeserializeObject<OcrResponse>(jsonResult);
                _onSuccess?.Invoke(response);
            }

            public void onFailure(string errorMessage)
            {
                _onFailure?.Invoke(errorMessage);
            }
        }

        public Texture2D SourceTexture;
        public OcrResponse RecentOcrResult;
        public UnityEvent<OcrResponse> OnComplete;

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
                Debug.LogError("No Source Texture was provided.");
                return;
            }

            if (Application.platform != RuntimePlatform.Android)
            {
                Debug.LogWarning("AndroidOCR is intended for the Android platform. Bypassing native call.");
                return;
            }

            byte[] imageData = SourceTexture.EncodeToJPG();
            int rotation = 0;

            string className = "com.google.xr.embardiment.ocr.OcrBridge";
            using (var bridge = new AndroidJavaObject(className))
            {
                var callback = new OcrCallbackProxy(
                    (response) =>
                    {
                        RecentOcrResult = response;
                        invocationCallback?.Invoke(response);
                        OnComplete?.Invoke(response);
                    },
                    (error) =>
                    {
                        Debug.LogError("OCR Failure: " + error);
                    }
                );

                bridge.Call("processImage", imageData, rotation, callback);
            }
        }
    }
}