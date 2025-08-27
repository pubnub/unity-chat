using System;
using System.Threading.Tasks;
using PubnubChatApi.Enums;

namespace PubNubChatAPI.Entities
{
    //TODO: now that core SDK is exposed do we still need SetAuthToken, ParseToken, and SetPubnubOrigin?
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
        
        /// <summary>
        /// Sets a new token for this Chat instance.
        /// </summary>
        public void SetAuthToken(string token)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Decodes an existing token.
        /// </summary>
        /// <returns>A JSON string object containing permissions embedded in that token.</returns>
        public string ParseToken(string token)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets a new custom origin value.            
        /// </summary>
        /// <param name="origin"></param>
        public void SetPubnubOrigin(string origin)
        {
            throw new NotImplementedException();
        }
    }
}