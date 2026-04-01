using TMPro;
using UnityEngine;

/// <summary>
/// Simple UI controller for displaying Pubnub Messages.
/// </summary>
public class PubnubMessageUIController : MonoBehaviour
{
    [SerializeField] private GameObject TheirBubble;
    [SerializeField] private TextMeshProUGUI TheirText;
    [SerializeField] private TextMeshProUGUI TheirMeta;
    [SerializeField] private GameObject MyBubble;
    [SerializeField] private TextMeshProUGUI MyText;
    [SerializeField] private TextMeshProUGUI MyMeta;

    public void Initialize(string text, string meta, bool isMine)
    {
        TheirBubble.SetActive(!isMine);
        MyBubble.SetActive(isMine);
        
        var textUi = isMine ? MyText : TheirText;
        var metaUi = isMine ? MyMeta : TheirMeta;

        textUi.text = text;
        metaUi.text = meta;
    }
}
