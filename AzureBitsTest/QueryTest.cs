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
    public class QueryTest
    {
        [TestMethod]
        public void GetReads()
        {
            HistoricalTableQuery<DeviceRead> query = new HistoricalTableQuery<DeviceRead>(
                AccountHelper.GetTable( "reads" ));

            List<object> ids = new List<object>();
            ids.Add( "NET12345678" );
            ids.Add( "NET98765432" );

            List<DeviceRead> reads = query.ParallelGet(ids);

            foreach (DeviceRead read in reads)
            {
                Console.WriteLine(read.Data);
            }
        }
    }
}
