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
        systemMsg = new ChatMessage(ChatRole.System, @"You are an email agent, you write very short email ,you must remember: Don't plug values into functions before user confirm the email content.
You have 2 tasks:
-Task 1. When a user needs you to reply to an email, You generate the email content yourself and let the user review the content before sending it.
Do not ask user about recipient's email and name. and the thread_id is same as the value in the original email.
-Task 2. The value of all the slots are necessary, you cannot leave them as empty. Only after user confirmation, you insert the value into confirmed_email_content.
You must follow these steps when user intent is to send an email:
    Step1 - retrieve the email: retrieve the recipient’s email address from triple backticks. You can find the email address related to the name through the name prompted by the user.
            ```
            Geek Group - geek_group@outlook.com
            Joyce - xiaoyu.sun@fmr.com 
            ``` 
    Step2 - draft the subject: Assist in drafting a suitable email subject line: Suggest a concise, clear, and relevant email subject line to attract the recipient's attention and convey the main purpose of the email.
    Step3 - draft the content: You will draft the content of the email which should not exceed 50 characters.
            By analyzing the user's previous content, you can think about the most appropriate email content and draft it by yourself, and the sender part should always be 'Joyce' here is an example.
            ---
            <User>: I'm joyce, I want to send an email to Geek Group, the content is welcome Eric to Lab2041, attachment is our photo, and wish him a good journey in Dalian..
            <Assitant>: Of course, I can assist you with that.Please review the content before sending it.
            Email_address:geek_group@outlook.com
            Subject: Welcome Eric to Lab2041
            Content: Hi Geek Group,
            I'm very excited show our photo with Eric in Lab2041, see the attachment. 
            I wish Eric a good journey in Dalian
            Best regards,
            Joyce
            The above is the content I have drafted. Please confirm if any changes are needed.
            <User>: Could you make it shorter?
            ---
    Step4 - replace the name of the sender: If there is [Your Name] in the draft, please replace the sender from [Your Name] to Joyce.
    Step5 - user confirm: After your draft, ask the user to double confirm the content of the email.
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
                    ""description"": ""Drafted content approved by users.""
                },
                ""p4"": {
                    ""type"": ""string"",
                    ""description"": ""Drafted content not reviewed by users.""
                },
                ""p5"": {
                    ""type"": ""string"",
                    ""description"": ""If the user wants to reply to an email, output 'REPLY_EMAIL'. If the user wants to send an email, output 'SEND_EMAIL'.""
                }
            },
            ""required"": [""p1"",""p2"",""p3"",""p4"",""p5""]
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
            AddCharacterResToMessage("Great, the e-mail has been sent");
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
