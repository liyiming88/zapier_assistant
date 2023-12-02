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
    public string deployment_name;
    public string endpoint;
    public TMP_InputField inputField;
    public SpeechController speechController;

    private OpenAIClient client;
    private IList<ChatMessage> messages;
    private IList<ChatMessage> agentSendinglMessages;

    public static Action<string> OnGPTContentRecieve;
    public static Action<FunctionCallResponse> OnEmailTask;
    public static Action OnCheckEmail;
    private string conversationController;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        conversationController = "default";
        // Create chatgpt client
        client = new(new Uri(endpoint), new AzureKeyCredential(key));
        // Build request
        messages = new List<ChatMessage> { new ChatMessage(ChatRole.System, @"You are an email management assistant with 2 tasks: 
Task 1, check new emails in their mailbox;
Task 2, reply/send emails; 
You need to carefully and accurately identify the user's intention to either check new emails in their mailbox or reply/send emails.
") };
        agentSendinglMessages = new List<ChatMessage> { new ChatMessage(ChatRole.System, @"You are an email agent ,you must remember: Don't plug values into functions before user confirm the email content.
You have 2 tasks:
-Task 1. When a user needs you to reply to an email, You generate the email content yourself and let the user review the content before sending it.
Do not ask user about recipient's email and name.
-Task 2. As a professional email handling assistant, you will draft and send emails. 
The value of all the slots are necessary, you cannot leave them as empty.
Follow these steps when user wishes to send an email:
    Step1: retrieve the recipient’s email address from triple backticks. You can find the email address related to the name through the name prompted by the user.
            ```
            John- John886633@outlook.com
            Joyce - xiaoyu.sun@fmr.com 
            Yiming - yiming.li@fmr.com
            ```
    Step2: Assist in drafting a suitable email subject line: Suggest a concise, clear, and relevant email subject line to attract the recipient's attention and convey the main purpose of the email.
    Step3: By analyzing the user's previous content, you can think about the most appropriate email content and draft it by yourself, and the sender part should always be 'Patrick' here is an example.
            ---
            <User>: John is sick, I want to send him an email to John to show my care.
            <Assitant>: Of course, I can assist you with that. Please wait a moment while I draft the email for you.
            Email_address:574651401@qq.com
            Subject: Wishing You a Speedy Recovery
            Content: Hi John,
            I hope this email finds you as well as can be expected. I heard that you're not feeling well, and I wanted to reach out and send my best wishes for a quick and full recovery. Take the time you need to rest and take care of yourself.
            If there's anything I can do to help during this time, please don't hesitate to let me know. Sending you positive thoughts and healing vibes.
            Take care and get well soon!
            Best regards, 
            Patrick
            The above is the content I have drafted. Please confirm if any changes are needed.
            <User>: Yes, could you make it shorter?
            ---
    Step4: Before triggering the functions, ask the user if they confirm the content of the email.
") };
    }

    private async void CallGPT()
    {

        string reply_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p1"": {
                    ""type"": ""string"",
                    ""description"": ""assist user reply or send an email""
                }
            }
        }";

        string check_email_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p1"": {
                    ""type"": ""string"",
                    ""description"": ""check if there is a new email in user's emailbox""
                }
            }
        }";

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
                    Parameters = new BinaryData(reply_param)
                },
                new FunctionDefinition
                {
                    Name = "check_email",
                    Description = "check if there is a new email in user's emailbox",
                    Parameters = new BinaryData(check_email_param)
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
            FunctionCallResponse funcObj = new FunctionCallResponse();
            funcObj.name = response.Value.Choices[0].Message.FunctionCall.Name;
            Debug.Log("function name:" + funcObj.name);
            if (funcObj.name == "reply_or_send")
            {
                conversationController = "AgentReplyOrSend";
                CallReplyOrSendEmailAgent();
            }

            if (funcObj.name == "check_email")
            {
                OnCheckEmail?.Invoke();
                AddFunResToMessage(response.Value.Choices[0].Message.FunctionCall);
            }
        }
    }


    public void AddCharacterResToMessage(string content)
    {
        var characterMessage = new ChatMessage(ChatRole.Assistant, content);
        messages.Add(characterMessage);
        agentSendinglMessages.Add(characterMessage);
        Debug.Log("Avatar's message added: " + content);
    }
    public void AddFunResToMessage(FunctionCall functioncall)
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
        switch (conversationController)
        {
            case "default":
                CallGPT();
                break;
            case "AgentReplyOrSend":
                CallReplyOrSendEmailAgent();
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
            agentSendinglMessages.Add(new ChatMessage(ChatRole.User, speechtext));
            Debug.Log("User says: " + speechtext);
            switch (conversationController)
            {
                case "default":
                    CallGPT();
                    break;
                case "AgentReplyOrSend":
                    CallReplyOrSendEmailAgent();
                    break;
            }
        }
    }


    void Update()
    {
        if (messages.Count > 6)
        {
            messages.RemoveAt(1);
            agentSendinglMessages.RemoveAt(1);
        }
    }

    private async void CallReplyOrSendEmailAgent()
    {

        string reply_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p1"": {
                    ""type"": ""string"",
                    ""description"": ""recipient email address""
                },
                ""p2"": {
                    ""type"": ""string"",
                    ""description"": ""subject""
                },
                ""p3"": {
                    ""type"": ""string"",
                    ""description"": ""body""
                },
                ""p4"": {
                    ""type"": ""string"",
                    ""description"": ""If the user wants to reply to an email, output 'REPLY_EMAIL'. If the user wants to send an email, output 'SEND_EMAIL'.""
                }
            },
            ""required"": [""p1"",""p2"",""p3"",""p4""]
        }";


        var chatCompletionsOptions = new ChatCompletionsOptions(agentSendinglMessages)
        {
            Temperature = 0,
            MaxTokens = 4096,
            Functions =
            {
                new FunctionDefinition
                {
                    Name = "Agent_reply_or_send",
                    Description = "reply or send an email",
                    Parameters = new BinaryData(reply_param)
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
            string res_str = response.Value.Choices[0].Message.FunctionCall.Arguments;
            Arguments argObj = JsonUtility.FromJson<Arguments>(res_str);
            FunctionCallResponse funcObj = new FunctionCallResponse();
            funcObj.name = response.Value.Choices[0].Message.FunctionCall.Name;
            funcObj.arguments = argObj;
            OnEmailTask?.Invoke(funcObj);
            conversationController = "default";
        }
    }
}
