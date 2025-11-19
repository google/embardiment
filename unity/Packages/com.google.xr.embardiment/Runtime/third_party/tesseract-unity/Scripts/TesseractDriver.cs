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
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using UnityEditor;

public class TesseractDriver
{
    private TesseractWrapper _tesseract;
    private static readonly List<string> fileNames = new List<string> {"tessdata.tgz"};
    private bool _shouldDebug = false;

    public string CheckTessVersion()
    {
        _tesseract = new TesseractWrapper();

        try
        {
            string version = "Tesseract version: " + _tesseract.Version();
            Debug.Log(version);
            return version;
        }
        catch (Exception e)
        {
            string errorMessage = e.GetType() + " - " + e.Message;
            Debug.LogError("Tesseract version: " + errorMessage);
            return errorMessage;
        }
    }

    public void Setup(UnityAction onSetupComplete)
    {
#if UNITY_EDITOR
        OcrSetup(onSetupComplete);
#elif UNITY_ANDROID
        CopyAllFilesToPersistentData(fileNames, onSetupComplete);
#else
        OcrSetup(onSetupComplete);
#endif
    }

    public void OcrSetup(UnityAction onSetupComplete)
    {
        _tesseract = new TesseractWrapper();

#if UNITY_EDITOR
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForPackageName("com.google.xr.embardiment").resolvedPath;
        string datapath = Path.Combine(packageRoot, "Runtime", "third_party", "tesseract-unity", "StreamingAssets", "tessdata");
#elif UNITY_ANDROID
        string datapath = Application.persistentDataPath + "/tessdata/";
#else
        string datapath = Path.Combine(Application.streamingAssetsPath, "tessdata");
#endif
        if (_tesseract.Init("eng", datapath))
        {
            if (_shouldDebug) {
                Debug.Log("Init Successful");
            }
            onSetupComplete?.Invoke();
        }
        else
        {
            Debug.LogError(_tesseract.GetErrorMessage());
        }
    }

    private async void CopyAllFilesToPersistentData(List<string> fileNames, UnityAction onSetupComplete)
    {
        String fromPath = "jar:file://" + Application.dataPath + "!/assets/";
        String toPath = Application.persistentDataPath + "/";

        foreach (String fileName in fileNames)
        {
            if (!File.Exists(toPath + fileName))
            {
                Debug.Log("Copying from " + fromPath + fileName + " to " + toPath);
                
                using (UnityWebRequest request = UnityWebRequest.Get(fromPath + fileName))
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(toPath + fileName, request.downloadHandler.data);
                        Debug.Log("File copy done");
                    }
                    else
                    {
                        Debug.LogError("Failed to copy file: " + request.error);
                    }
                }
            }
            else
            {
                Debug.Log("File exists! " + toPath + fileName);
            }

            UnZipData(fileName);
        }

        OcrSetup(onSetupComplete);
    }

    public string GetErrorMessage()
    {
        return _tesseract?.GetErrorMessage();
    }

    public JObject Recognize(Texture2D imageToRecognize)
    {
        return _tesseract.Recognize(imageToRecognize);
    }

    public Task<JObject> RecognizeAsync(Color32[] colors, int width, int height)
    {
        return Task.Run(() => _tesseract.Recognize(colors, width, height));
    }

    public Texture2D GetHighlightedTexture()
    {
        return _tesseract.GetHighlightedTexture();
    }

    private void UnZipData(string fileName)
    {
        if (File.Exists(Application.persistentDataPath + "/" + fileName))
        {
            UnZipUtil.ExtractTGZ(Application.persistentDataPath + "/" + fileName, Application.persistentDataPath);
            Debug.Log("UnZipping Done");
        }
        else
        {
            Debug.LogError(fileName + " not found!");
        }
    }
}
