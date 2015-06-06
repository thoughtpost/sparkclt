using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Thoughtpost.Azure
{
    public class RetryEventArgs : EventArgs
    {
        public string TableName { get; set; }
        public int CurrentRetryCount { get; set; }
        public int StatusCode { get; set; }
        public Exception LastException { get; set; }
        public TimeSpan RetryInterval { get; set; }
        public OperationContext OperationContext { get; set; }

        public RetryEventArgs(int currentRetryCount, int statusCode, Exception lastException, TimeSpan retryInterval, OperationContext operationContext)
        {
            this.CurrentRetryCount = currentRetryCount;
            this.StatusCode = statusCode;
            this.LastException = lastException;
            this.RetryInterval = retryInterval;
            this.OperationContext = operationContext;
        }
    }
}
