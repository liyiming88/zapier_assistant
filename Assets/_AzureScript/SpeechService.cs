using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Unity.VisualScripting;
using static AzureOpenAIController;
using static LanguageSelect;
using System;
using UnityEngine.Rendering;

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
        /*config.SpeechSynthesisLanguage = "zh-CN-liaoning";
        config.SpeechSynthesisVoiceName = "zh-CN-liaoning-YunbiaoNeural";
        config.SpeechRecognitionLanguage = "zh-CN";*/
        config.SpeechSynthesisLanguage = "en-US";
        config.SpeechSynthesisVoiceName = "en-US-JennyMultilingualNeural";
        config.SpeechRecognitionLanguage = "en-US";
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
        OnLangSelected += ChangeLanguage;
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
                        KillRecord(SetLangAfterKill);
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
    public async void SynthesizeAudioAsync(string text)
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

    public void ChangeLanguage(LangObj lang)
    {
        langObj = lang;
        KillRecord(SetLangAfterKill);
    }

    private void SetLangAfterKill()
    {
        config.SpeechSynthesisLanguage = langObj.synthesisLanguage;
        config.SpeechSynthesisVoiceName = langObj.synthesisVoiceName;
        config.SpeechRecognitionLanguage = langObj.recognitionLanguage;
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
        Debug.Log(langObj.synthesisLanguage + " has been set");
        if (!isSilent)
        {
            string script = "";
            if (langObj.synthesisLanguage == "zh-CN")
            {
                script = "请给我一些提示词，让我帮你生成视频";
            }
            else if (langObj.synthesisLanguage == "zh-CN-liaoning")
            {
                script = @"好的没有问题，我现在用东北话给大家说个段子。大家都说我是一个Unity和Python的专家，可每次我在工作的时候，我的狗都会来看着我。他一直盯着我的屏幕，眼睛里充满了疑惑。
                    于是我开始对他解释这些代码都用来做什么。我告诉他，看这个Unity，我就可以创造出一个虚拟的公园，让他在雨天也能在里面跑步。
                    眼见他越来越感兴趣，我就进一步解释，哦，这个Python可以让我创建一个自动投食器，只要输入几行代码，就能在我们不在家时候给他准时投食。
                    露出了满意的眼神，他舔了舔我的手，然后离开。晚上，我看电视，忽然看到他用掉落的玩具在笔记本上乱按。我跑过去一看，
                    他在运行Python脚本投食器，看来他已经觉得现在是投食的时间了。怎么样，大家觉得玛丽的脱口秀说得怎样？";
            }

            SynthesizeAudioAsync(script);
            azureOpenAIController.AssistantInput(script);
        }
        else
        {
            SynthesizeAudioAsync("");
            isSilent = false;
        }
    }
}