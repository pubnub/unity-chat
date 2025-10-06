// snippet.using
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class MessageDraftSample
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
    
    public static async Task DraftUpdatedEventExample()
    {
        // snippet.draft_updated_event_example
        // Get a channel and create a message draft
        var channelResult = await chat.GetChannel("my_channel");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            var messageDraft = channel.CreateMessageDraft();
            
            messageDraft.ShouldSearchForSuggestions = true;

            messageDraft.OnDraftUpdated += (elements, mentions) =>
            {
                if (!mentions.Any())
                {
                    return;
                }
                messageDraft.InsertSuggestedMention(mentions[0], mentions[0].ReplaceTo);
            };
        }
        // snippet.end
    }
}
