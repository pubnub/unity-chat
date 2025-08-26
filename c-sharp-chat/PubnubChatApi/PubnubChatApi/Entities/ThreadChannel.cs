using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
using PubnubChatApi.Entities.Data;

namespace PubNubChatAPI.Entities
{
    public class ThreadChannel : Channel
    {
        public string ParentChannelId { get; }
        public string ParentMessageTimeToken { get; }

        private bool initialised;
        
        internal ThreadChannel(Chat chat, string channelId, string parentChannelId, string parentMessageTimeToken, ChatChannelData data) : base(chat, channelId, data)
        {
            ParentChannelId = parentChannelId;
            ParentMessageTimeToken = parentMessageTimeToken;
        }

        private async Task<ChatOperationResult> InitThreadChannel()
        {
            var result = new ChatOperationResult();
            var channelUpdate = await UpdateChannelData(chat, Id, channelData);
            if (result.RegisterOperation(channelUpdate))
            {
                return result;
            }
            chat.channelWrappers.Add(Id, this);
            result.RegisterOperation(await chat.PubnubInstance.AddMessageAction()
                .Action(new PNMessageAction() { Type = "threadRootId", Value = Id }).Channel(ParentChannelId)
                .MessageTimetoken(long.Parse(ParentMessageTimeToken)).ExecuteAsync());
            return result;
        }

        public override async Task<ChatOperationResult> SendText(string message, SendTextParams sendTextParams)
        {
            var result = new ChatOperationResult();
            if (!initialised)
            {
                if (result.RegisterOperation(await InitThreadChannel()))
                {
                    return result;
                }
                initialised = true;
            }
            return await base.SendText(message, sendTextParams);
        }

        public async Task<List<ThreadMessage>> GetThreadHistory(string startTimeToken, string endTimeToken, int count)
        {
            throw new NotImplementedException();
        }

        public async Task PinMessageToParentChannel(ThreadMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task UnPinMessageFromParentChannel()
        {
            throw new NotImplementedException();
        }
    }
}