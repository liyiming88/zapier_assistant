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

    public static Action<string> OnGPTContentRecieve;
    public static Action<FunctionCallResponse> OnEmailTask;

    public class FunctionCallResponse
    {
        public string name;
        public Arguments arguments;
    }

    public class Arguments
    {
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
        // Build request
        messages = new List<ChatMessage> { new ChatMessage(ChatRole.System, @"You are a very useful work assistant. You help users read email messages and Yiming is your user.
- Task 1. When you receive a string, you first notify your user that they have received an email from {sender}, do not tell them subject and content, and ask if they would like it to be read aloud. Once the user allows it, 
you proceed to read it. Let me give you an example.
<user>:{'sender': 'Patrick','subject': 'Lab Tour Prepare','content': 'Hi Yiming, tomorrow you are going to host Lab tour, good luck. Patrick','id': 'ididididi','new_data': true, 'thread_id': '154631654132131'}
<you>: Hi Yiming, you got a new e-mail from {sender}, shall I read it for you?
<user>: Yes, please
<you>: The subject is Lab Tour Prepare, the content is Hi Yiming, tomorrow you are going to host Lab tour, good luck. Patrick. Do you want me to assist you to reply this email?

Tasks for you:
-Task 1. When a user needs you to reply to an email, You generate the email content yourself and let the user review the content before sending it.Do not ask user about recipient's email and name.

-Task 2. When a user asks you to send a new email on their behalf, you need to confirm that you have sufficient information about the recipient's name, recipient's email, subject, and content before proceeding with the email sending. When user provide the recipient's name,
        You check the name in contacts first. If the name is not in contacts, you ask the user for the recipient's email address. If the name is in contacts, you use the related email address. 
-Task 3. Before sending the email, you need to confirm with the user that the email content is correct. If the user says no, you need to ask the user to re-enter the email content. If the user says yes, you send the email.
Contacts
    ---
        Yiming - patrickli123asd@gmail.com
        Patrick - yiming.li@fmr.com
        Joyce - xiaoyu.sun@fmr.com
    ---

") };
        /*speechController.SynthesizeAudioAsync("Hi, I'm your AI assistant, how can I help you today");*/


    }

    private async void CallGPT()
    {

        string reply_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p2"": {
                    ""type"": ""string"",
                    ""description"": ""recipient email address""
                },
                ""p3"": {
                    ""type"": ""string"",
                    ""description"": ""subject""
                },
                ""p4"": {
                    ""type"": ""string"",
                    ""description"": ""body""
                },
                ""p5"": {
                    ""type"": ""string"",
                    ""description"": ""If the user wants to reply to an email, the output value is 'REPLY_EMAIL'. If the user wants to compose a new email, the output value is 'SEND_EMAIL'""
                }
            },
            ""required"": [""p2"",""p3"",""p4"",""p5""]
        }";

/*        string illustration_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p1"": {
                    ""type"": ""string"",
                    ""description"": ""What is the main message you want to convey in the body of your draft email? Summarize in no more than 5 words.'""
                }
            },
            ""required"": [""p1""]
        }";*/

        var chatCompletionsOptions = new ChatCompletionsOptions(messages)
        {
            Temperature = 0,
            MaxTokens = 4096,
            Functions =
            {
                /*new FunctionDefinition
                {
                    Name = "summarize_the_main_message_from_email",
                    Description = "Summarize the main message you want to convey in the body of your draft email,summarize in no more than 5 words.",
                    Parameters = new BinaryData(illustration_param)
                },*/
                new FunctionDefinition
                {
                    Name = "email_task",
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
/*            string func_name = response.Value.Choices[0].Message.FunctionCall.Name;
            string recipient = argObj.recipient;
            string to_email = argObj.to_email;
            string subject = argObj.subject;
            string body = argObj.body;*/
            FunctionCallResponse funcObj = new FunctionCallResponse();
            funcObj.name = response.Value.Choices[0].Message.FunctionCall.Name;
            funcObj.arguments = argObj;
            if (funcObj.name == "email_task") {
                string tasktype = argObj.p5;
                Debug.Log("tasktype: " + tasktype);
                OnEmailTask?.Invoke(funcObj);
                AddCharacterResToMessage("Great, the e-mail has been sent");
            }
           /* if (funcObj.name == "summarize_the_main_message_from_email")
            {
                Debug.Log("prompt of illustration: " + argObj.p1);
                AddCharacterResToMessage("I am generating the illustration, which usually takes more than 10 seconds. When I have generated it, I will send this email with the attachment.");
                speechController.SynthesizeAudioAsync("I am generating the illustration, which usually takes more than 10 seconds. When I have generated it, I will send this email with the attachment.");
            }*/
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

    void Update()
    {
        if (messages.Count > 6)
        {
            messages.RemoveAt(1);
        }
    }
}
