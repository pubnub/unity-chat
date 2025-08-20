using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PubnubChatApi.Entities.Data;
using PubnubChatApi.Utilities;

namespace PubNubChatAPI.Entities
{
    public class ThreadChannel : Channel
    {
        public string ParentChannelId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        
        public Message ParentMessage
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        
        internal ThreadChannel(Chat chat, string channelId, ChatChannelData data) : base(chat, channelId, data)
        {
        }
        
        internal static string MessageToThreadChannelId(Message message)
        {
            return $"PUBNUB_INTERNAL_THREAD_{message.ChannelId}_{message.Id}";
        }
        
        public override async Task<ChatOperationResult> PinMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public override async Task<ChatOperationResult> UnpinMessage()
        {
            throw new NotImplementedException();
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