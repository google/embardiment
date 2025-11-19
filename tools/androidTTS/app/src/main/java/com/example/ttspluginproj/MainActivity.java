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

package com.example.ttspluginproj;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

import androidx.activity.EdgeToEdge;
import androidx.appcompat.app.AppCompatActivity;
import androidx.localbroadcastmanager.content.LocalBroadcastManager;

import java.lang.reflect.Method;

import com.example.ttsunityplugin.TTSPluginInstance;


public class MainActivity extends AppCompatActivity {

    private static final String TAG = "TTSPluginTester";

    EditText inputTextToSpeak;
    EditText inputVoiceId;
    EditText inputPitchValue;
    EditText inputSpeechRateValue;

    Button speakButton;
    Button speechRateButton;
    Button pitchButton;
    Button listVoicesButton;
    Button voiceIdButton;
    TextView utterStatus;
    TextView voiceCount;
    TextView voiceList;

    TTSPluginInstance pluginInstance = new TTSPluginInstance();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        EdgeToEdge.enable(this);
        setContentView(R.layout.activity_main);

        inputTextToSpeak = findViewById(R.id.text_to_speak_field);
        inputPitchValue = findViewById(R.id.pitch_value);
        inputSpeechRateValue = findViewById(R.id.speech_rate_value);
        inputVoiceId = findViewById(R.id.input_voice_id);
        utterStatus = findViewById(R.id.utter_status);
        voiceCount=findViewById(R.id.voice_count);
        voiceList=findViewById(R.id.voice_list);
        speakButton = findViewById(R.id.speak_button);
        speakButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                pluginInstance.Speak(inputTextToSpeak.getText().toString());
            }
        });
        pitchButton = findViewById(R.id.set_pitch_button);
        pitchButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                float defaultPitch = 1;
                pluginInstance.SetPitch(ClearParseFloat(inputPitchValue.getText().toString(),defaultPitch));
            }
        });
        speechRateButton = findViewById(R.id.set_speech_rate_button);
        speechRateButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                float defaultRate = 1;
                pluginInstance.SetSpeechRate(ClearParseFloat(inputSpeechRateValue.getText().toString(),defaultRate));
            }
        });
        listVoicesButton = findViewById(R.id.list_voices_button);
        listVoicesButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                PopulateVoiceList();
            }
        });

        voiceIdButton = findViewById(R.id.set_voice_id_button);
        voiceIdButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                int defaultIndex = 0;
                pluginInstance.SetVoiceIndex(String.valueOf(ClearParseInt(inputVoiceId.getText().toString(),defaultIndex)));
            }
        });
        pluginInstance.receiveUnityActivity(this);
        pluginInstance.InitializeTTS();

        //Wire up Broadcastreceiver for fake Unity SendMessage support
        LocalBroadcastManager.getInstance(this).registerReceiver(mReceiver, new IntentFilter("incomingMessage"));

    }

    @Override
    protected void onStart(){
        super.onStart();
    }

    private void PopulateVoiceList(){
        if (pluginInstance.IsInitialized){
            String[] voices = pluginInstance.GetVoiceList().split(",");
            voiceList.setText("");
            for (int i=0;i< voices.length -1; i++) {
                String[] voiceDetails = voices[i].split("|");
                voiceList.append( i + "    ");
                for (String detail : voiceDetails){
                    voiceList.append( " " + detail);
                }
                voiceList.append("\n");
            }
        }
    }

    BroadcastReceiver mReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String method = intent.getStringExtra("method");
            String param = intent.getStringExtra("param");
            Log.d(TAG + "-mR", "Received Intent Fake SendMessage message for Method: " + method + " with param: " + param) ;

            //Invoke Method through reflection
            try{
                Class classObj = Class.forName(("com.example.ttspluginproj.MainActivity"));
                Method sendMessageMethod = classObj.getDeclaredMethod(method, String.class);
                sendMessageMethod.invoke(MainActivity.this, param);
            } catch (Exception e){
                Log.e(TAG + "-mR", "Invoke method failed: " + method + " with param: " + param + " Msg: " + e);
            }
        }
    };

    public void HandelUtteranceProgressCallback(String param){
        switch (param){
            case "onStart":
                utterStatus.setText("Speaking..");
                break;
            case "onDone":
                utterStatus.setText("");
                break;
            default:
                utterStatus.setText("");
        }
    }

    //region Utils
    private float ClearParseFloat(String stringToParse, float defaultValue){

        Float parsedValue;

        try {
            parsedValue = Float.parseFloat(stringToParse);
            return parsedValue;

        }catch (NullPointerException nullPointerException){
            return defaultValue;

        }catch (NumberFormatException numberFormatException){
            return defaultValue;

        }catch (Exception e){
            return defaultValue;
        }
    }

    private int ClearParseInt(String stringToParse, int defaultValue){

        int parsedValue;

        try {
            parsedValue = Integer.parseInt(stringToParse);
            return parsedValue;

        }catch (NullPointerException nullPointerException){
            return defaultValue;

        }catch (NumberFormatException numberFormatException){
            return defaultValue;

        }catch (Exception e){
            return defaultValue;
        }
    }

    //endregion
}