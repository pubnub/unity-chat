// snippet.using
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;
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
    
    public static async Task InsertSuggestedMentionExample()
    {
        // snippet.insert_suggested_mention_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
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

        messageDraft.Update("@Alex are you there?");
        // snippet.end
    }
    
    public static async Task CheckMessageMentionsExample()
    {
        // snippet.check_message_mentions_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            Debug.Log($"Found channel with name {channel.Name}");

            // get the message with the specified timetoken
            var messageResult = await channel.GetMessage("16200000000000000");
            if (!messageResult.Error)
            {
                var message = messageResult.Result;
                Debug.Log($"Message: {message.MessageText}");

                // check if the message contains any mentions
                if (message.MentionedUsers != null && message.MentionedUsers.Count > 0)
                {
                    Debug.Log("The message contains mentions.");
                    foreach (var mentionedUser in message.MentionedUsers)
                    {
                        Debug.Log($"Mentioned User: {mentionedUser.Name}");
                    }
                }
                else
                {
                    Debug.Log("The message does not contain any mentions.");
                }
            }
            else
            {
                Debug.Log("Message with the specified timetoken not found.");
            }
        }
        else
        {
            Debug.Log("Support channel not found.");
        }
        // snippet.end
    }
    
    public static async Task GetCurrentUserMentionsExample()
    {
        // snippet.get_current_user_mentions_example
        // fetch the last 10 mentions for the current user
        var mentions = await chat.GetCurrentUserMentions(string.Empty, string.Empty, 10);

        if (!mentions.Error && mentions.Result.Mentions.Any())
        {
            foreach (var mention in mentions.Result.Mentions)
            {
                Debug.Log($"Mentioned in Channel ID: {mention.ChannelId}, Message: {mention.Message.MessageText}");
            }
        }
        else
        {
            Debug.Log("No mentions found.");
        }
        // snippet.end
    }
    
    public static async Task NotificationForMentionExample()
    {
        // snippet.notification_for_mention_example
        var userResult = await chat.GetCurrentUser();
        if (userResult.Error)
        {
            return;
        }
        var user = userResult.Result;
        user.SetListeningForMentionEvents(true);
        user.OnMentionEvent += mentionEvent => 
        {
            if(mentionEvent.ChannelId == "support")
            {
                Debug.Log($"{user.Id} has been mentioned on the support channel!");
            }
        };
        // snippet.end
    }
}
