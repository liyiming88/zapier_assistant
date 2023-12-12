using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static AzureOpenAIController;

public class ZapierController : MonoBehaviour
{
    public AzureOpenAIController AzureOpenAIController;
    public EmailSend emailSend;
    public bool localhost;
    public string host;
    private bool isCheckEmail;
    public class ResponseData
    {
        public bool new_data;
        public string content;
        public string id;
        public string sender;
        public string subject;
        public string thread_id;
    }

    void Start()
    {
        OnCheckEmail += CheckEmailTrigger;
        if (localhost)
        {
            host = "http://127.0.0.1:3000/";
        }
        StartCoroutine(GetAttachment());
    }

    private void Update()
    {
        if (isCheckEmail)
        {
            StartCoroutine(CheckEmail());
            isCheckEmail = false;
        }
    }
    IEnumerator CheckEmail()
    {
        string url = host;
        Debug.Log("Polling Email");
        UnityWebRequest www = UnityWebRequest.Get(url+ "getEmail");

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            yield break;
        }

        string response = www.downloadHandler.text;

        response = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.Default.GetBytes(response));
        Debug.Log("Processing new data..." + response);
        AzureOpenAIController.UserInput(response);
    }

   
    IEnumerator GetAttachment()
    {
        string url = host+"getAttachment";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + webRequest.error);
            }
            else
            {
                string responseText = webRequest.downloadHandler.text; 
                JObject jsonNode = JObject.Parse(responseText);
                string fileStream = (string)jsonNode["file_stream"];
                Debug.Log("file_stream: " + fileStream);
                emailSend.filestream = fileStream;
            }
        }
    }

    public void CheckEmailTrigger() {
        isCheckEmail = true;
    }
}