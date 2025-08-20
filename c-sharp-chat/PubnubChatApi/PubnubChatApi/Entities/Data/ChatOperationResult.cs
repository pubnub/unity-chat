using System;
using System.Collections.Generic;
using PubnubApi;

namespace PubnubChatApi.Entities.Data
{
    public class ChatOperationResult
    {
        public bool Error { get; internal set; }
        public List<PNStatus> InternalStatuses { get; internal set; } = new();
        public Exception Exception { get; internal set; }
        
        /// <summary>
        /// Registers a single PNResult to this overall Chat Operation Result.
        /// Returns pubnubResult.Status.Error
        /// </summary>
        internal bool RegisterOperation<T>(PNResult<T> pubnubResult)
        {
            InternalStatuses.Add(pubnubResult.Status);
            Error = pubnubResult.Status.Error;
            if (Error)
            {
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
            InternalStatuses.AddRange(otherChatResult.InternalStatuses);
            Exception = otherChatResult.Exception;
            Error = otherChatResult.Error;
            return Error;
        }
    }
    
    public class ChatOperationResult<T> : ChatOperationResult
    {
        public T Result { get; internal set; }
    }
}