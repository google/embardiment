/*
 * Copyright 2025 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package com.google.xr.embardiment.llm

import android.app.Application
import android.util.Log
import com.google.ai.edge.aicore.GenerativeModel
import com.google.ai.edge.aicore.generationConfig
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class LlmBridge {
    interface LlmCallback {
        fun onSuccess(result: String)
        fun onFailure(errorMessage: String)
    }

    private val scope = CoroutineScope(Dispatchers.Default)
    private var generativeModel: GenerativeModel? = null
    /* let's use these default values */
    private var temperature: Float? = null
    private var topK: Int? = null
    private var maxOutputTokens: Int? = null

    fun updateSettings(maxOutputTokens: Int, temperature: Float, topK: Int) {
        this.maxOutputTokens = maxOutputTokens
        this.temperature = temperature
        this.topK = topK
        generativeModel?.close();
        generativeModel = null;
    }

    companion object {
        private val applicationContext: Application? by lazy {
            try {
                val unityPlayerClass = Class.forName("com.unity3d.player.UnityPlayer")
                val currentActivity = unityPlayerClass.getField("currentActivity").get(null)
                (currentActivity as? android.app.Activity)?.application
            } catch (e: Exception) {
                Log.e("LlmBridge", "Failed to get application context via UnityPlayer.", e)
                null
            }
        }
    }

    private suspend fun initializeModel() = withContext(Dispatchers.IO) {
        if (generativeModel != null) return@withContext

        val context = applicationContext
            ?: throw IllegalStateException("Application context is null. Cannot initialize model.")

        val currentTemp = temperature ?: throw IllegalStateException("LLM temperature not set. Call updateSettings() from Unity.")
        val currentTopK = topK ?: throw IllegalStateException("LLM topK not set. Call updateSettings() from Unity.")
        val currentMaxTokens = maxOutputTokens ?: throw IllegalStateException("LLM maxOutputTokens not set. Call updateSettings() from Unity.")

        generativeModel = GenerativeModel(
            generationConfig {
                this.candidateCount = 1
                this.context = context
                this.temperature = currentTemp
                this.topK = currentTopK
                this.maxOutputTokens = currentMaxTokens
            }
        )
        Log.d("LlmBridge", "GenerativeModel initialized successfully.")
    }


    fun generateResponse(prompt: String, callback: LlmCallback) {
        Log.d("LlmBridge", "Received prompt: $prompt")

        scope.launch {
            try {
                if (generativeModel == null) {
                    initializeModel()
                }

                val response = generativeModel?.generateContent(prompt)
                val responseText = response?.text ?: "No valid response from model."

                withContext(Dispatchers.Main) {
                    callback.onSuccess(responseText)
                }

            } catch (e: Exception) {
                Log.e("LlmBridge", "Error during LLM processing.", e)
                withContext(Dispatchers.Main) {
                    callback.onFailure(e.localizedMessage ?: "Failed during processing.")
                }
            }
        }
    }
}