// snippet.using
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;
using UnityEngine;
// snippet.end

public class ChannelReferencesSample
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
    
    public static async Task AddChannelReferenceExample()
    {
        // snippet.add_channel_reference_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var testChannel = channelResult.Result;
        
        // Create a message draft
        var messageDraft = testChannel.CreateMessageDraft();

        // Update the message with the initial text
        messageDraft.Update("Hello Alex! I have sent you this link on the #offtopic channel.");

        // Add a channel mention for the "#offtopic" channel
        messageDraft.AddMention(45, 9, new MentionTarget
        {
            Target = "group.offtopic", // Assuming the channel ID is "group.offtopic"
            Type = MentionType.Channel
        });
        // snippet.end
    }
    
    public static void RemoveChannelReferenceExample(MessageDraft messageDraft)
    {
        // snippet.remove_channel_reference_example
        // assume the message reads
        // Hello Alex! I have sent you this link on the #offtopic channel.

        // Remove the channel reference for "#offtopic"
        messageDraft.RemoveMention(45);
        // snippet.end
    }
    
    public static async Task InsertSuggestedChannelReferenceExample()
    {
        // snippet.insert_suggested_channel_reference_example
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

        messageDraft.Update("Alex are you a member of the #offtop channel?");
        // snippet.end
    }
    
    public static async Task CheckMessageChannelReferencesExample()
    {
        // snippet.check_message_channel_references_example
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

                // check if the message contains any channel references
                if (message.ReferencedChannels != null && message.ReferencedChannels.Count > 0)
                {
                    Debug.Log("The message contains channel references.");
                    foreach (var referencedChannel in message.ReferencedChannels)
                    {
                        Debug.Log($"Referenced Channel: {referencedChannel.Name}");
                    }
                }
                else
                {
                    Debug.Log("The message does not contain any channel references.");
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
}
