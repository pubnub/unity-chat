using System;
using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi
{
    public class ChatOperationResult
    {
        public bool Error { get; internal set; }
        public List<PNStatus> InternalStatuses { get; internal set; } = new();
        public Exception Exception { get; internal set; }

        internal string OperationName { get; }
        protected Chat chat;
        
        internal ChatOperationResult(string operationName, Chat chat)
        { 
            OperationName = operationName;
            this.chat = chat;
        }
        
        /// <summary>
        /// Registers a single PNResult to this overall Chat Operation Result.
        /// Returns pubnubResult.Status.Error
        /// </summary>
        internal bool RegisterOperation<T>(PNResult<T> pubnubResult)
        {
            InternalStatuses.Add(pubnubResult.Status);
            chat.Logger.Debug($"Chat operation \"{OperationName}\" registered PN Status: {chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(pubnubResult.Status)}");
            Error = pubnubResult.Status.Error;
            if (Error)
            {
                chat.Logger.Debug($"Chat operation \"{OperationName}\" registered PN Status with error: {pubnubResult.Status.ErrorData.Information}");
                Exception = pubnubResult.Status.ErrorData.Throwable;
            }
            return Error;
        }

        /// <summary>
        /// Registers another ChatOperationResult to this ChatOperationResult.
        /// Returns otherChatResult.Error
        /// </summary>
        internal bool RegisterOperation(ChatOperationResult otherChatResult)
        {
            foreach (var status in otherChatResult.InternalStatuses)
            {
                chat.Logger.Debug($"Chat operation \"{OperationName}\" registered PN Status from operation \"{otherChatResult.OperationName}\": {chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(status)}");
                InternalStatuses.Add(status);

            }
            if (otherChatResult.Error)
            {
                chat.Logger.Debug($"Chat operation \"{OperationName}\" registered PN Status from operation \"{otherChatResult.OperationName}\" with error: {otherChatResult.Exception.Message}");
            }
            Exception = otherChatResult.Exception;
            Error = otherChatResult.Error;
            return Error;
        }
    }
    
    public class ChatOperationResult<T> : ChatOperationResult
    {
        internal ChatOperationResult(string operationName, Chat chat) : base(operationName, chat)
        {
        }
        
        public T Result { get; internal set; }
    }
}