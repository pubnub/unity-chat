using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PubNubChatAPI.Entities;

namespace PubnubChatApi.Utilities
{
    internal static class PointerParsers
    {
        internal static List<Membership> ParseJsonMembershipPointers(Chat chat, string membershipPointersJson)
        {
            var memberships = new List<Membership>();
            if (CUtilities.IsValidJson(membershipPointersJson))
            {
                var membershipPointers = JsonConvert.DeserializeObject<IntPtr[]>(membershipPointersJson);
                if (membershipPointers == null)
                {
                    return memberships;
                }

                memberships = ParseJsonMembershipPointers(chat, membershipPointers);
            }

            return memberships;
        }

        internal static List<Membership> ParseJsonMembershipPointers(Chat chat, IntPtr[] membershipPointers)
        {
            var memberships = new List<Membership>();
            foreach (var membershipPointer in membershipPointers)
            {
                var id = Membership.GetMembershipIdFromPtr(membershipPointer);
                if (chat.TryGetMembership(id, membershipPointer, out var membership))
                {
                    memberships.Add(membership);
                }
            }
            return memberships;
        }
        
        internal static List<Channel> ParseJsonChannelPointers(Chat chat, string channelPointersJson)
        {
            var channels = new List<Channel>();
            if (CUtilities.IsValidJson(channelPointersJson))
            {
                var channelPointers = JsonConvert.DeserializeObject<IntPtr[]>(channelPointersJson);
                if (channelPointers == null)
                {
                    return channels;
                }

                channels = ParseJsonChannelPointers(chat, channelPointers);
            }

            return channels;
        }

        internal static List<Channel> ParseJsonChannelPointers(Chat chat, IntPtr[] channelPointers)
        {
            var channels = new List<Channel>();
            foreach (var channelPointer in channelPointers)
            {
                var id = Channel.GetChannelIdFromPtr(channelPointer);
                if (chat.TryGetChannel(id, channelPointer, out var channel))
                {
                    channels.Add(channel);
                }
            }
            return channels;
        }
        
        internal static List<User> ParseJsonUserPointers(Chat chat, string userPointersJson)
        {
            var users = new List<User>();
            if (CUtilities.IsValidJson(userPointersJson))
            {
                var userPointers = JsonConvert.DeserializeObject<IntPtr[]>(userPointersJson);
                if (userPointers == null)
                {
                    return users;
                }

                users = ParseJsonUserPointers(chat, userPointers);
            }

            return users;
        }

        internal static List<User> ParseJsonUserPointers(Chat chat, IntPtr[] userPointers)
        {
            var users = new List<User>();
            foreach (var userPointer in userPointers)
            {
                var id = User.GetUserIdFromPtr(userPointer);
                if (chat.TryGetUser(id, userPointer, out var user))
                {
                    users.Add(user);
                }
            }
            return users;
        }
    }
}