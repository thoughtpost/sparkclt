using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;

using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

using Thoughtpost.Azure;
using Thoughtpost.Azure.Query;

namespace AzureBitsTest
{
    [TestClass]
    public class QueueTest
    {
        [TestMethod]
        public void SaveToQueue()
        {
            HistoricalTableQuery<DeviceRead> query = new HistoricalTableQuery<DeviceRead>(
                AccountHelper.GetTable("reads"));

            List<DeviceRead> reads = query.Get("NET12345678");

            StorageHelper<DeviceRead> helper = new StorageHelper<DeviceRead>();

            foreach (DeviceRead read in reads)
            {
                helper.SaveToQueue(read, "sparkqueue");
            }
        }


    }
}
