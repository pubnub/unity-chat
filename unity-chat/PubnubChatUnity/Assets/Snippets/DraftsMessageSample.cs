// snippet.using
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class DraftsMessageSample
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
    
    public static async Task CreateMessageDraftExample()
    {
        // snippet.create_message_draft_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var channel = channelResult.Result;
        
        var messageDraft = channel.CreateMessageDraft();
        // snippet.end
    }
    
    public static async Task AddMessageDraftChangeListenerExample()
    {
        // snippet.add_message_draft_change_listener_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var channel = channelResult.Result;
        
        // Create a message draft
        var messageDraft = channel.CreateMessageDraft();

        // Enable receiving search suggestions
        messageDraft.ShouldSearchForSuggestions = true;


        // Use a dedicated callback
        void InsertDelegateCallback(List<MessageElement> elements, List<SuggestedMention> mentions)
        {
          // your logic goes here
        }

        // Add the InsertDelegateCallback function to the OnDraftUpdated event
        messageDraft.OnDraftUpdated += InsertDelegateCallback;

        // Or use a lambda
        // Event handlers added with a lambda
        messageDraft.OnDraftUpdated += (elements, mentions) =>
        {
          // your logic goes here
        };
        // snippet.end
    }
    
    public static async Task RemoveMessageDraftChangeListenerExample()
    {
        // snippet.remove_message_draft_change_listener_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var channel = channelResult.Result;
        
        // Create a message draft
        var messageDraft = channel.CreateMessageDraft();

        // Enable receiving search suggestions
        messageDraft.ShouldSearchForSuggestions = true;


        // Use a dedicated callback
        void InsertDelegateCallback(List<MessageElement> elements, List<SuggestedMention> mentions)
        {
          // your logic goes here
        }

        // Add the InsertDelegateCallback function to the OnDraftUpdated event
        messageDraft.OnDraftUpdated += InsertDelegateCallback;

        // Remove the InsertDelegateCallback function to the OnDraftUpdated event
        messageDraft.OnDraftUpdated -= InsertDelegateCallback;
        // snippet.end
    }
    
    public static async Task AddMessageElementExample()
    {
        // snippet.add_message_element_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var channel = channelResult.Result;
        
        var messageDraft = channel.CreateMessageDraft();

        // Add initial text
        messageDraft.Update("Hello Alex!");

        // Add a user mention to the string "Alex"
        messageDraft.AddMention(6, 4, new MentionTarget {
            Target = "alex_d",
            Type = MentionType.User
        });

        // Change the text
        messageDraft.Update("Hello Alex! I have sent you this link on the #offtopic channel.");

        // Add a URL mention to the string "link"
        messageDraft.AddMention(33, 4, new MentionTarget {
            Target = "www.pubnub.com",
            Type = MentionType.Url
        });

        // Add a channel mention to the string "#offtopic"
        messageDraft.AddMention(45, 9, new MentionTarget {
            Target = "group.offtopic",
            Type = MentionType.Channel
        });
        // snippet.end
    }
    
    public static void RemoveMessageElementExample(MessageDraft messageDraft)
    {
        // snippet.remove_message_element_example
        // Assume the message reads:
        // Hello Alex! I have sent you this link on the #offtopic channel.

        // Remove the link mention
        messageDraft.RemoveMention(33);
        // snippet.end
    }
    
    public static void UpdateMessageTextExample(MessageDraft messageDraft)
    {
        // snippet.update_message_text_example
        // the message reads:
        // I sent [Alex] this picture.
        // where [Alex] is a user mention
        messageDraft.Update("I did not send Alex this picture.");
        // the message now reads: 
        // I did not send [Alex] this picture.
        // the mention is preserved because its text wasn't changed
        // snippet.end
    }
    
    public static async Task InsertSuggestedMessageElementExample()
    {
        // snippet.insert_suggested_message_element_example
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
        messageDraft.InsertText(0, "maybe i'll mention @John");
        // snippet.end
    }
    
    public static void InsertMessageTextExample(MessageDraft messageDraft)
    {
        // snippet.insert_message_text_example
        // The message reads:
        // Check this support article https://www.support-article.com/.
        
        // Add "out" after "Check" (position 6, right after "Check ")
        messageDraft.InsertText(6, "out ");
        
        // The message now reads:
        // Check out this support article https://www.support-article.com/.
        // snippet.end
    }
    
    public static void RemoveMessageTextExample(MessageDraft messageDraft)
    {
        // snippet.remove_message_text_example
        // The message reads:
        // Check out this support article https://www.support-article.com/.

        messageDraft.RemoveText(5, 4);
        // The message now reads:
        // Check this support article https://www.support-article.com/.
        // snippet.end
    }
    
    public static async Task SendDraftMessageExample()
    {
        // snippet.send_draft_message_example
        var channelResult = await chat.GetChannel("support");
        if (channelResult.Error) return;
        var channel = channelResult.Result;
        
        // Create a message draft
        var messageDraft = channel.CreateMessageDraft();

        // Add initial text
        messageDraft.Update("Hello Alex!");

        // Send the message
        await messageDraft.Send();
        // snippet.end
    }
}
