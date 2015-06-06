using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
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

namespace Thoughtpost.Azure
{
    public class AccountHelper
    {
        // PUT YOUR CONNECTION STRINGS HERE
        protected static string ServiceBusConnectionString = "";
        protected static string StorageConnectionString = "";

        static AccountHelper()
        {
            ServiceBusConnectionString = ConfigurationHelper.GetSetting("ServiceBusConnectionString");
            StorageConnectionString = ConfigurationHelper.GetSetting("StorageConnectionString");
        }

        public static Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient GetBlobClient()
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            return blobClient;
        }

        public static CloudBlobContainer GetBlobContainer(string container)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container. 
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(container);

            // Create the container if it doesn't already exist.
            blobContainer.CreateIfNotExists();

            blobContainer.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess =
                        BlobContainerPublicAccessType.Blob
                });

            return blobContainer;
        }

        public static TopicClient GetTopicClient(string topicName)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ServiceBusConnectionString);

            if (!namespaceManager.TopicExists(topicName))
            {
                namespaceManager.CreateTopic(topicName);
            }

            // Initialize the connection to Service Bus Queue
            TopicClient tc = TopicClient.CreateFromConnectionString(ServiceBusConnectionString, topicName);

            tc.RetryPolicy = GetServiceBusRetryPolicy(topicName);

            return tc;
        }

        public static SubscriptionClient GetSubscriptionClient(string topicName, string subscriptionName)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ServiceBusConnectionString);

            if (!namespaceManager.TopicExists(topicName))
            {
                namespaceManager.CreateTopic(topicName);
            }

            if (!namespaceManager.SubscriptionExists(topicName, subscriptionName))
            {
                namespaceManager.CreateSubscription(topicName, subscriptionName);
            }

            // Initialize the connection to Service Bus Queue
            SubscriptionClient sc = SubscriptionClient.CreateFromConnectionString(ServiceBusConnectionString, topicName, subscriptionName);

            sc.RetryPolicy = GetServiceBusRetryPolicy(subscriptionName);
            //sc.PrefetchCount = 100;

            return sc;
        }

        public static SubscriptionClient GetSubscriptionClientWithFilter(string topicName, string subscriptionName,
            string sqlFilter)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ServiceBusConnectionString);

            SqlFilter filter = new SqlFilter(sqlFilter);

            if (!namespaceManager.TopicExists(topicName))
            {
                namespaceManager.CreateTopic(topicName);
            }

            if (!namespaceManager.SubscriptionExists(topicName, subscriptionName))
            {
                namespaceManager.CreateSubscription(topicName, subscriptionName, filter);
            }

            // Initialize the connection to Service Bus Queue
            SubscriptionClient sc = SubscriptionClient.CreateFromConnectionString(ServiceBusConnectionString, topicName, subscriptionName);

            sc.RetryPolicy = GetServiceBusRetryPolicy(topicName);
            sc.PrefetchCount = 10;

            return sc;
        }

        public static CloudQueue GetCloudQueue(string queueName)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Queue.CloudQueueClient client = storageAccount.CreateCloudQueueClient();

            client.DefaultRequestOptions.RetryPolicy = GetQueueRetryPolicy(queueName);

            // Create the table if it doesn't exist.
            CloudQueue q = client.GetQueueReference(queueName);

            q.CreateIfNotExists();

            return q;
        }

        public static CloudQueue GetCloudQueueIfExists(string queueName)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Queue.CloudQueueClient client = storageAccount.CreateCloudQueueClient();

            client.DefaultRequestOptions.RetryPolicy = GetQueueRetryPolicy(queueName);

            // Create the table if it doesn't exist.
            CloudQueue q = client.GetQueueReference(queueName);

            if (q.Exists() == false) return null;

            return q;
        }

        public static QueueClient GetQueueClient(string queueName)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ServiceBusConnectionString);
            if (!namespaceManager.QueueExists(queueName))
            {
                namespaceManager.CreateQueue(queueName);
            }

            // Initialize the connection to Service Bus Queue
            QueueClient queueClient = QueueClient.CreateFromConnectionString(ServiceBusConnectionString, queueName);

            queueClient.RetryPolicy = GetServiceBusRetryPolicy(queueName);
            //queueClient.PrefetchCount = 100;

            return queueClient;
        }

        public static Microsoft.WindowsAzure.Storage.Table.CloudTable GetTable(string tableName)
        {
            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(
                StorageConnectionString);

            ServicePoint tableServicePoint = ServicePointManager.FindServicePoint(storageAccount.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;

            Microsoft.WindowsAzure.Storage.Table.CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            tableClient.DefaultRequestOptions.RetryPolicy = GetTableRetryPolicy(tableName);

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();

            return table;
        }

        // TODO - Make the retry parameters configurable
        public static RetryPolicy GetServiceBusRetryPolicy(string name)
        {
            return new RetryExponential(
                TimeSpan.FromSeconds(2),    // min backoff
                TimeSpan.FromSeconds(10),    // max backoff
                //TimeSpan.FromSeconds(1),    // Time between retries
                //TimeSpan.FromSeconds(60),    // Time between retries
                10                          // number of retries
                );
        }

        // TODO - Make the retry parameters configurable
        public static IRetryPolicy GetTableRetryPolicy(string tableName)
        {
            return new EventExponentialRetry(
                TimeSpan.FromSeconds(2),    // Time between retries
                10,
                tableName);                         // Number of retries
        }

        public static IRetryPolicy GetQueueRetryPolicy(string tableName)
        {
            return new EventExponentialRetry(
                TimeSpan.FromSeconds(2),    // Time between retries
                10,
                tableName);                         // Number of retries
        }

    }
}
