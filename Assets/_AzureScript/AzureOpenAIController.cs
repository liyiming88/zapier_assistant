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

    private OpenAIClient client;
    private IList<ChatMessage> messages;

    public static Action<string> OnGPTContentRecieve;
    public static Action<FunctionCallResponse> OnEmailTask;

    public class FunctionCallResponse
    {
        public string name;
        public Arguments arguments;
    }

    public class Arguments
    {
        public string recipient;
        public string to_email;
        public string subject;
        public string body;
        public string tasktype;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Create chatgpt client
        client = new(new Uri(endpoint), new AzureKeyCredential(key));
        // Build request
        messages = new List<ChatMessage> { new ChatMessage(ChatRole.System, @"You are a very useful work assistant. You help users read email messages and Yiming is your user.
- Task 1. When you receive a string, you first notify your user that they have received an email from {sender}, do not tell them subject and content, and ask if they would like it to be read aloud. Once the user allows it, 
you proceed to read it. Let me give you an example.
<user>:{'sender': 'Patrick','subject': 'Lab Tour Prepare','content': 'Hi Yiming, tomorrow you are going to host Lab tour, good luck. Patrick','id': 'ididididi','new_data': true, 'thread_id': '154631654132131'}
<you>: Hi Yiming, you got a new e-mail from {sender}, shall I read it for you?
<user>: Yes, please
<you>: The subject is Lab Tour Prepare, the sender is Patrick, the content is Hi Yiming, tomorrow you are going to host Lab tour, good luck. Patrick.
After reading, you have to ask user if he wants you to assist to reply this email.

-Task 2. When a user needs you to reply to an email, understand the content the user wants to reply to before sending the email.

-Task 3. When a user asks you to send a new email on their behalf, you need to confirm that you have sufficient information about the recipient's name, recipient's email, subject, and content before proceeding with the email sending.

Contacts
    ---
        Patrick Li - patrickli123asd@gmail.com
        John - 574651401@qq.com
    ---

") };

    
    }

    private async void CallGPT()
    {

        string reply_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""recipient"": {
                    ""type"": ""string"",
                    ""description"": ""recipient""
                },
                ""to_email"": {
                    ""type"": ""string"",
                    ""description"": ""recipient email address""
                },
                ""subject"": {
                    ""type"": ""string"",
                    ""description"": ""subject""
                },
                ""body"": {
                    ""type"": ""string"",
                    ""description"": ""body""
                },
                ""tasktype"": {
                    ""type"": ""string"",
                    ""description"": ""REPLY_EMAIL or SEND_EMAIL""
                }
            },
            ""required"": [""recipient"",""to_email"",""subject"",""body"",""tasktype""]
        }";

        var chatCompletionsOptions = new ChatCompletionsOptions(messages)
        {
            Temperature = 0,
            MaxTokens = 4096,
            Functions =
            {
                /*new FunctionDefinition
                {
                    Name = "reply email",
                    Description = "按照用户的要求选择语言，比如东北话",
                    Parameters = new BinaryData(reply_param)
                },*/
                new FunctionDefinition
                {
                    Name = "email_task",
                    Description = "reply or send a email",
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
            string recipient = argObj.recipient;
            string to_email = argObj.to_email;
            string subject = argObj.subject;
            string body = argObj.body;
            string tasktype = argObj.tasktype;
            Debug.Log("func_name: " + func_name);
            Debug.Log("recipient: " + recipient);
            Debug.Log("to_email: " + to_email);
            Debug.Log("subject: " + subject);
            Debug.Log("body: " + body);
            Debug.Log("tasktype: " + tasktype);
            FunctionCallResponse funcObj = new FunctionCallResponse();
            funcObj.name = response.Value.Choices[0].Message.FunctionCall.Name;
            funcObj.arguments = argObj;
            OnEmailTask?.Invoke(funcObj);
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
