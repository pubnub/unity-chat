using System.Collections.Generic;
using PubnubChatApi.Entities.Events;

namespace PubnubChatApi.Entities.Data
{
    public struct EventsHistoryWrapper
    {
        public List<ChatEvent> Events;
        public bool IsMore;
    }
}