using System;
using PubnubChatApi.Entities.Data;
using UnityEngine;

namespace PubnubChat
{
    [CreateAssetMenu(fileName = "PubnubChatConfigAsset", menuName = "PubNub/PubNub Chat Config Asset")]
    public class PubnubChatConfigAsset : ScriptableObject
    {
        [field: SerializeField] public string PublishKey { get; private set; }
        [field: SerializeField] public string SubscribeKey { get; private set; }
        [field: SerializeField] public string UserId { get; private set; }
        [field: SerializeField] public string AuthKey { get; private set; }
        [field: SerializeField] public int TypingTimeout { get; private set; } = 5000;
        [field: SerializeField] public int TypingTimeoutDifference { get; private set; } = 1000;

        public static implicit operator PubnubChatConfig(PubnubChatConfigAsset asset)
        {
            if (string.IsNullOrEmpty(asset.UserId))
            {
                throw new NullReferenceException("You need to set the UserId before passing configuration");
            }

            if (string.IsNullOrEmpty(asset.PublishKey))
            {
                throw new NullReferenceException("You need to set the PublishKey before passing configuration");
            }

            if (string.IsNullOrEmpty(asset.SubscribeKey))
            {
                throw new NullReferenceException("You need to set the SubscribeKey before passing configuration");
            }

            return new PubnubChatConfig(asset.PublishKey, asset.SubscribeKey, asset.UserId, asset.AuthKey,
                asset.TypingTimeout, asset.TypingTimeoutDifference);
        }
    }
}