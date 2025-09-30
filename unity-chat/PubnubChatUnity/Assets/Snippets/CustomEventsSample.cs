// snippet.using
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubApi.Unity;
using PubnubChatApi;

// snippet.end

public class CustomEventsSample
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
    
    public static async Task EmitCustomEventExample()
    {
        // snippet.emit_custom_event_example
        await chat.EmitEvent(
            type: PubnubChatEventType.Custom,
            channelId: "CUSTOMER-SATISFACTION-CREW",
            jsonPayload: 
                "{\"chatID\": \"chat1234\"," +
                "\"timestamp\": \"2022-04-30T10:30:00Z\"," +
                "\"customerID\": \"customer5678\"," + 
                "\"triggerWord\": \"frustrated\"}"
        );
        // snippet.end
    }
    
    public static async Task ListenForCustomEventsExample()
    {
        // snippet.listen_for_custom_events_example
        var channelResult = await chat.GetChannel("CUSTOMER-SATISFACTION-CREW");
        if (channelResult.Error) return;
        var channel = channelResult.Result;
        
        // simulated event data received
        string eventData = 
            "\"chatID\":\"chat1234\"," +
            "\"timestamp\":\"2022-04-30T10:30:00Z\"," +
            "\"customerID\":\"customer5678\"," +
            "\"triggerWord\":\"frustrated\"";

        // example function to handle the "frustrated" event and satisfy the customer
        void HandleFrustratedEvent(string eventData) {
            //basic JSON parsing using the pluggable library
            var data = chat.PubnubInstance.JsonPluggableLibrary.DeserializeToDictionaryOfObject(eventData);
            
            // extract relevant information from the event data
            string customerID = data["customerID"].ToString();
            string timestamp = data["timestamp"].ToString();
            string triggerWord = data["triggerWord"].ToString();

            // create a response
            string response = "Thank you for reaching out. We're sorry to hear " +
                $"that you're {triggerWord}. Our team is here to help and will work to resolve your" +
                "concerns as quickly as possible. Your satisfaction is important to us.";

            // send the response back to the customer's chat
            SendResponseToCustomerChat(customerID, timestamp, response);
        }

        // example event listener using "SetListeningForCustomEvents()" on some channel
        channel.SetListeningForCustomEvents(true);
        channel.OnCustomEvent += customEvent => 
        {
             if(customEvent.Payload.Contains("\"triggerWord\":\frustrated\""))
             {
                 HandleFrustratedEvent(customEvent.Payload);
             }
        };
        // snippet.end
    }
    
    // Helper method for the example above
    private static void SendResponseToCustomerChat(string customerID, string timestamp, string response)
    {
        // Implementation would go here
        Console.WriteLine($"Sending response to {customerID}: {response}");
    }
    
    public static async Task GetEventsHistoryExample()
    {
        // snippet.get_events_history_example
        // define the required parameters
        string channelId = "CUSTOMER-SATISFACTION-CREW";
        int count = 10;

        // fetch the last 10 historical events
        var historyResult = await chat.GetEventsHistory(channelId, null, null, count);
        if (historyResult.Error)
        {
            // Handle error
            return;
        }
        var history = historyResult.Result;

        // process the returned historical events
        foreach (var eventItem in history.Events)
        {
            Console.WriteLine($"Timestamp: {eventItem.TimeToken}, Event type: {eventItem.Type}");
        }
        // snippet.end
    }
}
