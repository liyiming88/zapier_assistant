using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Unity.VisualScripting;
using static AzureOpenAIController;
using static LanguageSelect;
using System;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class SpeechService : MonoBehaviour
{
    SpeechConfig config;
    static SpeechRecognizer recognizer;
    static SpeechSynthesizer synthesizer;
    public AzureOpenAIController azureOpenAIController;
    private object threadLocker = new object();
    private string message;
    private bool recongnizeFinished;
    private bool killed;
    private LangObj langObj;
    public GameObject character;
    private Animator animator;
    private bool idleState;
    private bool talkState;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool recongnizable = true;
    private bool isSilent;
    public bool inputMode;
    private bool openMicAftersynthesis = true;
    public void Start()
    {
        // character setting;
        startPosition = character.transform.position;
        startRotation = character.transform.rotation;
        animator = character.GetComponent<Animator>();
        // langauage setting;
        // Auth: Auzre Speech API key and region
        config = SpeechConfig.FromSubscription("87df75af6be3479a80484a90b0404966", "eastus");
        // 'zh-CN': Chinese; 'en-US': English
        //diction:'zh-CN-XiaoxiaoNeural' 'en-US-AriaNeural'
        // here is Azure speech language list: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=tts
        config.SpeechSynthesisLanguage = "zh-CN-liaoning";
        config.SpeechSynthesisVoiceName = "zh-CN-liaoning-YunbiaoNeural";
        config.SpeechRecognitionLanguage = "zh-CN";
        /*config.SpeechSynthesisLanguage = "en-US";
        config.SpeechSynthesisVoiceName = "en-US-JennyMultilingualNeural";
        config.SpeechRecognitionLanguage = "en-US";*/
        // Create speech recongnizer
        recognizer = new SpeechRecognizer(config);
        // subscribution: callback RecognizedHandler() when recognizer start to recognize
        recognizer.Recognized += RecognizedHandler;
        // subscribution: callback StartToThink() when recognizer start to process recognizing
        recognizer.Recognizing += AvatarAnimite;

        // Create speech synthesizer
        synthesizer = new SpeechSynthesizer(config);
        synthesizer.SynthesisStarted += StopRecord;
        synthesizer.SynthesisCompleted += RestartRecord;
       
        /* SynthesizeAudioAsync($"你好Jeffery, 很高兴这次能和你一起共同主持这个活动。");*/
        Debug.Log("Speech sdk inited");
        string[] aaa = Microphone.devices;
        OpenMic();
        OnGPTContentRecieve += SynthesizeAudioAsync;
    }

    public async void OpenMic()
    {
        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false); // this will start the listening when you click the button, if it's already off
        lock (threadLocker)
        {
            Debug.Log("Start recording");
        }
    }

    private void RecognizedHandler(object sender, SpeechRecognitionEventArgs e)
    {
        if (recongnizable)
        {
            Debug.Log("Start to speech to text");
            lock (threadLocker)
            {
                switch (e.Result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        Debug.Log($"RECOGNIZED: Text={e.Result.Text}");
                        break;
                    case ResultReason.NoMatch:
                        Debug.Log($"NOMATCH: Speech could not be recognized.");
                        isSilent = true;
                        KillRecord();
                        OpenMic();
                        break;
                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(e.Result);
                        Debug.Log($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Debug.Log($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Debug.Log($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            Debug.Log($"CANCELED: Did you set the speech resource key and region values?");
                        }
                        break;
                }
                message = e.Result.Text;
                recongnizeFinished = true;
                recongnizable = false;

                azureOpenAIController.UserInput(message);
            }
        }

    }

    private void AvatarAnimite(object sender, SpeechRecognitionEventArgs e)
    {

    }

    void Update()
    {
        if (!inputMode) {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ManualStop();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                ManualStart();
            }
        }
        if (talkState)
        {
            animator.SetTrigger("talk");
            character.transform.position = startPosition;
            character.transform.rotation = startRotation;
        }
        if (idleState)
        {
            animator.SetTrigger("idle");
            character.transform.position = startPosition;
            character.transform.rotation = startRotation;
        }
        talkState = false;
        idleState = false;
    }
    // stop record user speech
    private async void StopRecord(object sender, SpeechSynthesisEventArgs e)
    {
        Debug.Log("Stop recording");
        talkState = true;

        // this will start the listening when you click the button, if it's already off
        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
        lock (threadLocker)
        {
            // Todo, set some bool flag states

        }
    }

    public async void ManualStop()
    {
        Debug.Log("----------------stop----------------");
        // this will start the listening when you click the button, if it's already off
        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
    }

    public async void ManualStart()
    {
        Debug.Log("----------------start----------------");
        // this will start the listening when you click the button, if it's already off
        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
    }

    public void ToggleMic(bool state)
    {
        openMicAftersynthesis = state;
        Debug.Log("Mic ready to open: " + state);
    }

    // start to record user speech
    private async void RestartRecord(object sender, SpeechSynthesisEventArgs e)
    {
        if (openMicAftersynthesis)
        {
            recongnizable = true;
            idleState = true;
            if (!killed)
            {
                Debug.Log("Avatar stops talking and recording starts");
                // this will start the listening when you click the button, if it's already off
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                lock (threadLocker)
                {
                    // Todo, set some bool flag states

                }
            }
        }
    }

    public async void KillRecord(Action callback = null)
    {
        Debug.Log("Kill record");
        await recognizer.StopContinuousRecognitionAsync();
        Debug.Log("Kill Avatar speaking");
        await synthesizer.StopSpeakingAsync();
        // reset language after killing
        callback?.Invoke();

    }

    // text to speech
    // avatar is talking
    public static async void SynthesizeAudioAsync(string text)
    {
        Debug.Log("Start text to speech, and avatar starts to speak");
        if (text.Contains("<speak"))
        {

            await synthesizer.StartSpeakingSsmlAsync(text);
        }
        else
        {
            await synthesizer.StartSpeakingTextAsync(text);
        }

    }

    void OnDestroy()
    {
        killed = true;
        KillRecord();
    }

}