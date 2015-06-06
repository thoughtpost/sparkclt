using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

using Newtonsoft.Json;

namespace Thoughtpost.Azure
{
    public class DeviceRead : TableEntity, ITableQueueEntity
    {
        public DeviceRead() 
        {
            NetworkID = string.Empty;
            DeviceID = string.Empty;
            ReadTimestamp = DateTimeUtility.GetESTNow();
            Data = string.Empty;
            ExceptionDetails = string.Empty;
        }

        public string NetworkID { get; set; }
        public string DeviceID { get; set; }
        public DateTime ReadTimestamp { get; set; }
        public int ReadNumber { get; set; }

        public string Data { get; set; }

        public string ExceptionDetails { get; set; }

        public void SetKeys()
        {
            if (string.IsNullOrEmpty( this.DeviceID ))
            {
                this.PartitionKey = this.NetworkID.ToString();
            }
            else
            {
                this.PartitionKey = this.DeviceID;
            }

            this.RowKey = DateTimeUtility.InverseTimeKey(this.ReadTimestamp) + "-" + this.ReadNumber.ToString("d9");
        }

    }
}
