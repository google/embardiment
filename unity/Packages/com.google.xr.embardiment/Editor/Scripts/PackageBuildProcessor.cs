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

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Google.XR.Embardiment
{
    public class PackageBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string _packageName = "com.google.xr.embardiment";
        private const string _packageStreamingAssetsPath = "Runtime/third_party/tesseract-unity/StreamingAssets";
        private const string _dataSubFolder = "tessdata";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string sourcePath = Path.Combine(GetPackageFullPath(), _packageStreamingAssetsPath, _dataSubFolder);
            string destinationPath = Path.Combine(Application.streamingAssetsPath, _dataSubFolder);

            if (!Directory.Exists(sourcePath))
            {
                Debug.LogWarning($"Tesseract data not found in package at '{sourcePath}'. It will not be included in the build.");
                return;
            }

            // Clean up any previous copies before starting.
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            Debug.Log($"Copying '{sourcePath}' to '{destinationPath}' for build.");
            CopyDirectory(sourcePath, destinationPath);
            AssetDatabase.Refresh();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            string copiedDataPath = Path.Combine(Application.streamingAssetsPath, _dataSubFolder);

            if (Directory.Exists(copiedDataPath))
            {
                Directory.Delete(copiedDataPath, true);
                AssetDatabase.Refresh();
                Debug.Log("Cleaned up copied Tesseract data from StreamingAssets.");
                CleanupEmptyDirectories(Application.streamingAssetsPath);
            }
        }

        private static void CleanupEmptyDirectories(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                CleanupEmptyDirectories(directory);
            }

            if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
            {
                Directory.Delete(path, false);
                File.Delete(path + ".meta");
            }
        }

        private string GetPackageFullPath()
        {
            string packagePath = Path.GetFullPath($"Packages/{_packageName}");
            if (Directory.Exists(packagePath))
            {
                return packagePath;
            }

            packagePath = Path.GetFullPath("Library/PackageCache");
            if (Directory.Exists(packagePath))
            {
                foreach (var dir in Directory.GetDirectories(packagePath))
                {
                    if (dir.Contains(_packageName)) return dir;
                }
            }
            return null;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (Path.GetExtension(file) == ".meta") continue;
                File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
            }
        }
    }
}