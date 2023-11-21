using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using static AzureOpenAIController;

public class EmailSend : MonoBehaviour
{

    private string action_id;
    public string apiKey;
    private EmailData emailData;
    private bool send_email;

    [System.Serializable]
    public class EmailData
    {
        public string instructions;
    }

    void Start()
    {
        emailData = new EmailData();
        OnEmailSend += PrepareEmail;
        StartCoroutine(GetZapierAIActionId());
        
    }

    // Update is called once per frame
    void Update()
    {
        if (send_email)
        {
            StartCoroutine(PostEmail());
            send_email = false;
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
        string instructions = $"'email':{obj.arguments.email},'subject':{obj.arguments.subject},'body':{obj.arguments.body}";
        emailData.instructions = instructions;
        send_email = true;

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
        string url = $"https://actions.zapier.com/api/v1/exposed/{action_id}/execute/";

        /*string requestBody = "{\"instructions\": \"'email':574651401@qq.com, 'subject':from postman , 'body':do you have time\"}";*/
        string requestBody = JsonUtility.ToJson(emailData);
        // 创建UnityWebRequest对象
        UnityWebRequest webRequest = new UnityWebRequest(url, "POST");

        // 将json字符串转化成byte数组，并且设置到webRequest的uploadHandler中
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestBody);
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);

        // 设置请求的header
        webRequest.SetRequestHeader("x-api-key", apiKey);
        webRequest.SetRequestHeader("Content-Type", "application/json");

        // 这里设置downloadHandler，用于处理响应内容（即使我们不打算读取内容）
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        // 发送请求并且等待请求结束
        yield return webRequest.SendWebRequest();

        // 检查请求是否有错误
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + webRequest.error);
        }
        else
        {
            // 请求成功，可以用webRequest.downloadHandler.text获取响应
            Debug.Log("Response: " + webRequest.downloadHandler.text);
        }
    }

}
