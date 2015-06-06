using System;
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

using Newtonsoft.Json;

namespace Thoughtpost.Azure
{
    public class StorageHelper<T> where T : ITableQueueEntity, new()
    {
        public TableResult SaveToTable(T entity, string tableName)
        {
            CloudTable t = AccountHelper.GetTable(tableName);

            return SaveToTable(entity, t);
        }

        public TableResult SaveToTable(T entity, CloudTable table)
        {
            entity.SetKeys();

            TableOperation insertOp = TableOperation.InsertOrMerge(entity);

            return table.Execute(insertOp);
        }

        public void SaveToQueue(T entity, string queueName)
        {
            CloudQueue queue = AccountHelper.GetCloudQueue(queueName);

            SaveToQueue(entity, queue);
        }

        public void SaveToQueue(T entity, CloudQueue queue)
        {
            queue.AddMessage(new CloudQueueMessage(ToJson(entity)));
        }
        
        public static T FromJson(string json)
        {
            T entity = default(T);

            entity = JsonConvert.DeserializeObject<T>(json);
            entity.SetKeys();

            return entity;
        }

        public static string ToJson(T entity)
        {
            string json = JsonConvert.SerializeObject(entity);

            return json;
        }
    }
}
