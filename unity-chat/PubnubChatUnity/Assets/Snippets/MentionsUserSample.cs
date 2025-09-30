// snippet.using
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class MentionsUserSample
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
    
    public static async Task AddUserMentionExample()
    {
        // snippet.add_user_mention_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var testChannel = channelResult.Result;
        
        // Create a message draft
        var messageDraft = testChannel.CreateMessageDraft();

        // Update the message with the initial text
        messageDraft.Update("Hello Alex! I have sent you this link on the #offtopic channel.");

        // Add a user mention to the string "Alex"
        messageDraft.AddMention(6, 4, new MentionTarget
        {
            Target = "alex_d",
            Type = MentionType.User
        });
        // snippet.end
    }
    
    public static void RemoveUserMentionExample(MessageDraft messageDraft)
    {
        // snippet.remove_user_mention_example
        // assume the message reads
        // Hello Alex! I have sent you this link on the #offtopic channel.`

        // remove the user reference
        messageDraft.RemoveMention(6);
        // snippet.end
    }
}
