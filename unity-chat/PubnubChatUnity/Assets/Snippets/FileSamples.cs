// snippet.using

using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;

// snippet.end

public class FileSamples
{
    private static Chat chat;

    static async Task Init()
    {
        // snippet.init
        // Configuration
        PubnubChatConfig chatConfig = new PubnubChatConfig();
        
        PNConfiguration pnConfiguration = new PNConfiguration(new UserId("myUniqueUserId"))
        {
            SubscribeKey = "demo",
            PublishKey = "demo",
            Secure = true
        };

        // Initialize Unity Chat
        var chatResult = await UnityChat.CreateInstance(chatConfig, pnConfiguration);
        if (!chatResult.Error)
        {
            chat = chatResult.Result;
        }
        // snippet.end
    }
    
    public static async Task SendFile()
    {
        // snippet.send_file
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Could not get channel! Error: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;
        
        var sendResult = await channel.SendText("some message", new SendTextParams()
        {
            Files = new List<ChatInputFile>()
            {
                new ChatInputFile()
                {
                    Name = "some_file.txt",
                    //Same as above because assuming it's in the same directory as the script
                    Source = "some_file.txt",
                    Type = "text"
                }
            }
        });
        
        //Checking sendResult in case there was an issue with the file e.g. it didn't exist, was too large,
        //or the keyset didn't have Files functionality enabled
        if (sendResult.Error)
        {
            Debug.LogError($"Error when sending message with file: {sendResult.Exception.Message}");
        }
        // snippet.end
    }
    
    public static async Task SendFileFromDraft()
    {
        // snippet.send_file_from_draft
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Could not get channel! Error: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;

        var messageDraft = channel.CreateMessageDraft();
        messageDraft.InsertText(0, "some text");
        messageDraft.Files.Add(new ChatInputFile()
        {
            Name = "some_file.txt",
            //Same as above because assuming it's in the same directory as the script
            Source = "some_file.txt",
            Type = "text"
        });

        var sendResult = await messageDraft.Send();
        
        //Checking sendResult in case there was an issue with the file e.g. it didn't exist, was too large,
        //or the keyset didn't have Files functionality enabled
        if (sendResult.Error)
        {
            Debug.LogError($"Error when sending message with file: {sendResult.Exception.Message}");
        }
        // snippet.end
    }

    public static async Task GetFilesFromMessage()
    {
        // snippet.get_files_from_message
        
        //Also works with messages from OnMessageReceived callback
        var message = await chat.GetMessage("some_channel", "12345678912345678");
        if (message.Error)
        {
            Debug.LogError($"Could not get message! Error: {message.Exception.Message}");
            return;
        }

        var files = message.Result.Files;
        foreach (var chatFile in files)
        {
            Debug.Log($"Message file with ID: {chatFile.Id}, Name: {chatFile.Name}, URL: {chatFile.Url}, and Type: {chatFile.Type}");
        }
        // snippet.end
    }

    public static async Task GetFiles()
    {
        // snippet.get_files
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Could not get channel! Error: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;
        
        var files = await channel.GetFiles();
        if (files.Error)
        {
            Debug.LogError($"Error when trying to get files: {files.Exception.Message}");
            return;
        }

        foreach (var file in files.Result.Files)
        {
            Debug.Log($"File ID: {file.Id}, file Name: {file.Name}, file URL: {file.Url}");
        }
        // snippet.end
    }
    
    public static async Task DeleteFile()
    {
        // snippet.delete_file
        var getChannel = await chat.GetChannel("some_channel");
        if (getChannel.Error)
        {
            Debug.LogError($"Could not get channel! Error: {getChannel.Exception.Message}");
            return;
        }
        var channel = getChannel.Result;

        //ID and Name should be from either Message.Files or channel.GetFiles()
        var delete = await channel.DeleteFile("file_id", "file_name");
        // snippet.end
    }
}
