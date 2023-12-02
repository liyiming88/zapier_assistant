using Azure;
using Azure.AI.OpenAI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AzureOpenAIController;

public class AgentSendOrReplyEmail : MonoBehaviour
{
    public SpeechController speechController;
    private ChatMessage systemMsg;
    private string functions;
    // Start is called before the first frame update
    void Start()
    {
        systemMsg = new ChatMessage(ChatRole.System, @"You are an email agent ,you must remember: Don't plug values into functions before user confirm the email content.
You have 2 tasks:
-Task 1. When a user needs you to reply to an email, You generate the email content yourself and let the user review the content before sending it.
Do not ask user about recipient's email and name.
-Task 2. As a professional email handling assistant, you will draft a short and brief email which should not exceed 50 characters.
The value of all the slots are necessary, you cannot leave them as empty.
Follow these steps when user wishes to send an email:
    Step1: retrieve the recipient’s email address from triple backticks. You can find the email address related to the name through the name prompted by the user.
            ```
            John- John886633@outlook.com
            Joyce - xiaoyu.sun@fmr.com 
            Yiming - yiming.li@fmr.com
            ```
    Step2: Assist in drafting a suitable email subject line: Suggest a concise, clear, and relevant email subject line to attract the recipient's attention and convey the main purpose of the email.
    Step3: You will draft the content of the email which should not exceed 50 characters.
            By analyzing the user's previous content, you can think about the most appropriate email content and draft it by yourself, and the sender part should always be 'Patrick' here is an example.
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
    Step4: If there is [Your Name] in the draft, please replace the sender from [Your Name] to Patrick.
    Step5: Before triggering the functions, ask the user if they confirm the content of the email.
");

        functions = @"
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ExecuteFromSettingSysMsg() {
        messages.RemoveAt(0);
        messages.Insert(0, systemMsg);
        Execute();
    }
    public async void Execute()
    {
        var chatCompletionsOptions = new ChatCompletionsOptions(messages)
        {
            Temperature = 0,
            MaxTokens = 4096,
            Functions =
            {
                new FunctionDefinition
                {
                    Name = "Agent_reply_or_send",
                    Description = "reply or send an email",
                    Parameters = new BinaryData(functions)
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
            AddFunResToMessage(response.Value.Choices[0].Message.FunctionCall);
            Arguments argObj = JsonUtility.FromJson<Arguments>(res_str);
            FunctionCallResponse funcObj = new FunctionCallResponse();
            funcObj.name = response.Value.Choices[0].Message.FunctionCall.Name;
            funcObj.arguments = argObj;
            OnEmailTask?.Invoke(funcObj);
            agentName = "main";
            speechController.SynthesizeAudioAsync("Great, the e-mail has been sent");
            Debug.Log("The agent switch to " + agentName);
        }

    }
}
