using System.Diagnostics;
using PubNubChatAPI.Entities;

var chat = new Chat("pub-c-79961364-c3e6-4e48-8d8d-fe4f34e228bf", "sub-c-2b4db8f2-c025-4a76-9e23-326123298667", "heheh");
var channel = chat.CreatePublicConversation("test", new ChatChannelData()
{
    ChannelName = "test",
    ChannelDescription = "fuck",
    ChannelCustomDataJson = "{}",
    ChannelStatus = "1",
    ChannelType = "sure",
    ChannelUpdated = "true"
});
//channel.Join();
Debug.WriteLine("test");
var callback = new Channel.CallbackStringFunction((result => Debug.WriteLine(result)));
Debug.WriteLine("did you get here?");
channel.Connect(callback);
Debug.WriteLine("or here?");
//channel.Join();
            
Thread.Sleep(5000);

channel.SendText("ARE YOU HEARING ME YOU BASTARDS?");
Debug.WriteLine("OR HERE?");
            
Thread.Sleep(3000);
Debug.WriteLine("A");
            
Debug.WriteLine("B");
            
Thread.Sleep(7000);