
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Thoughtpost.Azure.Processors
{
    // BASED ON BRIAN HITNEYS EXAMPLES
    public interface IProcessMessages
    {
        /// <summary>
        /// Defines an acceptable "working time" for the worker role
        /// </summary>
        TimeSpan ProcessTime { get; }

        int ProcessCounter { get; }

        bool Process();

        void Stop();

        bool Cleanup();

        bool IsEventDriven { get; }

        bool IsStoppedCleanly { get; }

        Exception FatalException { get; }
    }

    public class ProcessorRecord
    {       
        public bool StopFlag { get; private set; }
        public bool StopWhenCompleteFlag { get; private set; }

        /// <summary>
        /// Worker thread the processor will run on
        /// </summary>
        public Thread WorkerThread { get; private set; }
        
        /// <summary>
        /// Default sleep time (ms) between iterations if no work to do
        /// </summary>
        public int DefaultSleepTimeMs { get; set; }

        private int _CurrentSleeptimeMs;
        /// <summary>
        /// Current sleep time (ms) between iterations if no work to do
        /// </summary>      
        public int CurrentSleepTimeMs 
        { 
            get
            {
                return _CurrentSleeptimeMs;
            }
            set
            {              
                _CurrentSleeptimeMs = value;
            }
        }

        /// <summary>
        /// Priority of the processor
        /// </summary>
        public ThreadPriority Priority { get; set; }

        /// <summary>
        /// A descriptive name for the processor
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The last time this thread completed some work
        /// </summary>
        public DateTime LastThreadTest { get; set; }

        /// <summary>
        /// The last time this thread had its counter change
        /// </summary>
        public DateTime LastCounterChange { get; set; }

        /// <summary>
        /// The last counter value for this process
        /// </summary>
        public int LastProcessCounter { get; set; }

        /// <summary>
        /// The type that represents this processor
        /// </summary>
        private Type processorType;

        /// <summary>
        /// The instance of the processor type that we are wrapping
        /// </summary>
        public IProcessMessages Processor { get; private set; }

        /// <summary>
        /// The event to use to block instead of sleep if its an event driven processor
        /// </summary>
        public ManualResetEvent CompletedEvent = new ManualResetEvent(false);

        /// <summary>
        /// Argument to be passed to processor constructor
        /// </summary>
        public object Args = null;

        public ProcessorRecord(){}

        public ProcessorRecord(Type type)
            : this(type, type.ToString(), null, ThreadPriority.Normal, 1000, false) {}

        public ProcessorRecord(Type type, string name)
            : this(type, name, null, ThreadPriority.Normal, 1000, false) { }

        public ProcessorRecord(Type type, string name, object args)
            : this(type, name, args, ThreadPriority.Normal, 1000, false) { }

        public ProcessorRecord(Type type, string name, object args, ThreadPriority priority)
            : this(type, name, args, priority, 1000, false) {}

        public ProcessorRecord(Type type, string name, object args, ThreadPriority priority, int sleepTimeMs)
            : this(type, name, args, priority, sleepTimeMs, false) {}

        public ProcessorRecord(Type type, string name, object args,
            ThreadPriority priority, int sleepTimeMs, 
            bool autoStart )
        {
            processorType = type;
            this.Name = name;
            this.Priority = priority;
            this.DefaultSleepTimeMs = sleepTimeMs;
            this.CurrentSleepTimeMs = sleepTimeMs;

            this.StopFlag = true;
            this.StopWhenCompleteFlag = true;

            this.Args = args;
           
            ActivateProcessor();

            if (autoStart)
            {
                Start();
            }
        }
    
        private void ActivateProcessor()
        {
            try
            {
                Processor = (IProcessMessages)Activator.CreateInstance(processorType, this.Args );
            }
            catch (Exception exActivate)
            {
                throw exActivate;
            }
            LastThreadTest = DateTime.Now;
            LastCounterChange = DateTime.MinValue;
            LastProcessCounter = 0;
        }

        /// <summary>
        /// Re/create the processor instance - used in the event of an error.
        /// </summary>
        public void ResetProcessor()
        {
            if (Processor != null)
            {
                if (Processor.IsEventDriven)
                {
                    Processor.Cleanup();
                }
            }

            ActivateProcessor();

            //if the thread is alive, abort it and re-execute
            if (this.WorkerThread != null && this.WorkerThread.IsAlive)
            {
                Abort();
            }

            Start();
        }

        /// <summary>
        /// Create the processor instance if it isn't already running.
        /// If processor is already running, nothing is done.
        /// </summary>
        public void StartIfNotRunning()
        {
            if (this.WorkerThread == null || !this.WorkerThread.IsAlive)
            {
                Start();
            }          
        }

        /// <summary>
        /// Re/create the processor instance.  
        /// Throws a ThreadStateException if already running. 
        /// </summary>
        public void Start()
        {
            if (this.WorkerThread == null || !this.WorkerThread.IsAlive)
            {
                LastThreadTest = DateTime.Now;
                LastCounterChange = DateTime.MinValue;
                LastProcessCounter = 0;

                this.StopFlag = false;
                this.StopWhenCompleteFlag = false;

                this.WorkerThread = new Thread(RunProcessor.Run);
                this.WorkerThread.Name = this.Name;
                this.WorkerThread.Priority = this.Priority;
                this.WorkerThread.Start(this);                
            }
            else
            {
                throw new ThreadStateException(
                    string.Format("'{0}' is already started.", this.Name));
            }
        }

        /// <summary>
        /// Abort the current processor
        /// </summary>
        public void Abort()
        {
            if (this.WorkerThread.IsAlive)
            {
                this.StopFlag = true;
                try
                {
                    this.WorkerThread.Abort();
                    this.WorkerThread.Join();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message); 
                }
            }
        }

        /// <summary>
        /// Stop the current processor gracefully at next cycle
        /// </summary>
        public void Stop()
        {
            this.StopFlag = true;
            this.Processor.Stop();
        }

        /// <summary>
        /// Stop the current processor gracefully when no work to do
        /// </summary>
        public void StopWhenComplete()
        {
            this.StopWhenCompleteFlag = true;
            this.Processor.Stop();
        }

        /// <summary>
        /// Returns true if the processor is currently running.
        /// </summary>
        public bool IsActive()
        {
            return (this.StopFlag == false
                && this.StopWhenCompleteFlag == false);           
        }

    }
}
