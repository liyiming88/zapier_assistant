using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GetRequestExample : MonoBehaviour
{
    public AzureOpenAIController AzureOpenAIController;
    // 假设JSON包含的字段为data，是一个布尔类型。
    public class ResponseData
    {
        public bool new_data;
        public string content;
        public string id;
        public string sender;
        public string subject;

    }

    void Start()
    {
        StartCoroutine(SendGetRequest());
    }

    IEnumerator SendGetRequest()
    {
        string url = "http://54.224.200.205:3000/";
        Debug.Log("Polling Email");
        UnityWebRequest www = UnityWebRequest.Get(url);

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            yield break;
        }

        string response = www.downloadHandler.text;
        ResponseData responseData = JsonUtility.FromJson<ResponseData>(response);
        bool new_data = responseData.new_data;

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
            yield return SendGetRequest(); // 递归调用发送请求的方法
        }
    }
}