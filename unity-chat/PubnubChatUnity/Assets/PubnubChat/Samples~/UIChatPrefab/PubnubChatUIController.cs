using System;
using System.Collections.Generic;
using PubnubApi;
using PubnubChatApi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Channel = PubnubChatApi.Channel;

/// <summary>
/// A simple UI implementation of a Pubnub Chat.
/// </summary>
public class PubnubChatUIController : MonoBehaviour
{
    [Header("Pubnub settings")]
    [SerializeField] private string publishKey;
    [SerializeField] private string subscribeKey;
    [SerializeField] private string secretKey;
    [SerializeField] private string userId;
    [SerializeField] private string channelId;
    [SerializeField] private bool fetchChannelHistoryOnStart;
    [SerializeField] private int displayedMessagesLimit = 50;
    [Header("UI references")]
    [SerializeField] private PubnubMessageUIController messageUIPrefab;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private RectTransform scrollViewContent;
    [SerializeField] private TextMeshProUGUI information;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;

    private Stack<PubnubMessageUIController> displayedMessages = new ();
    private Channel channel;
    
    private async void Awake()
    {
        //Displaying the ID, alternatively you can use channel.Name after it has been fetched later
        information.text = $"Channel: {channelId}";
        
        //Create a chat and abort if there was an error
        var createChat = await UnityChat.CreateInstance(
            new PubnubChatConfig(), 
            new PNConfiguration(new UserId(userId))
            {
                PublishKey = publishKey, 
                SubscribeKey = subscribeKey, 
                SecretKey = secretKey
            }, false);
        if (createChat.Error)
        {
            Debug.LogError($"Failed to initialize chat! Error message: {createChat.Exception.Message}");
            return;
        }
        var chat = createChat.Result;
        
        //Create a channel and abort if there was an error
        //Note that if a channel with the given ID already exists the result of this operation will simply return it
        var createChanel = await chat.CreatePublicConversation(channelId);
        if (createChanel.Error)
        {
            Debug.LogError($"Failed to create channel! Error message: {createChanel.Exception.Message}");
            return;
        }
        channel = createChanel.Result;
        
        //Assign listeners for sending message
        sendButton.onClick.AddListener(SendMessage);
        messageInput.onSubmit.AddListener(delegate{SendMessage();});
        
        //If the setting is enabled messages will be fetched from the Channel history
        if (fetchChannelHistoryOnStart)
        {
            //Hardcoded for 5h for demonstration purpose
            var getHistory = await channel.GetMessageHistory(ChatUtils.TimeTokenNow(),
                ChatUtils.TimeToken(DateTime.UtcNow.Subtract(new TimeSpan(5, 0, 0))), displayedMessagesLimit);
            if (getHistory.Error)
            {
                Debug.LogError($"Failed to fetch history! Error message: {getHistory.Error}");
            }
            else
            {
                var history = getHistory.Result;
                foreach (var message in history)
                {
                    DisplayMessage(message);
                }
            }
        }
        //Setting up the callback for incoming messages
        channel.OnMessageReceived += OnMessageReceived;
        //Connecting to the channel (OnMessageReceived won't start triggering without this)
        //Conversely, to stop receiving OnMessageReceived you can call channel.Disconnect()
        channel.Connect();
    }

    private void SendMessage()
    {
        var text = messageInput.text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        messageInput.text = string.Empty;
        //Not awaiting to not block UI
        channel.SendText(text);
    }

    private void OnMessageReceived(Message message)
    {
        DisplayMessage(message);
    }

    private void DisplayMessage(Message message)
    {
        //Destroying messages if limit is reached (can be useful for performance)
        if (displayedMessages.Count >= displayedMessagesLimit)
        {
            var poppedMessage = displayedMessages.Pop();
            Destroy(poppedMessage.gameObject); 
        }
        var messageUi = Instantiate(messageUIPrefab, scrollViewContent);
        messageUi.gameObject.SetActive(true);
        //Pubnub timetokens are in UTC so to display human-readable time the ToLocalTime() call is needed
        var messageSendDate = Pubnub.TranslatePubnubUnixNanoSecondsToDateTime(message.TimeToken).ToLocalTime();
        messageUi.Initialize(message.MessageText, $"<b>{message.UserId}</b>, {messageSendDate:yyyy-MM-dd HH:mm:ss}",message.UserId == userId);
        displayedMessages.Push(messageUi);
        scrollView.normalizedPosition = new Vector2(0, 0);
    }

    //For listeners cleanup
    private void OnDestroy()
    {
        sendButton.onClick.RemoveListener(SendMessage);
        messageInput.onSubmit.RemoveAllListeners();
        if (channel != null)
        {
            channel.OnMessageReceived -= OnMessageReceived;   
        }
    }
}
