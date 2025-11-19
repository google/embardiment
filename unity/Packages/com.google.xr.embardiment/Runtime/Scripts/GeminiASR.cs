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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Google.XR.Embardiment
{
    [ExecuteInEditMode]
    public class GeminiASR : MonoBehaviour
    {
        private static readonly string _geminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public AudioClip SourceAudio;
        public string RecentTranscription;
        public UnityEvent<string> OnComplete;

        private string _micName;
        private AudioClip _workingClip;
        private bool _verboseLogging = false;

        [Button]
        public void RequestRecognition()
        {
            RequestRecognition(null, null);
        }

        public void RequestRecognition(AudioClip newSourceAudio)
        {
            RequestRecognition(newSourceAudio, null);
        }

        public void RequestRecognition(AudioClip newSourceAudio, Action<string> invocationCallback)
        {
            if (newSourceAudio != null)
            {
                SourceAudio = newSourceAudio;
            }
            if (SourceAudio == null)
            {
                Debug.LogError("No source audio provided");
                return;
            }
            _ = RequestRecognitionAsync(SourceAudio, invocationCallback);
        }

        public void StartRecording()
        {
            if (_micName == null)
            {
                Debug.LogError("No mic found");
                return;
            }
            _workingClip = Microphone.Start(_micName, false, 300, 44100);
        }

        public void StopRecordingAndSend()
        {
            StopRecordingAndSend(null);
        }

        public void StopRecordingAndSend(Action<string> invocationCallback)
        {
            if (_micName == null)
            {
                return;
            }
            int lastSample = Microphone.GetPosition(_micName);
            if (lastSample == 0)
            {
                Debug.LogError("Error - no audio samples collected");
                return;
            }
            Microphone.End(_micName);

            float[] samples = new float[lastSample * _workingClip.channels];
            _workingClip.GetData(samples, 0);

            AudioClip clipToSend = AudioClip.Create("Recorded Clip", lastSample, _workingClip.channels, _workingClip.frequency, false);
            clipToSend.SetData(samples, 0);

            RequestRecognition(clipToSend, invocationCallback);
        }

        private void OnEnable()
        {
            _micName = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        }

        private async Task RequestRecognitionAsync(AudioClip newSourceAudio, Action<string> invocationCallback)
        {
            try
            {
                string clipFilePath = Path.Combine(Application.persistentDataPath, "temp_asr.wav");

                float[] samples = new float[SourceAudio.samples * SourceAudio.channels];
                SourceAudio.GetData(samples, 0);

                using (FileStream fs = new FileStream(clipFilePath, FileMode.Create))
                {
                    byte[] header;
                    int byteCount = SourceAudio.samples * SourceAudio.channels * 2; // 16-bit samples
                    using (var stream = new MemoryStream())
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                        writer.Write(36 + byteCount);
                        writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                        writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                        writer.Write(16); // PCM chunk size
                        writer.Write((short)1); // Audio format 1=PCM
                        writer.Write((short)SourceAudio.channels);
                        writer.Write(SourceAudio.frequency);
                        writer.Write(SourceAudio.frequency * SourceAudio.channels * 2);
                        writer.Write((short)(SourceAudio.channels * 2));
                        writer.Write((short)16); // Bits per sample
                        writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                        writer.Write(byteCount);
                        header = stream.ToArray();
                    }
                    fs.Write(header, 0, header.Length);

                    byte[] byteData = new byte[samples.Length * 2];
                    int rescaleFactor = 32767;

                    for (int i = 0; i < samples.Length; i++)
                    {
                        short temp = (short)(samples[i] * rescaleFactor);
                        byte[] byteArr = BitConverter.GetBytes(temp);
                        byteArr.CopyTo(byteData, i * 2);
                    }

                    fs.Write(byteData, 0, byteData.Length);
                }
                string uploadUrl = await GetUploadUrl(clipFilePath);
                string audioUri = await UploadAudio(clipFilePath, uploadUrl);
                RecentTranscription = await TranscribeAudio(audioUri);
                invocationCallback?.Invoke(RecentTranscription);
                OnComplete?.Invoke(RecentTranscription);
            }
            catch (Exception e)
            {
                Debug.LogError("Error during Speech Recognition: " + e.Message);
            }
        }

        private async Task<string> GetUploadUrl(string clipFilePath)
        {
            if (GeminiKey.Instance == null)
            {
                Debug.LogError("Error: no API key.  Please consult readme for instructions");
                return null;
            }
            using (UnityWebRequest request = new UnityWebRequest("https://generativelanguage.googleapis.com/upload/v1beta/files", "POST"))
            {
                FileInfo fileInfo = new FileInfo(clipFilePath);

                var metadata = new { file = new { display_name = "MyAudioFile" } };
                string jsonBody = JsonUtility.ToJson(metadata);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("x-goog-api-key", GeminiKey.Instance.Key);
                request.SetRequestHeader("X-Goog-Upload-Protocol", "resumable");
                request.SetRequestHeader("X-Goog-Upload-Command", "start");
                request.SetRequestHeader("X-Goog-Upload-Header-Content-Length", $"{fileInfo.Length}");
                request.SetRequestHeader("X-Goog-Upload-Header-Content-Type", "audio/wav");
                request.SetRequestHeader("Content-Type", "application/json");

                if (_verboseLogging) Debug.Log("Sending Recognition Request");
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (_verboseLogging) Debug.LogError("Error: " + request.error);
                    return null;
                }
                else
                {
                    return request.GetResponseHeader("x-goog-upload-url");
                }
            }
        }

        private async Task<string> UploadAudio(string clipFilePath, string uploadUrl)
        {
            using (UnityWebRequest request = new UnityWebRequest(uploadUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerFile(clipFilePath);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("X-Goog-Upload-Offset", "0");
                request.SetRequestHeader("X-Goog-Upload-Command", "upload, finalize");

                if (_verboseLogging) Debug.Log("Uploading Audio");
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (_verboseLogging) Debug.Log("Error: " + request.error);
                    return null;
                }
                else
                {
                    JObject response = JObject.Parse(request.downloadHandler.text);
                    return (string)response["file"]["uri"];
                }
            }
        }

        private async Task<string> TranscribeAudio(string audioUrl)
        {
            JObject postData = new JObject();
            postData["contents"] = new JArray { new JObject() };
            postData["contents"][0]["parts"] = new JArray { new JObject(), new JObject() };
            postData["contents"][0]["parts"][0]["text"] = "Transcribe the audio in this clip";
            postData["contents"][0]["parts"][1]["file_data"] = new JObject();
            postData["contents"][0]["parts"][1]["file_data"]["mime_type"] = "audio/wav";
            postData["contents"][0]["parts"][1]["file_data"]["file_uri"] = audioUrl;

            string postDataString = JsonConvert.SerializeObject(postData);
            byte[] postDataRaw = Encoding.UTF8.GetBytes(postDataString);

            using (UnityWebRequest request = new UnityWebRequest(_geminiEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(postDataRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", GeminiKey.Instance.Key);

                if (_verboseLogging) Debug.Log("Sending ASR Request");
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (_verboseLogging) Debug.Log("Error: " + request.error);
                    if (_verboseLogging) Debug.Log("Error: " + request.downloadHandler.text);
                    return null;
                }
                else
                {
                    JObject response = JObject.Parse(request.downloadHandler.text);
                    JObject firstCandidate = (JObject)response["candidates"][0];
                    return (string)firstCandidate["content"]["parts"][0]["text"];
                }
            }
        }
    }
}
