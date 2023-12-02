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
    // 假设JSON包含的字段为data，是一个布尔类型。
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
            host = "localhost";
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
        // 处理new_data为true的情况
        Debug.Log("Processing new data..." + response);
        AzureOpenAIController.UserInput(response);
    }

    IEnumerator GetEmailPolling()
    {
        string url = host;
        Debug.Log("Polling Email");
        UnityWebRequest www = UnityWebRequest.Get(url + "getEmail");

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            yield break;
        }

        string response = www.downloadHandler.text;
        ResponseData responseData = JsonUtility.FromJson<ResponseData>(response);
        bool new_data = responseData.new_data;
        emailSend.threadId = responseData.thread_id;

        if (new_data)
        {
            response = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.Default.GetBytes(response));
            // 处理new_data为true的情况
            Debug.Log("Processing new data..." + response);
            AzureOpenAIController.UserInput(response);
        }
        else
        {
            // 处理new_data为false的情况
            Debug.Log("Sending request again...");
            yield return new WaitForSeconds(1f); // 等待1秒后重新发送请求
            yield return GetEmailPolling(); // 递归调用发送请求的方法
        }
    }

    // 协程用来发送GET请求，并获取file_stream的值
    IEnumerator GetAttachment()
    {
        string url = host+"getAttachment";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // 请求并等待返回
            yield return webRequest.SendWebRequest();

            // 检查是否有错误
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                // 如果有错误，打印错误信息
                Debug.LogError("Error: " + webRequest.error);
            }
            else
            {
                // 成功，处理结果
                string responseText = webRequest.downloadHandler.text;  // 获取返回的JSON字符串
                JObject jsonNode = JObject.Parse(responseText);  // 解析JSON
                if ((bool)jsonNode["new_data"]) {
                    string fileStream = (string)jsonNode["file_stream"];  // 获取file_stream字段的值
                    Debug.Log("file_stream: " + fileStream);
                    emailSend.filestream = fileStream;
                }
                else
                {
                    // 处理new_data为false的情况
                    Debug.Log("Sending request again...");
                    yield return new WaitForSeconds(1f); // 等待1秒后重新发送请求
                    yield return GetAttachment(); // 递归调用发送请求的方法
                }
            }
        }
    }

    public void CheckEmailTrigger() {
        isCheckEmail = true;
    }
}