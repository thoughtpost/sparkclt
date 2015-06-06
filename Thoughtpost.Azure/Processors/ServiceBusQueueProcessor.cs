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
    public partial class ServiceBusQueueProcessor<T> : IProcessMessages
            where T : ITableQueueEntity, new()
    {
        public ServiceBusQueueProcessor(QueueProcessorConfiguration<T> configuration)
        {
            this.Configuration = configuration;

            queueSource = AccountHelper.GetQueueClient(configuration.SourceName);

            if (!string.IsNullOrEmpty(configuration.TargetName))
            {
                queueTarget = AccountHelper.GetCloudQueue(configuration.TargetName);
            }
            if (!string.IsNullOrEmpty(configuration.TableName))
            {
                table = AccountHelper.GetTable(configuration.TableName);
            }
        }

        public QueueProcessorConfiguration<T> Configuration { get; set; }

        private TimeSpan _ProcessTime = TimeSpan.FromMinutes(10);
        private int counter = 0;
        private bool stopFlag = false;
        private bool isStoppedCleanly = false;

        private QueueClient queueSource;
        private CloudQueue queueTarget;
        private CloudTable table = null;

        #region IProcessMessages

        public Exception FatalException
        {
            get
            {
                return null;
            }
        }

        public void Stop()
        {
            this.stopFlag = true;
        }

        public bool IsStoppedCleanly
        {
            get { return isStoppedCleanly; }
        }

        public TimeSpan ProcessTime
        {
            get { return _ProcessTime; }
        }

        public int ProcessCounter
        {
            get { return counter; }
        }

        private void LogErrors(object sender, ExceptionReceivedEventArgs e)
        {
            if (e.Exception != null)
            {
                Trace.WriteLine(e.Exception.Message);
            }
        }

        public bool Process()
        {
            try
            {
                Trace.WriteLine("Starting event handler for queue");

                TimeSpan tsWait = TimeSpan.FromSeconds(30);

                BrokeredMessage receivedMessage = queueSource.Receive(tsWait);

                while (receivedMessage != null)
                {
                    if (this.stopFlag) break;

                    T msg = default(T);

                    StorageHelper<T> helper = new StorageHelper<T>();

                    try
                    {
                        msg = receivedMessage.GetBody<T>();

                        Trace.WriteLine("Processing message: " + msg.RowKey);

                        this.Configuration.Handler(msg);

                        if (this.table != null)
                        {
                            helper.SaveToTable(msg, table);
                        }

                        if (this.queueTarget != null)
                        {
                            helper.SaveToQueue(msg, queueTarget);
                        }

                        counter++;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine( ex.Message);
                        Trace.WriteLine( ex.StackTrace);

                        // Handle any message processing specific exceptions here
                        if (msg != null)
                        {
                            msg.ExceptionDetails = ex.Message;

                            CloudQueue qcError = AccountHelper.GetCloudQueue(this.Configuration.ErrorName);

                            helper.SaveToQueue(msg, qcError);
                        }
                    }
                    finally
                    {
                        receivedMessage.Complete();
                    }

                    if (this.stopFlag)
                    {
                        break;
                    }

                    receivedMessage = queueSource.Receive(tsWait);
                }

                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine( ex.Message);
                Trace.WriteLine(ex.StackTrace);

                return false;
            }
            finally
            {

            }
        }

        public bool Cleanup()
        {
            if (queueSource != null)
            {
                queueSource = null;
            }

            if (queueTarget != null)
            {
                queueTarget = null;
            }

            if (table != null)
            {
                table = null;
            }

            this.isStoppedCleanly = true;

            return true;
        }

        public bool IsEventDriven
        {
            get { return true; }
        }

        #endregion
    }
}
