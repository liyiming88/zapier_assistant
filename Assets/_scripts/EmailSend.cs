using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using static AzureOpenAIController;
using System.IO;
using static EmailSend;

public class EmailSend : MonoBehaviour
{

    private string action_id;
    public string apiKey;
    private bool execute;
    public string filestream { get; set; }
    public string threadId { get; set; }

    private string replyWebhookURL = "https://hooks.zapier.com/hooks/catch/17041246/3krc4cs/";
    private string sendWebhookURL = "https://hooks.zapier.com/hooks/catch/17041246/3k9yc17/";

    private string webhookURL = "";
    private RequestBody requestBody;

    [System.Serializable]
    public class RequestBody
    {
        public string file_stream;
        public string to_email;
        public string cc_email;
        public string subject;
        public string body;
        public string thread_id;
    }


    void Start()
    {
        filestream = "";
        threadId = "";
        requestBody = new RequestBody();
        OnEmailTask += PrepareEmail;
        StartCoroutine(GetZapierAIActionId());

    }

    // Update is called once per frame
    void Update()
    {
        if (execute)
        {
            StartCoroutine(PostEmail());
            execute = false;
        }

        /*  if (Input.GetKeyDown(KeyCode.Space))
          {

              string instructions = $"'email':lym6953597@163.com,'subject':周六来加班,'body':Vincent你在哪里？周六项目需要加班";
              emailData.instructions = instructions;
              StartCoroutine(PostEmail());
          }*/
    }

    public void PrepareEmail(FunctionCallResponse obj)
    {
        if (obj.arguments.tasktype == "SEND_EMAIL")
        {
            webhookURL = sendWebhookURL;
        }
        else if (obj.arguments.tasktype == "REPLY_EMAIL")
        {
            webhookURL = replyWebhookURL;
        }
        requestBody.to_email = obj.arguments.to_email;
        requestBody.subject = obj.arguments.subject;
        requestBody.body = obj.arguments.body;
        requestBody.thread_id = threadId;
        requestBody.file_stream = "";
        execute = true;

    }

    IEnumerator GetZapierAIActionId()
    {
        string url = "https://actions.zapier.com/api/v1/exposed/";

        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SetRequestHeader("x-api-key", apiKey);

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(webRequest.error);
        }
        else
        {
            string jsonData = webRequest.downloadHandler.text;
            JObject jsonResponse = JObject.Parse(jsonData);

            JArray results = (JArray)jsonResponse["results"];
            foreach (JToken resultToken in results)
            {
                JObject result = (JObject)resultToken;

                string description = (string)result["description"];
                if (description == "Microsoft Outlook: Send Email")
                {
                    action_id = (string)result["id"];
                    break;
                }
            }

            Debug.Log("id value for 'Microsoft Outlook: Send Email': " + action_id);
        }
    }

    IEnumerator PostEmail()
    {
        string json = JsonUtility.ToJson(requestBody);

        UnityWebRequest request = new UnityWebRequest(webhookURL, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
        }
    }

}
