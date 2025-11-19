// *** NOTE: This file is part of a third-party package, licensed under
// the Apache License, Version 2.0. The original version of this file
// did not contain a per-file copyright header. This file has been
// modified by Google LLC. ***
//
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
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class TesseractWrapper
{
#if UNITY_EDITOR
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#elif UNITY_ANDROID
    private const string TesseractDllName = "libtesseract.so";
    private const string LeptonicaDllName = "liblept.so";
#else
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#endif

    private IntPtr _tessHandle;
    private Texture2D _highlightedTexture;
    private string _errorMsg;
    private const float MinimumConfidence = 60;
    private bool _shouldDebug = false;
    private bool _shouldDrawLines = false;

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessVersion();

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPICreate();

    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIInit3(IntPtr handle, string dataPath, string language);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIDelete(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage(IntPtr handle, IntPtr imagedata, int width, int height,
        int bytes_per_pixel, int bytes_per_line);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage2(IntPtr handle, IntPtr pix);

    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIRecognize(IntPtr handle, IntPtr monitor);

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessDeleteText(IntPtr text);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIEnd(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIClear(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetWords(IntPtr handle, IntPtr pixa);
    
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIAllWordConfidences(IntPtr handle);

    public TesseractWrapper()
    {
        _tessHandle = IntPtr.Zero;
    }

    public string Version()
    {
        IntPtr strPtr = TessVersion();
        string tessVersion = Marshal.PtrToStringAnsi(strPtr);
        return tessVersion;
    }

    public string GetErrorMessage()
    {
        return _errorMsg;
    }

    public bool Init(string lang, string dataPath)
    {
        if (!_tessHandle.Equals(IntPtr.Zero))
            Close();

        try
        {
            _tessHandle = TessBaseAPICreate();
            if (_tessHandle.Equals(IntPtr.Zero))
            {
                _errorMsg = "TessAPICreate failed";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dataPath))
            {
                _errorMsg = "Invalid DataPath";
                return false;
            }

            int init = TessBaseAPIInit3(_tessHandle, dataPath, lang);
            if (init != 0)
            {
                Close();
                _errorMsg = "TessAPIInit failed. Output: " + init;
                return false;
            }
        }
        catch (Exception ex)
        {
            _errorMsg = ex + " -- " + ex.Message;
            return false;
        }

        return true;
    }

    public JObject Recognize(Texture2D texture) {
        _highlightedTexture = texture;

        int width = _highlightedTexture.width;
        int height = _highlightedTexture.height;
        Color32[] colors = _highlightedTexture.GetPixels32();
        return Recognize(colors, width, height);
    }
    public JObject Recognize(Color32[] colors, int width, int height) {
        if (_tessHandle.Equals(IntPtr.Zero))
            return null;

        int count = width * height;
        int bytesPerPixel = 4;
        byte[] dataBytes = new byte[count * bytesPerPixel];
        int bytePtr = 0;

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                int colorIdx = y * width + x;
                dataBytes[bytePtr++] = colors[colorIdx].r;
                dataBytes[bytePtr++] = colors[colorIdx].g;
                dataBytes[bytePtr++] = colors[colorIdx].b;
                dataBytes[bytePtr++] = colors[colorIdx].a;
            }
        }

        IntPtr imagePtr = Marshal.AllocHGlobal(count * bytesPerPixel);
        Marshal.Copy(dataBytes, 0, imagePtr, count * bytesPerPixel);

        TessBaseAPISetImage(_tessHandle, imagePtr, width, height, bytesPerPixel, width * bytesPerPixel);

        if (TessBaseAPIRecognize(_tessHandle, IntPtr.Zero) != 0)
        {
            Marshal.FreeHGlobal(imagePtr);
            return null;
        }
        
        IntPtr confidencesPointer = TessBaseAPIAllWordConfidences(_tessHandle);
        int i = 0;
        List<int> confidence = new List<int>();
        
        while (true)
        {
            int tempConfidence = Marshal.ReadInt32(confidencesPointer, i * 4);

            if (tempConfidence == -1) break;

            i++;
            confidence.Add(tempConfidence);
        }

        int pointerSize = Marshal.SizeOf(typeof(IntPtr));
        IntPtr intPtr = TessBaseAPIGetWords(_tessHandle, IntPtr.Zero);
        Boxa boxa = Marshal.PtrToStructure<Boxa>(intPtr);
        Box[] boxes = new Box[boxa.n];

        for (int index = 0; index < boxes.Length; index++)
        {
            if (confidence[index] >= MinimumConfidence)
            {
                IntPtr boxPtr = Marshal.ReadIntPtr(boxa.box, index * pointerSize);
                boxes[index] = Marshal.PtrToStructure<Box>(boxPtr);
                Box box = boxes[index];
                if (_highlightedTexture != null) {
                    DrawLines(_highlightedTexture,
                        new Rect(box.x, _highlightedTexture.height - box.y - box.h, box.w, box.h),
                        Color.green);
                }
            }
        }

        IntPtr stringPtr = TessBaseAPIGetUTF8Text(_tessHandle);
        Marshal.FreeHGlobal(imagePtr);
        if (stringPtr.Equals(IntPtr.Zero))
            return null;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string recognizedText = Marshal.PtrToStringAnsi (stringPtr);
#else
        string recognizedText = Marshal.PtrToStringAuto(stringPtr);
#endif

        TessBaseAPIClear(_tessHandle);
        TessDeleteText(stringPtr);
        
        string[] words = recognizedText.Split(new[] {' ', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder fullText = new StringBuilder();
        JArray wordBoxCombos = new JArray();

        for (i = 0; i < boxes.Length; i++)
        {
            if (_shouldDebug)
            {
                Debug.Log(words[i] + " -> " + confidence[i]);
            }
            if (confidence[i] >= MinimumConfidence)
            {
                fullText.Append(words[i] + " ");
                JObject thisWord = new JObject
                {
                    ["x"] = boxes[i].x,
                    ["y"] = boxes[i].y,
                    ["w"] = boxes[i].w,
                    ["h"] = boxes[i].h,
                    ["word"] = words[i]
                };
                wordBoxCombos.Add(thisWord);
            }
        }
        return new JObject
        {
            ["fullText"] = fullText.ToString(),
            ["wordBoxes"] = wordBoxCombos
        };
    }

    private void DrawLines(Texture2D texture, Rect boundingRect, Color color, int thickness = 3)
    {
        if (!_shouldDrawLines) {
            return;
        }
        int x1 = (int) boundingRect.x;
        int x2 = (int) (boundingRect.x + boundingRect.width);
        int y1 = (int) boundingRect.y;
        int y2 = (int) (boundingRect.y + boundingRect.height);

        for (int x = x1; x <= x2; x++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x, y1 + i, color);
                texture.SetPixel(x, y2 - i, color);
            }
        }

        for (int y = y1; y <= y2; y++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x1 + i, y, color);
                texture.SetPixel(x2 - i, y, color);
            }
        }

        texture.Apply();
    }

    public Texture2D GetHighlightedTexture()
    {
        return _highlightedTexture;
    }

    public void Close()
    {
        if (_tessHandle.Equals(IntPtr.Zero))
            return;
        TessBaseAPIEnd(_tessHandle);
        TessBaseAPIDelete(_tessHandle);
        _tessHandle = IntPtr.Zero;
    }
}
