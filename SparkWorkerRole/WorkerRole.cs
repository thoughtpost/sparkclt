using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

using Thoughtpost.Azure;
using Thoughtpost.Azure.Processors;

namespace SparkWorkerRole
{
    public class WorkerRole : BaseWorkerRole
    {

        protected override ProcessorRecord[] CreateProcessors()
        {
            //define the processors
            ProcessorRecord[] processors = new ProcessorRecord[]{
                new ProcessorRecord (typeof(StorageQueueProcessor<DeviceRead>), 
                    "FirstQueue",
                    new QueueProcessorConfiguration<DeviceRead>() 
                    {
                        SourceName = "sparkqueue",
                        TargetName = "nextqueue",
                        TableName = "reads",
                        ErrorName = "errorqueue",
                        Handler = delegate (DeviceRead entity) {
                            entity.Data += "!!!PASSED THROUGH THE PROCESSOR!!!";
                        }
                    }),
            };

            return processors;
        }

    }
}
