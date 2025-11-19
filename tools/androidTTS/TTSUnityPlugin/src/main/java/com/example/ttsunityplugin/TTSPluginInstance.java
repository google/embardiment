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

package com.example.ttsunityplugin;

import android.app.Activity;
import android.content.Intent;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import android.speech.tts.Voice;
import android.util.Log;

import androidx.localbroadcastmanager.content.LocalBroadcastManager;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.Set;

import com.unity3d.player.UnityPlayer;

public class TTSPluginInstance implements TextToSpeech.OnInitListener{

    private static final String TAG = "UnityTTSPlugin";

    private boolean runFromUnity = false;
    private TextToSpeech tts;
    private Set<Voice> voiceSet;
    private List<Voice> voices;
    private Integer currentVoiceIndex = -1;

    private float currentPitch = 1.0F;
    private float currentSpeechRate = 1.0F;

    private String localeStr;
    private static Activity unityActivity;
    private static final String UNITY_TARGET_GAME_OBJECT  = "TTSPluginManager";
    public boolean IsInitialized =false;

    public TTSPluginInstance(){
        try{
            Class<UnityPlayer> Uplayer = ((Class<UnityPlayer>) Class.forName("com.unity3d.player.UnityPlayer"));
            Log.i(TAG + "-constructor", "UnityPlayer successfully initialized. Plugin being called from Unity.");
            runFromUnity = true;

        } catch (ClassNotFoundException e) {
            Log.i(TAG + "-constructor", "UnityPlayer Not found. Plugin not being called from Unity." + e);
        } catch (Exception e) {
            Log.i(TAG + "-constructor", "UnityPlayer class exception." + e);
        }
    }

    public static void receiveUnityActivity(Activity activity){
        unityActivity = activity;
    }

    public void InitializeTTS(){
        tts = new TextToSpeech(unityActivity, this);
        localeStr =  Locale.getDefault().toLanguageTag();
    }

    @Override
    public void onInit(int status) {

        if(status==TextToSpeech.SUCCESS){
            tts.setSpeechRate(currentSpeechRate);
            tts.setPitch(currentPitch);

            int result = tts.setLanguage(Locale.forLanguageTag(localeStr));
            if (result== TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED){
                Log.e(TAG + "-onInit", "Language not supported");
            }
            else {
                voiceSet = tts.getVoices();
                voices = new ArrayList<>(voiceSet);
                tts.setOnUtteranceProgressListener(new UtteranceProgressListener() {
                    @Override
                    public void onStart(String utteranceId)
                    {
                        ReportUtteranceEvent("onStart",utteranceId,"");
                        Log.i(TAG + "-utterOnStart","Started speaking: " + utteranceId);
                    }

                    @Override
                    public void onDone(String utteranceId) {
                        ReportUtteranceEvent("onDone",utteranceId,"");
                        Log.i(TAG + "-utterOnDone","Done speaking: " + utteranceId);
                    }

                    @Override
                    public void onError(String utteranceId) {
                        ReportUtteranceEvent("onError",utteranceId,"");
                        Log.i( TAG + "-utterOnError","Error speaking: " + utteranceId);
                    }

                    @Override
                    public void onError(String utteranceId, int errorCode) {
                        ReportUtteranceEvent("onError",utteranceId,String.valueOf(errorCode));
                        Log.i( TAG + "-utterOnError","Error speaking: " + utteranceId + "Error Code: " + errorCode);
                    }
                });
                IsInitialized = true;
            }
        }
        else
        {
            Log.e( TAG+"-onInit","Initialization failed");
        }
    }


    //region UnityPluginAPIs

    /**
     * Unity Plugin API: to set the voice (accent) to be used by TTS
     *
     * @param voiceIndexStr string representation of the interger index value of the voice to set
     */
    public void SetVoiceIndex(String voiceIndexStr){
        try {
            currentVoiceIndex = Integer.parseInt(voiceIndexStr);
        } catch (NumberFormatException e) {
            Log.e(TAG + "-SetVoiceIndex", "Invalid voice index");
            currentVoiceIndex = -1;
        }
    }

    /**
     * Unity Plugin API: to set the language to be spoken by TTS
     *
     * @param languageStr string representation of the locale value for the language to speak
     */
    public void SetLanguage(String languageStr){
        localeStr = languageStr;
        int result = tts.setLanguage(Locale.forLanguageTag(localeStr));
        if (result== TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED){
            Log.e(TAG + "-SetLanguage", "Language not supported");
        }
    }

    /**
     * Unity Plugin API: to set the speechRate to be spoken by TTS
     *
     * @param speechRate string representation of the float value to set as the rate of speech of the speech engine
     * */
    public void SetSpeechRate(float speechRate){
        currentSpeechRate = speechRate;
        tts.setSpeechRate(currentSpeechRate);
    }

    /**
     * Unity Plugin API: to set the pitch to be spoken at by TTS
     *
     * @param pitch string representation of the float value to set the Pitch of the TTS engine to speak at
     */
    public void SetPitch(float pitch){
        currentPitch=pitch;
        tts.setPitch(currentPitch);
    }

    /**
     * Unity Plugin API: used to check if the TTS engine is currently speaking
     *
     * @return  string representation of true/false boolean indicating if the TTS engine is speaking
     */
    public String GetIsSpeaking(){
        return String.valueOf(tts.isSpeaking());
    }

    /**
     * Unity Plugin API: returns a list of the voices that are available from this TTS engine
     *
     * @return  a pipe "|" then comma "," delimited string containing the voice details
     */
    public String GetVoiceList(){
        String voiceList = "";
        for (Voice voice : voices){
            //voiceList += voice.toString() + ",";
            voiceList += voice.getLocale().toString() + " | " + voice.getName() + " | " + String.valueOf(voice.getQuality())+ " | " + String.valueOf(voice.getLatency())  + " | " + String.valueOf(voice.isNetworkConnectionRequired()) + ",";
        }

        return voiceList.replaceAll(",$","");
    }

    /**
     * Unity Plugin API: to initiate speaking of the text by the TTS engine in the current voice, pitch, rate etc..
     *
     * @param textToSpeak string representation of text that the TTS engine should speak
     */
    public void Speak(String textToSpeak){
        if (currentVoiceIndex >= 0 && currentVoiceIndex < voices.size()) {
            Voice newVoice =voices.get(currentVoiceIndex);
            tts.setVoice(newVoice);
        }

        tts.speak(textToSpeak, TextToSpeech.QUEUE_FLUSH, null, TextToSpeech.ACTION_TTS_QUEUE_PROCESSING_COMPLETED);
    }

    /**
     * Unity Plugin API: to stop and interupt speaking of text by the TTS engine
     *
     */
    public void Stop(){
        tts.stop();
    }

    private void ReportUtteranceEvent(String eventDescription, String utteranceId, String detail){
        //UnitySendMessageWrapper(UNITY_TARGET_GAME_OBJECT, "HandelUtteranceProgressCallback", (eventDescription + "," + utteranceId + "," + detail).replaceAll(",$",""));
        UnitySendMessageWrapper(UNITY_TARGET_GAME_OBJECT, "HandelUtteranceProgressCallback", (eventDescription ));
    }

    //endregion

    //region Utils
    private void UnitySendMessageWrapper(String gameObject, String methodName, String methodParam){
        if (runFromUnity){
            UnityPlayer.UnitySendMessage(gameObject, methodName, methodParam);
            Log.d(TAG + "-USMW", "Sent UnitySendMessage: " + gameObject + "," + methodName + "," + methodParam);
        }
        else {
            SendIntentFakeSendMessage(methodName, methodParam);
            Log.d(TAG + "-USMW", "Sent Fake UnitySendMessage intent: " + methodName + "," + methodParam);
        }
    }

    private void SendIntentFakeSendMessage(String methodName, String methodParam){
        Intent fakeSendMessageIntent = new Intent("incomingMessage");
        fakeSendMessageIntent.putExtra("method", methodName);
        fakeSendMessageIntent.putExtra("param",methodParam);
        LocalBroadcastManager.getInstance(unityActivity).sendBroadcast(fakeSendMessageIntent);
    }

    //endregion
}
