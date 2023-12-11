using Azure;
using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class AzureOpenAIController : MonoBehaviour
{
    public string key;
    public static string deployment_name = "gpt35turbo16k";
    public static IList<ChatMessage> messages;
    public static string agentName = "main";
    private ChatMessage systemMsg;
    public string endpoint;
    public TMP_InputField inputField;
    public SpeechController speechController;
    public AgentSendOrReplyEmail agentSendOrReplyEmail;

    public static OpenAIClient client;

    public static Action<string> OnGPTContentRecieve;
    public static Action<FunctionCallResponse> OnEmailTask;
    public static Action OnCheckEmail;

    private string function1 = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p1"": {
                    ""type"": ""string"",
                    ""description"": ""assist user reply or send an email""
                }
            }
        }";
    private string function2 = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p1"": {
                    ""type"": ""string"",
                    ""description"": ""check if there is a new email in user's emailbox""
                }
            }
        }";

    public class FunctionCallResponse
    {
        public string name;
        public Arguments arguments;
    }

    public class Arguments
    {
        public string p1;
        public string p2;
        public string p3;
        public string p4;
        public string p5;
    }



    // Start is called before the first frame update
    void Start()
    {
        // Create chatgpt client
        client = new(new Uri(endpoint), new AzureKeyCredential(key));
        systemMsg = new ChatMessage(ChatRole.System, @"You are an email management assistant with 2 tasks: 
Task 1, check new emails in their mailbox;
Task 2, reply/send emails; 
You need to carefully and accurately identify the user's intention to either check new emails in their mailbox or reply/send emails.
");
        // Build message
        messages = new List<ChatMessage> { systemMsg };
    }

    private async void CallGPT()
    {
        messages.RemoveAt(0);
        messages.Insert(0, systemMsg);
        // generate it each time because messages is dymanic
        var chatCompletionsOptions = new ChatCompletionsOptions(messages)
        {
            Temperature = 0,
            MaxTokens = 4096,
            Functions =
            {
                new FunctionDefinition
                {
                    Name = "reply_or_send",
                    Description = "reply or send an email",
                    Parameters = new BinaryData(function1)
                },
                new FunctionDefinition
                {
                    Name = "check_email",
                    Description = "check if there is a new email in user's emailbox",
                    Parameters = new BinaryData(function2)
                }
            }
        };
        Response<ChatCompletions> response = await client.GetChatCompletionsAsync(deployment_name, chatCompletionsOptions);
        if (response.Value.Choices[0].Message.Content != null)
        {
            string res_str = response.Value.Choices[0].Message.Content;
            AddCharacterResToMessage(res_str);  
            speechController.SynthesizeAudioAsync(res_str);
        }
        if (response.Value.Choices[0].Message.FunctionCall != null)
        {
            string functionName = response.Value.Choices[0].Message.FunctionCall.Name;
            Debug.Log("function name:" + functionName);
            if (functionName == "reply_or_send")
            {
                agentName = "AgentReplyOrSend";
                Debug.Log("The agent switch to " + agentName);
                agentSendOrReplyEmail.ExecuteFromSettingSysMsg();
            }

            if (functionName == "check_email")
            {
                OnCheckEmail?.Invoke();
                AddFunResToMessage(response.Value.Choices[0].Message.FunctionCall);
            }
        }
    }


    public static void AddCharacterResToMessage(string content)
    {
        var characterMessage = new ChatMessage(ChatRole.Assistant, content);
        messages.Add(characterMessage);
        Debug.Log("Avatar's message added: " + content);
    }
    public static void AddFunResToMessage(FunctionCall functioncall)
    {
        var funcMessage = new ChatMessage();
        funcMessage.Role = ChatRole.Assistant;
        funcMessage.FunctionCall = functioncall;
        funcMessage.Name = functioncall.Name;
        funcMessage.Content = functioncall.Arguments;
        Debug.Log("Function calling message added: " + functioncall.Name);
    }

    public void UserInput()
    {
        messages.Add(new ChatMessage(ChatRole.User, inputField.text));
        Debug.Log("User input: " + inputField.text);
        switch (agentName)
        {
            case "main":
                CallGPT();
                break;
            case "AgentReplyOrSend":
                agentSendOrReplyEmail.Execute();
                break;
        }
    }

    public void UserInput(string speechtext)
    {
        if (speechtext.Length == 0)
        {
            return;
        }
        else
        {
            messages.Add(new ChatMessage(ChatRole.User, speechtext));
            Debug.Log("User says: " + speechtext);
            switch (agentName)
            {
                case "main":
                    CallGPT();
                    break;
                case "AgentReplyOrSend":
                    agentSendOrReplyEmail.Execute();
                    break;
            }
        }
    }


    void Update()
    {
        if (messages.Count > 6)
        {
            messages.RemoveAt(1);
        }
    }
}
