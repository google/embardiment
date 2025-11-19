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

using Google.XR.Embardiment;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GazeRayProvider : MonoBehaviour
{
    private XRGazeInteractor gi;
    public DocumentAwareAgentSamsungXR agent;

    void Start()
    {
        gi = GetComponent<XRGazeInteractor>();
    }

    void Update()
    {
        Ray ray = new(gi.rayOriginTransform.position, gi.rayOriginTransform.TransformDirection(Vector3.forward));
        agent.ProcessRay(ray);
    }
}
