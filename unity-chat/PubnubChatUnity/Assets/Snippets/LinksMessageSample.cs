// snippet.using
using System;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class LinksMessageSample
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
    
    public static async Task AddLinkMentionExample()
    {
        // snippet.add_link_mention_example
        var channelResult = await chat.GetChannel("offtopic");
        if (channelResult.Error) return;
        var testChannel = channelResult.Result;
        
        // Create a message draft
        var messageDraft = testChannel.CreateMessageDraft();

        // Update the message with the initial text
        messageDraft.Update("Hello Alex! I have sent you this link on the #offtopic channel.");

        // Add a URL to the word "link"
        messageDraft.AddMention(33, 4, new MentionTarget
        {
            Target = "https://example.com",
            Type = MentionType.Url
        });
        // snippet.end
    }
    
    public static void RemoveLinkMentionExample(MessageDraft messageDraft)
    {
        // snippet.remove_link_mention_example
        // assume the message reads
        // Hello Alex! I have sent you this link on the #offtopic channel.`

        // remove the link mention
        messageDraft.RemoveMention(33);
        // snippet.end
    }
    
    public static async Task LinkSuggestionsExample()
    {
        // snippet.link_suggestions_example
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

        messageDraft.Update("Alex, update the link to https://www.pubnub.com ");
        // snippet.end
    }
    
    public static async Task GetTextLinksExample()
    {
        // snippet.get_text_links_example
        // reference the "support" channel
        var channelResult = await chat.GetChannel("support");
        if (!channelResult.Error)
        {
            var channel = channelResult.Result;
            // get the message with the specific timetoken
            var messageResult = await channel.GetMessage("16200000000000000");
            if (!messageResult.Error)
            {
                var message = messageResult.Result;
                // check if the message contains any text links
                if (message.TextLinks != null && message.TextLinks.Count > 0)
                {
                    Console.WriteLine("The message contains the following text links:");
                    foreach (var textLink in message.TextLinks)
                    {
                        Console.WriteLine($"Text Link: {textLink.Link}");
                    }
                }
                else
                {
                    Console.WriteLine("The message does not contain any text links.");
                }
            }
            else
            {
                Console.WriteLine("Message with specified timetoken not found.");
            }
        }
        else
        {
            Console.WriteLine("Channel 'support' not found.");
        }
        // snippet.end
    }
}
