using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PubnubApi;

namespace PubnubChatApi
{
    public class MutedUsersManager
    {
        public List<string> MutedUsers { get; private set; } = new();

        private Chat chat;
        private string userId;
        private string userMuteChannelId;
        
        internal MutedUsersManager(Chat chat)
        {
            this.chat = chat;
            userMuteChannelId = $"PN_PRV.{this.chat.PubnubInstance.GetCurrentUserId()}.mute1";
            if (this.chat.Config.SyncMutedUsers)
            {
                chat.PubnubInstance.AddListener(chat.ListenerFactory.ProduceListener(
                    objectEventCallback: 
                        delegate(Pubnub pn, PNObjectEventResult eventResult)
                        {
                            var uuid = eventResult.UuidMetadata.Uuid;
                            var type = eventResult.Type;
                            if (type == "uuid" && uuid == userMuteChannelId)
                            {
                                if (eventResult.Event == "set")
                                {
                                    CustomToMutedUsers(eventResult.UuidMetadata.Custom);
                                }
                                else if (eventResult.Event == "delete")
                                {
                                    MutedUsers = new List<string>();
                                }
                            }
                        }
                    )
                );
                chat.PubnubInstance.AddListener(
                    chat.ListenerFactory.ProduceListener(
                        statusCallback:
                            async delegate(Pubnub pn, PNStatus status)
                            {
                                if (status.Category is PNStatusCategory.PNConnectedCategory or PNStatusCategory.PNSubscriptionChangedCategory)
                                {
                                    if (!chat.PubnubInstance.GetSubscribedChannels().Contains(userMuteChannelId))
                                    {
                                        // the client might have been offline for a while and missed some updates so load the list first
                                        await LoadMutedUsers();
                                        this.chat.PubnubInstance.Subscribe<string>().Channels(new []{userMuteChannelId}).Execute();
                                    }
                                }
                            }
                    )
                );
                LoadMutedUsers();
            }
        }

        public async Task<ChatOperationResult> MuteUser(string userId)
        {
            var result = new ChatOperationResult("MutedUsersManager.MuteUser()", chat);
            if (MutedUsers.Contains(userId))
            {
                result.Error = true;
                result.Exception = new PNException($"User \"{userId}\" was already muted!");
                return result;
            }
            MutedUsers.Add(userId);
            if (chat.Config.SyncMutedUsers)
            {
                result.RegisterOperation(await UpdateMutedUsers());
            }
            return result;
        }
        
        public async Task<ChatOperationResult> UnMuteUser(string userId)
        {
            var result = new ChatOperationResult("MutedUsersManager.UnMuteUser()", chat);
            if (!MutedUsers.Contains(userId))
            {
                result.Error = true;
                result.Exception = new PNException($"User \"{userId}\" was already not muted!");
                return result;
            }
            MutedUsers.Remove(userId);
            if (chat.Config.SyncMutedUsers)
            {
                result.RegisterOperation(await UpdateMutedUsers());
            }
            return result;
        }

        private async Task<ChatOperationResult> UpdateMutedUsers()
        {
            var mutedUsersString = string.Join(',', MutedUsers);
            var result = await chat.PubnubInstance.SetUuidMetadata().Uuid(userMuteChannelId).Type("pn.prv").Custom(
                new Dictionary<string, object>()
                {
                    { "m", mutedUsersString }
                }).ExecuteAsync();
            return result.ToChatOperationResult("MutedUsersManager.UpdateMutedUsers()", chat);
        }

        private async Task<ChatOperationResult> LoadMutedUsers()
        {
            var result = new ChatOperationResult("MutedUsersManager.LoadMutedUsers()", chat);
            var getResult = await chat.PubnubInstance.GetUuidMetadata().Uuid(userMuteChannelId).IncludeCustom(true)
                .ExecuteAsync();
            if (result.RegisterOperation(getResult))
            {
                return result;
            }
            result.RegisterOperation(CustomToMutedUsers(getResult.Result.Custom));
            return result;
        }

        private ChatOperationResult CustomToMutedUsers(Dictionary<string, object> custom)
        {
            var result = new ChatOperationResult("MutesUsersManager.CustomToMutedUsers()", chat);
            if (custom.TryGetValue("m", out var mutedUsersObject) && mutedUsersObject != null)
            {
                try
                {
                    MutedUsers = mutedUsersObject.ToString().Split(",").ToList();
                }
                catch (Exception e)
                {
                    result.Error = true;
                    result.Exception = new PNException($"Exception in parsing synced muted users: {e.Message}");
                }
            }
            return result;
        }
    }
}