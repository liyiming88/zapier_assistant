﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using static AzureOpenAIController;
using System.IO;
using static EmailSend;
using UnityEngine.Rendering;

public class EmailSend : MonoBehaviour
{
    private bool execute;
    public string filestream { get; set; }
    public string threadId { get; set; }

    public SpeechController speechController;

    private string replyWebhookURL = "https://hooks.zapier.com/hooks/catch/17202968/3kvahw8/";
    private string sendWebhookURL = "https://hooks.zapier.com/hooks/catch/17202968/3kvs171/";

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
    }

    // Update is called once per frame
    void Update()
    {
        if (execute)
        {
            StartCoroutine(PostEmail());
            execute = false;
        }
    }

    public void PrepareEmail(FunctionCallResponse obj)
    {
        Debug.Log("tasktype: " + obj.arguments.p5);
        if (obj.arguments.p5 == "SEND_EMAIL")
        {
            webhookURL = sendWebhookURL;
        }
        else if (obj.arguments.p5 == "REPLY_EMAIL")
        {
            webhookURL = replyWebhookURL;
        }
        requestBody.to_email = obj.arguments.p1;
        requestBody.subject = obj.arguments.p2;
        requestBody.body = obj.arguments.p3;
        requestBody.thread_id = threadId;
        requestBody.file_stream = filestream;
        execute = true;
    }


    IEnumerator PostEmail()
    {
        string json = JsonUtility.ToJson(requestBody);
        Debug.Log("Payload to webhooks: "+json);
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
