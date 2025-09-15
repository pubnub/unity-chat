using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PubnubApi;
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
            var parsed = chat.PubnubInstance.ParseToken(chat.PubnubInstance.PNConfig.AuthKey);
            Dictionary<string, PNTokenAuthValues> mapping = resourceType switch
            {
                PubnubAccessResourceType.Uuids => parsed.Resources.Uuids,
                PubnubAccessResourceType.Channels => parsed.Resources.Channels,
                _ => throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null)
            };
            var authValues = mapping[resourceName];
            switch (permission)
            {
                case PubnubAccessPermission.Read:
                    return authValues.Read;
                case PubnubAccessPermission.Write:
                    return authValues.Write;
                case PubnubAccessPermission.Manage:
                    return authValues.Manage;
                case PubnubAccessPermission.Delete:
                    return authValues.Delete;
                case PubnubAccessPermission.Get:
                    return authValues.Get;
                case PubnubAccessPermission.Join:
                    return authValues.Join;
                case PubnubAccessPermission.Update:
                    return authValues.Update;
                default:
                    throw new ArgumentOutOfRangeException(nameof(permission), permission, null);
            }
        }
    }
}