using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Thoughtpost.Azure.Processors
{
    public partial class QueueProcessorConfiguration<T>
            where T : ITableQueueEntity, new()
    {
        public delegate void HandleEntity(T entity);

        public string SourceName { get; set; }
        public string ErrorName { get; set; }
        public string TargetName { get; set; }
        public string TableName { get; set; }
        public HandleEntity Handler { get; set; }
    }
}
