/*
 * Copyright 2025 The Embardiment Authors
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

package com.google.xr.embardiment.ocr

import android.graphics.BitmapFactory
import android.graphics.Rect
import android.util.Log
import com.google.mlkit.vision.common.InputImage
import com.google.mlkit.vision.text.Text
import com.google.mlkit.vision.text.TextRecognition
import com.google.mlkit.vision.text.latin.TextRecognizerOptions
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject

class OcrBridge {
    // The callback interface now expects a single JSON string on success.
    interface OcrCallback {
        fun onSuccess(resultJson: String)
        fun onFailure(errorMessage: String)
    }

    private val scope = CoroutineScope(Dispatchers.Default)
    private val recognizer = TextRecognition.getClient(TextRecognizerOptions.DEFAULT_OPTIONS)

    fun processImage(
        imageData: ByteArray,
        rotation: Int,
        callback: OcrCallback
    ) {
        Log.d("OcrBridge", "Received image data for processing: ${imageData.size} bytes.")

        scope.launch {
            try {
                val bitmap = withContext(Dispatchers.IO) {
                    BitmapFactory.decodeByteArray(imageData, 0, imageData.size)
                }

                if (bitmap == null) {
                    withContext(Dispatchers.Main) { callback.onFailure("Failed to decode image data.") }
                    return@launch
                }

                val image = InputImage.fromBitmap(bitmap, rotation)

                recognizer.process(image)
                    .addOnSuccessListener { visionText ->
                        // Serialize the result to a JSON string that matches the C# structs.
                        val jsonResponse = serializeVisionTextToJson(visionText)
                        Log.d("OcrBridge", "Text recognition successful.")
                        callback.onSuccess(jsonResponse)
                    }
                    .addOnFailureListener { e ->
                        Log.e("OcrBridge", "Text recognition failed.", e)
                        callback.onFailure(e.localizedMessage ?: "Unknown error")
                    }

            } catch (e: Exception) {
                Log.e("OcrBridge", "Error during image processing.", e)
                withContext(Dispatchers.Main) {
                    callback.onFailure(e.localizedMessage ?: "Failed during processing.")
                }
            }
        }
    }

    // This function manually builds the JSON structure to match your C# definitions.
    private fun serializeVisionTextToJson(visionText: Text): String {
        val root = JSONObject()
        root.put("fullText", visionText.text)

        val textBlocksArray = JSONArray()
        for (block in visionText.textBlocks) {
            val blockObject = JSONObject()
            blockObject.put("text", block.text)
            blockObject.put("boundingBox", jsonFromRect(block.boundingBox))

            val linesArray = JSONArray()
            for (line in block.lines) {
                val lineObject = JSONObject()
                lineObject.put("text", line.text)
                lineObject.put("boundingBox", jsonFromRect(line.boundingBox))

                val elementsArray = JSONArray()
                // ML Kit calls them 'elements', which matches your C# struct.
                for (element in line.elements) {
                    val elementObject = JSONObject()
                    elementObject.put("text", element.text)
                    elementObject.put("boundingBox", jsonFromRect(element.boundingBox))
                    elementsArray.put(elementObject)
                }
                // The JSON key is "elements", matching your C# struct.
                lineObject.put("elements", elementsArray)
                linesArray.put(lineObject)
            }
            blockObject.put("lines", linesArray)
            textBlocksArray.put(blockObject)
        }
        root.put("textBlocks", textBlocksArray)
        return root.toString()
    }

    // Helper to convert a Rect into a JSONObject using the keys from your C# BoundingBox.
    private fun jsonFromRect(rect: Rect?): JSONObject {
        val rectObject = JSONObject()
        if (rect != null) {
            rectObject.put("x", rect.left)
            rectObject.put("y", rect.top)
            rectObject.put("w", rect.width())  // Use "w" to match [JsonProperty("w")]
            rectObject.put("h", rect.height()) // Use "h" to match [JsonProperty("h")]
        }
        return rectObject
    }
}
