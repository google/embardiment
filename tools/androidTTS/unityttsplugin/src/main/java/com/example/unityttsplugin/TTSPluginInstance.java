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

package com.example.unityttsplugin;


import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import android.util.Log;

import java.util.ArrayList;
import java.util.Locale;

public class TTSPluginInstance implements TextToSpeech.OnInitListener{

    private static final String TAG = "UnityTTSPlugin";
    TextToSpeech tts;

    @Override
    public void onInit(int status) {
        if(status==TextToSpeech.SUCCESS){


            int result = tts.setLanguage(Locale.getDefault());
            tts.setSpeechRate(1);
            tts.setPitch(1);
            if (result== TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED){
                Log.e(TAG + "-onInit", "Language not supported");
            }
            else {

                tts.setOnUtteranceProgressListener(new UtteranceProgressListener() {
                    @Override
                    public void onStart(String utteranceId)
                    {
                        Log.i(TAG + "-utterOnStart","Started speaking: " + utteranceId);
                    }

                    @Override
                    public void onDone(String utteranceId) {
                        Log.i(TAG + "-utterOnDone","Done speaking: " + utteranceId);

                    }

                    @Override
                    public void onError(String utteranceId) {

                        Log.i( TAG + "-utterOnError","Error speaking: " + utteranceId);
                    }
                });
            }
        }
        else
        {
            Log.e( TAG+"-onInit","Initialization failed");
        }
    }

    public void Speak(){

    }

}
