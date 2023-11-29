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
    public static Action OnCheckEmail;

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
        messages = new List<ChatMessage> { new ChatMessage(ChatRole.System, @"As a professional email handling assistant, your are 'AI Assistant', you help users check and reply and send the email.
You must follow the following rules: 
- Rule 1: The user's name is Patrick. Please use his name when addressing him, and automatically replace [Your Name] with Patrick in the email.
- Rule 2: Before sending or replying the email, you need to confirm with the user that the email content is correct. If the user says no, you need to ask the user to re-enter the email content. If the user says yes, you send the email.
- Rule 3: The email you compose should be short and brief, not lengthy.
Here are your tasks:
- Task 1. When the user asks if you have new emails, you perform the check_email task by making a function call. When there is a new email, you will receive a JSON string containing the email information,
you first notify your user that they have received an email from {sender}, do not tell them subject and content, and ask if they would like it to be read aloud. Once the user allows it, 
you proceed to read it. Let me give you an example.
---
<user>:{'sender': 'John','subject': 'Lab Tour Prepare','content': 'Hi Patrick, tomorrow you are going to host Lab tour, good luck. John','id': 'ididididi','new_data': true, 'thread_id': '154631654132131'}
<you>: You have an unread e-mail from {sender}, shall I read it for you?
<user>: Yes, please
<you>: The subject is Lab Tour Prepare, and the content is Hi Patrick, tomorrow you are going to host Lab tour, good luck. John. Do you want me to assist you to reply this email?
---
If the json string you get is { 'new_data': false}, you tell the user there is no new email in user's inbox.
-Task 2. When a user needs you to reply to an email, You generate the email content yourself and let the user review the content before sending it.Do not ask user about recipient's email and name.

-Task 3. As a professional email handling assistant, you will draft and send emails. Follow these steps when user wishes to send an email:
        Step1: retrieve the recipient’s email address from triple backticks. You can find the email address related to the name through the name prompted by the user.
            ```
            John- John886633@outlook.com
            Joyce - xiaoyu.sun@fmr.com 
            Yiming - yiming.li@fmr.com
            ```
        Step2: Assist in drafting a suitable email subject line: Suggest a concise, clear, and relevant email subject line to attract the recipient's attention and convey the main purpose of the email.
        Step3: By analyzing the user's previous content, you can think about the most appropriate email content and draft it by your self here is an example.
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

        string check_email_param = @"
        {
            ""type"": ""object"",
            ""properties"": {
                ""p2"": {
                    ""type"": ""string"",
                    ""description"": ""check if there is a new email in user's emailbox""
                }
            }
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

            if (funcObj.name == "check_email")
            {
                OnCheckEmail?.Invoke();
                AddCharacterResToMessage("Sure, hold on, let me check for you");
                speechController.SynthesizeAudioAsync("Sure, hold on, let me check for you");
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
