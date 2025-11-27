using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public class ChatFilesResult
    {
        public List<ChatFile> Files;
        public string Next;
        public int Total;
    }
    
    public class ChatFile
    {
        public string Name;
        public string Id;
        public string Url;
        public string Type;

        internal Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>()
            {
                { "name", Name },
                { "id", Id },
                { "url", Url },
                { "type", Type }
            };
        }
    }

    public struct ChatInputFile
    {
        public string Name;
        public string Type;
        public string Source;
    }
}