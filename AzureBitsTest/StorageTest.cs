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

namespace AzureBitsTest
{
    [TestClass]
    public class StorageTest
    {
        [TestMethod]
        public void SaveRead()
        {
            DeviceRead read = new DeviceRead();
            read.NetworkID = "NET98765432";
            read.ReadTimestamp = DateTimeUtility.GetESTNow();
            read.ReadNumber = 1;
            read.Data = "MORE DATA";

            StorageHelper<DeviceRead> helper = new StorageHelper<DeviceRead>();
            helper.SaveToTable(read, "reads");
        }

        [TestMethod]
        public void SaveAnother()
        {
            DeviceRead read= new DeviceRead();
            read.NetworkID = "NET12345678";
            read.ReadTimestamp = DateTimeUtility.GetESTNow();
            read.ReadNumber = 1;
            read.Data = "ANOTHER ONE";

            StorageHelper<DeviceRead> helper = new StorageHelper<DeviceRead>();
            helper.SaveToTable(read, "reads");
        }
    }
}
