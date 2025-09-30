// snippet.using
using System;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi;

// snippet.end

public class UnreadMessageSample
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
    
    public static async Task GetLastReadMessageTimetokenExample()
    {
        // snippet.get_last_read_message_timetoken_example
        // reference the "support_agent_15" user
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            Console.WriteLine($"Found user with name {user.UserName}");
            
            // get the list of all user memberships
            var membershipsResponse = await user.GetMemberships();
            
            if (!membershipsResponse.Error)
            {
                // extract the actual memberships (support channel only) from the response
                var memberships = membershipsResponse.Result.Memberships.Where(x => x.ChannelId == "support").ToList();
                
                // since we filtered for the "support" channel, we should find it directly
                var membership = memberships.FirstOrDefault();
                
                if (membership != null)
                {
                    // retrieve the last read message timetoken
                    var lastReadToken = membership.LastReadMessageTimeToken;
                    Console.WriteLine($"The last read message timetoken for user {user.UserName} on channel 'support' is {lastReadToken}");
                }
                else
                {
                    Console.WriteLine("The user 'support_agent_15' is not a member of the 'support' channel.");
                }
            }
        }
        else
        {
            Console.WriteLine("User 'support_agent_15' not found.");
        }
        // snippet.end
    }
    
    public static async Task GetUnreadMessagesCountOneChannelExample()
    {
        // snippet.get_unread_messages_count_one_channel_example
        // reference the "support_agent_15" user
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            Console.WriteLine($"Found user with name {user.UserName}");
            
            // get the list of all user memberships using the GetMemberships method
            var membershipsResponse = await user.GetMemberships();
            
            if (!membershipsResponse.Error)
            {
                // extract the actual memberships from the response
                var memberships = membershipsResponse.Result.Memberships;
                
                // filter out the membership for the "support" channel
                var membership = memberships.FirstOrDefault(m => m.ChannelId == "support");
                
                if (membership != null)
                {
                    // retrieve the number of unread messages
                    var unreadMessagesCountResult = await membership.GetUnreadMessagesCount();
                    if (!unreadMessagesCountResult.Error)
                    {
                        Console.WriteLine($"The number of unread messages for user {user.UserName} on channel 'support' is {unreadMessagesCountResult.Result}");
                    }
                }
                else
                {
                    Console.WriteLine("The user 'support_agent_15' is not a member of the 'support' channel.");
                }
            }
        }
        else
        {
            Console.WriteLine("User 'support_agent_15' not found.");
        }
        // snippet.end
    }
    
    public static async Task GetUnreadMessagesCountAllChannelsExample()
    {
        // snippet.get_unread_messages_count_all_channels_example
        // retrieve the current user
        var currentUserResult = await chat.GetCurrentUser();
        if (!currentUserResult.Error)
        {
            var currentUser = currentUserResult.Result;
            Console.WriteLine($"Current user is {currentUser.UserName}");
            
            // retrieve the unread message counts for the current user with default parameters
            var unreadMessageCountsResult = await chat.GetUnreadMessagesCounts();
            if (!unreadMessageCountsResult.Error)
            {
                // process and display the retrieved unread message counts
                foreach (var unreadMessage in unreadMessageCountsResult.Result)
                {
                    Console.WriteLine($"Channel ID: {unreadMessage.ChannelId}, Unread Messages: {unreadMessage.Count}");
                }
            }
        }
        else
        {
            Console.WriteLine("Current user not found.");
        }
        // snippet.end
    }
    
    public static async Task SetLastReadMessageExample()
    {
        // snippet.set_last_read_message_example
        // reference the "support_agent_15" user
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            Console.WriteLine($"Found user with name {user.UserName}");

            // get the list of all user memberships
            var membershipsResponse = await user.GetMemberships();
            
            if (!membershipsResponse.Error)
            {
                // filter out the right channel
                var membership = membershipsResponse.Result.Memberships.FirstOrDefault(m => m.ChannelId == "support");
                if (membership != null)
                {
                    Console.WriteLine($"Found membership for channel: {membership.ChannelId}");

                    // reference the "support" channel
                    var channelResult = await chat.GetChannel("support");
                    if (!channelResult.Error)
                    {
                        var channel = channelResult.Result;
                        Console.WriteLine($"Found channel with name {channel.Name}");

                        // return the message object with the "16200000000000001" timetoken
                        var messageResult = await channel.GetMessage("16200000000000001");
                        if (!messageResult.Error)
                        {
                            var message = messageResult.Result;
                            Console.WriteLine($"Is deleted?: {message.IsDeleted}");

                            // set the last read message for the membership
                            await membership.SetLastReadMessage(message);
                            Console.WriteLine($"Last read message set for user {user.UserName} in channel {channel.Name}");
                        }
                        else
                        {
                            Console.WriteLine("Message with the specified timetoken not found.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Channel 'support' not found.");
                    }
                }
                else
                {
                    Console.WriteLine("Membership for 'support' channel not found.");
                }
            }
        }
        else
        {
            Console.WriteLine("User 'support_agent_15' not found.");
        }
        // snippet.end
    }
    
    public static async Task SetLastReadMessageTimetokenExample()
    {
        // snippet.set_last_read_message_timetoken_example
        // reference the "support_agent_15" user
        var userResult = await chat.GetUser("support_agent_15");
        if (!userResult.Error)
        {
            var user = userResult.Result;
            Console.WriteLine($"Found user with name {user.UserName}");

            // get the list of all user memberships
            var membershipsResponse = await user.GetMemberships();
            
            if (!membershipsResponse.Error)
            {
                // filter out the right channel
                var membership = membershipsResponse.Result.Memberships.FirstOrDefault(m => m.ChannelId == "support");
                if (membership != null)
                {
                    Console.WriteLine($"Found membership for channel: {membership.ChannelId}");

                    // reference the "support" channel
                    var channelResult = await chat.GetChannel("support");
                    if (!channelResult.Error)
                    {
                        var channel = channelResult.Result;
                        Console.WriteLine($"Found channel with name {channel.Name}");

                        // set the last read message timetoken for the membership
                        string timeToken = "16200000000000001";
                        await membership.SetLastReadMessageTimeToken(timeToken);
                        Console.WriteLine($"Last read message timetoken set for user {user.UserName} in channel {channel.Name}");
                    }
                    else
                    {
                        Console.WriteLine("Channel 'support' not found.");
                    }
                }
                else
                {
                    Console.WriteLine("Membership for 'support' channel not found.");
                }
            }
        }
        else
        {
            Console.WriteLine("User 'support_agent_15' not found.");
        }
        // snippet.end
    }
    
    public static async Task MarkAllMessagesAsReadExample()
    {
        // snippet.mark_all_messages_as_read_example
        // simulating a previously retrieved page token from the PubNub server
        string previouslyReturnedNextPageToken = "NPT";

        // create an instance of the Page class with the previously returned next page token
        PNPageObject nextPage = new PNPageObject
        {
            Next = previouslyReturnedNextPageToken
        };

        // mark a total of 50 messages as read from the next page
        var result = await chat.MarkAllMessagesAsRead(limit: 50, page: nextPage);

        // process the result as needed
        Console.WriteLine("Messages marked as read successfully");
        // snippet.end
    }
}
