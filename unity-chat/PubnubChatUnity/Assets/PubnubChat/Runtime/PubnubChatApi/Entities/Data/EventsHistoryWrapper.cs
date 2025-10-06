using System.Collections.Generic;

namespace PubnubChatApi
{
    public struct EventsHistoryWrapper
    {
        public List<ChatEvent> Events;
        public bool IsMore;
    }
}