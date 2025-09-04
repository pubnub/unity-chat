using System;
using System.Threading.Tasks;
using PubnubChatApi.Enums;

namespace PubNubChatAPI.Entities
{
    public class ChatAccessManager
    {

        private Chat chat;

        internal ChatAccessManager(Chat chat)
        {
            this.chat = chat;
        }

        public async Task<bool> CanI(PubnubAccessPermission permission, PubnubAccessResourceType resourceType, string resourceName)
        {
            throw new NotImplementedException();
        }
    }
}