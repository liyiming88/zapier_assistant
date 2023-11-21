using Azure;
using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AzureOpenAIController : MonoBehaviour
{
    public string key;
    public string deployment_name;
    public string endpoint;
    public TMP_InputField inputField;

    private OpenAIClient client;
    private IList<ChatMessage> messages;

    public static Action<string> OnGPTContentRecieve;
    public static Action<FunctionCallResponse> OnEmailSend;

    public class FunctionCallResponse
    {
        public string name;
        public Arguments arguments;
    }

    public class Arguments
    {
        public string name;
        public string email;
        public string subject;
        public string body;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Create chatgpt client
        client = new(new Uri(endpoint), new AzureKeyCredential(key));
        // Build request
        messages = new List<ChatMessage> { new ChatMessage(ChatRole.System, @"你是一个非常有用的工作助理，你帮助用户读取邮箱信件，
-你的任务1，当你收到这个字符串时，先通知用户你收到一封来自{sender}的邮件，询问是否播报。
当用户允许播报后，你进行播报。我给你举一个例子。
<user>:{'sender': '李逸明','subject': '实验室参观准备工作','content': 'Patrick，你好，周五下午我们要进行实验室参观的排练，请保证人员场地物料完备，谢谢。Thanks，李逸明','id': 'kjlsjfsiljakdflajfljekl','new_data': true}
<you>: Hi Patrick, 你有一封李逸明发来的新邮件哟，需要我为你播报吗
<user>: 好的，请播报
<you>: 这封邮件的标题是：实验室参观准备工作。内容是：Patrick，你好，周五下午我们要进行实验室参观的排练，请保证人员场地物料完备，谢谢。Thanks，李逸明。

任务2，当用户需要你为他发邮件时，你需要确认你保证充分知道 收件人名称，收件人邮箱, 主题, 内容 后才可以进行邮件发送。
已知邮件名称和邮件地址的对应关系是
    ---
        Vincent - lym6953597@163.com
        李逸明 - 574651041@qq.com
        John - patrickli789asd@gmail.com
    ---

") };


        /*   UserInput("我从小有一个当演员的梦想，但现在的我怕是很难实现这个梦想了。你能帮我实现演员梦吗，哪怕生成几个剧照或一段简短的视频都可以满足我。");*/
    }

    private async void CallGPT()
    {

        string reply_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""name"": {
                    ""type"": ""string"",
                    ""description"": ""邮件中的收件人名称""
                },
                ""email"": {
                    ""type"": ""string"",
                    ""description"": ""邮件中的收件人邮箱""
                },
                ""subject"": {
                    ""type"": ""string"",
                    ""description"": ""邮件中的主题""
                },
                ""body"": {
                    ""type"": ""string"",
                    ""description"": ""邮件中的内容""
                }
            },
            ""required"": [""name"",""email"",""subject"",""body""]
        }";

        var chatCompletionsOptions = new ChatCompletionsOptions(messages)
        {
            Temperature = 0,
            MaxTokens = 4096,
            Functions =
            {
              /*  new FunctionDefinition
                {
                    Name = "change_dialect",
                    Description = "按照用户的要求选择语言，比如东北话",
                    Parameters = new BinaryData(lang_param)
                },*/
                new FunctionDefinition
                {
                    Name = "send_email",
                    Description = "发邮件",
                    Parameters = new BinaryData(reply_param)
                }
            }
        };
        Response<ChatCompletions> response = await client.GetChatCompletionsAsync(deployment_name, chatCompletionsOptions);
        if (response.Value.Choices[0].Message.Content != null)
        {
            string res_str = response.Value.Choices[0].Message.Content;
            AddCharacterResToMessage(res_str);
            OnGPTContentRecieve?.Invoke(res_str);
        }
        if (response.Value.Choices[0].Message.FunctionCall != null)
        {
            string res_str = response.Value.Choices[0].Message.FunctionCall.Arguments;
            Arguments argObj = JsonUtility.FromJson<Arguments>(res_str);
            string func_name = response.Value.Choices[0].Message.FunctionCall.Name;
            string p1 = argObj.name;
            string p2 = argObj.email;
            string p3 = argObj.subject;
            string p4 = argObj.body;
            Debug.Log("func_name: " + func_name);
            Debug.Log("p1: " + p1);
            Debug.Log("p2: " + p2);
            Debug.Log("p3: " + p3);
            Debug.Log("p4: " + p4);
            FunctionCallResponse funcObj = new FunctionCallResponse();
            funcObj.name = response.Value.Choices[0].Message.FunctionCall.Name;
            funcObj.arguments = argObj;
            /*            AddCharacterResToMessage("");*/
            switch (func_name)
            {
                /*case "change_dialect":
                    OnDialectFuncRecieve?.Invoke(funcObj);
                    break;
                case "change_scene":
                    OnBgFuncRecieve?.Invoke(funcObj);
                    break;*/
                case "send_email":
                    OnEmailSend?.Invoke(funcObj);
                    break;
            }
        }
    }


    public void AddCharacterResToMessage(string content)
    {
        var characterMessage = new ChatMessage(ChatRole.Assistant, content);
        messages.Add(characterMessage);
        Debug.Log("Avatar's message added: " + content);
    }

    public void UserInput()
    {
        messages.Add(new ChatMessage(ChatRole.User, inputField.text));
        Debug.Log("User input: " + inputField.text);
        CallGPT();
    }

    public void UserInput(string speechtext)
    {
        if (speechtext == null || speechtext.Length == 0)
        {
            return;
        }
        else
        {
            messages.Add(new ChatMessage(ChatRole.User, speechtext));
            Debug.Log("User says: " + speechtext);
            CallGPT();
        }
    }

    public void AssistantInput(string speechtext)
    {
        messages.Add(new ChatMessage(ChatRole.Assistant, speechtext));
        Debug.Log("Assitant says: " + speechtext);
    }

    void Update()
    {
        if (messages.Count > 6)
        {
            messages.RemoveAt(1);
        }
    }
}
