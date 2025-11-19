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

using System.Collections;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Google.XR.Embardiment
{
    public class CameraOcr : MonoBehaviour
    {
        [InfoBox("To use:\n\n" +
            "1. Run app and approve camera access\n\n" +
            "2. Tap screen\n\n" +
            "3. View OCR on screen", EInfoBoxType.Normal)]
        public TextMeshProUGUI InfoArea;
        private WebCamTexture _webCamTexture;
        private RenderTexture _renderTexture;
        private bool _isCameraReady = false;
        private AndroidOcr _androidOcr;
        private RawImage _rawImage;
        private float _previousRotation = 0;

        private IEnumerator Start()
        {
            InfoArea = GetComponentInChildren<TextMeshProUGUI>();
            InfoArea.text = "Requesting camera access...";
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                InfoArea.text = "Webcam access denied.";
                yield break;
            }

            _webCamTexture = new WebCamTexture();
            _rawImage = GetComponentInChildren<RawImage>();
            _rawImage.texture = _webCamTexture;
            _webCamTexture.Play();

            yield return new WaitUntil(() => _webCamTexture.width > 100);

            _renderTexture = new RenderTexture(_webCamTexture.width, _webCamTexture.height, 0);
            _isCameraReady = true;
            InfoArea.text = "Webcam is ready.  Tap to start OCR.";
            _androidOcr = GetComponentInChildren<AndroidOcr>();
            _androidOcr.OnComplete.AddListener(UponCompletion);
        }

        private void Update()
        {
            if (_isCameraReady)
            {
                if (_webCamTexture.videoRotationAngle != _previousRotation)
                {
                    _rawImage.rectTransform.localEulerAngles = new Vector3(0, 0, -_webCamTexture.videoRotationAngle);
                    _rawImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)_webCamTexture.width / (float)_webCamTexture.height;
                    _previousRotation = _webCamTexture.videoRotationAngle;
                }
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    InfoArea.text = "Tap detected.  Sending image to OCR";
                    Graphics.Blit(_webCamTexture, _renderTexture);
                    Texture2D textureForOcr = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, false);
                    RenderTexture.active = _renderTexture;
                    textureForOcr.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
                    textureForOcr.Apply();
                    RenderTexture.active = null;
                    _androidOcr.RecognizeText(textureForOcr);
                }
            }
        }

        private void UponCompletion(AndroidOcr.OcrResponse response)
        {
            InfoArea.text = "OCR complete.  Found the following text:\n\n" + response.FullText;

            foreach (var block in response.TextBlocks)
            {
                Debug.Log("Block: " + block.Text);
                foreach (var line in block.Lines)
                {
                    Debug.Log("  Line: " + line.Text);
                }
            }
        }
    }
}