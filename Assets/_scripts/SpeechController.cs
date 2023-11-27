using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class SpeechController : MonoBehaviour
{
    private SpeechRecognizer recognizer;
    private SpeechSynthesizer synthesizer;
    private bool isRecognizing = false;
    private bool isSynthesising = false;
    public TextMeshProUGUI recognizedTextDisplay;
    public AzureOpenAIController gpt;
    public GameObject user;
    public GameObject assistant;
    public GameObject character;
    private Animator animator;
    private bool idleState;
    private bool talkState;
    private bool clearAssistantContext;
    private string recongnizedtxt;  

    void Awake()
    {
        animator = character.GetComponent<Animator>();
        var config = SpeechConfig.FromSubscription("87df75af6be3479a80484a90b0404966", "eastus");
        config.SpeechSynthesisLanguage = "en-US";
        config.SpeechSynthesisVoiceName = "en-US-JennyMultilingualNeural";
        config.SpeechRecognitionLanguage = "en-US";
        recognizer = new SpeechRecognizer(config);
        synthesizer = new SpeechSynthesizer(config);

        // 订阅事件以获取实时识别结果
        recognizer.Recognizing += (s, e) => {
            if (e.Result.Reason == ResultReason.RecognizingSpeech)
            {
                /*Debug.Log($"Recognizing: {e.Result.Text}");*/
                recongnizedtxt = e.Result.Text;
            }
        };

        recognizer.Recognized += (s, e) => {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Debug.Log($"Recognized: {e.Result.Text}");
                recongnizedtxt = e.Result.Text;
                gpt.UserInput(e.Result.Text);
            }
        };

        recognizer.Canceled += (s, e) => {
            Debug.Log($"Canceled: {e.Reason}");
            isRecognizing = false;
        };

        recognizer.SessionStopped += (s, e) => {
            Debug.Log("Session stopped.");
            isRecognizing = false;
        };

        synthesizer.SynthesisStarted += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.SynthesizingAudioStarted)
            {
                isSynthesising = true;
                talkState = true;
                idleState = false;
            }
        };

        synthesizer.SynthesisCompleted += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                talkState = false;
                idleState = true;
            }
        };
        string[] aaa = Microphone.devices;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isRecognizing)
        {
            if (clearAssistantContext) {
                recongnizedtxt = "";
                clearAssistantContext = false;
            }
            isSynthesising = false;
            user.SetActive(true);
            assistant.SetActive(false);
            StartSpeechRecognition();
        }
        else if (Input.GetKeyUp(KeyCode.Q) && isRecognizing)
        {
            clearAssistantContext = true;
            StopSpeechRecognition();
        }
        recognizedTextDisplay.text = recongnizedtxt;

        if (talkState)
        {
            animator.SetTrigger("talk");
        }
        if (idleState)
        {
            animator.SetTrigger("idle");
        }
        if (isSynthesising)
        {
            user.SetActive(false);
            assistant.SetActive(true);
        }
        talkState = false;
        idleState = false;
    }

    async void StartSpeechRecognition()
    {
        isRecognizing = true;
        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
    }

    async void StopSpeechRecognition()
    {
        isRecognizing = false;
        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
    }

    void OnDestroy()
    {
        recognizer.Dispose();
        synthesizer.Dispose();
    }

    public async void SynthesizeAudioAsync(string text)
    {
        await synthesizer.StartSpeakingTextAsync(text);
        
        recongnizedtxt = text;
    }
}