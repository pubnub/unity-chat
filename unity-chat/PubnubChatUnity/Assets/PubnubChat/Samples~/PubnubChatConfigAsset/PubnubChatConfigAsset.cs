using PubnubChatApi.Entities.Data;
using UnityEngine;

namespace PubnubChat
{
    [CreateAssetMenu(fileName = "PubnubChatConfigAsset", menuName = "PubNub/PubNub Chat Config Asset")]
    public class PubnubChatConfigAsset : ScriptableObject
    {
        [field: SerializeField] public int TypingTimeout { get; private set; } = 5000;
        [field: SerializeField] public int TypingTimeoutDifference { get; private set; } = 1000;
        [field: SerializeField] public int RateLimitFactor { get; private set; }
        [field: SerializeField] public PubnubChatConfig.RateLimitPerChannel RateLimitPerChannel { get; private set; }
        [field: SerializeField] public bool StoreUserActivityTimestamp { get; private set; }
        [field: SerializeField] public int StoreUserActivityInterval { get; private set; } = 60000;

        public static implicit operator PubnubChatConfig(PubnubChatConfigAsset asset)
        {
            return new PubnubChatConfig(
                asset.TypingTimeout, asset.TypingTimeoutDifference, 
                rateLimitFactor: asset.RateLimitFactor,
                rateLimitPerChannel: asset.RateLimitPerChannel,
                storeUserActivityInterval: asset.StoreUserActivityInterval,
                storeUserActivityTimestamp: asset.StoreUserActivityTimestamp);
        }
    }
}