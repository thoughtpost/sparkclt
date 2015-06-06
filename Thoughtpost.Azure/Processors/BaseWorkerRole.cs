using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

namespace Thoughtpost.Azure.Processors
{
    // BASED ON BRIAN HITNEYS EXAMPLES
    public abstract class BaseWorkerRole : RoleEntryPoint
    {
        TimeSpan HEALTH_CHECK_SLEEP = TimeSpan.FromSeconds(5);
        TimeSpan COUNTER_RESET_CHECK = TimeSpan.FromMinutes(1);

        private volatile bool onStopCalled = false;
        private volatile bool returnedFromRunMethod = false;
        
        protected virtual TimeSpan HealthCheckSleep
        {
            get
            {
                return HEALTH_CHECK_SLEEP;
            }
        }

        protected virtual TimeSpan CounterResetCheck
        {
            get
            {
                return COUNTER_RESET_CHECK;
            }
        }

        protected virtual string RoleName
        {
            get
            {
                return "BaseWorkerRole";
            }
        }

        protected abstract ProcessorRecord[] CreateProcessors();

        public override void Run()
        {
            //define the processors
            ProcessorRecord[] processors = CreateProcessors();

            //start up each one
            try
            {
                foreach (ProcessorRecord pr in processors)
                {
                    pr.Start();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }

            // now just monitor the health of the threads that we have created.
            while (true)
            {
                Thread.Sleep(HEALTH_CHECK_SLEEP);
                
                try
                {
                    foreach (ProcessorRecord pr in processors)
                    {
                        if (pr.Processor.FatalException != null)
                        {
                            Trace.TraceError(string.Format("Fatal exception - {0}",
                                pr.Processor.FatalException.Message));
                            returnedFromRunMethod = true;
                            return;
                        }

                        //if (pr.Processor.ProcessTime.TotalSeconds > 0)
                        if (pr.Processor.IsEventDriven == false)
                        {
                            //set an acceptable timeout value.
                            DateTime timeOut = pr.LastThreadTest.Add(pr.Processor.ProcessTime)
                                .Add(TimeSpan.FromMilliseconds(pr.CurrentSleepTimeMs)
                                .Add(HEALTH_CHECK_SLEEP)
                                );

                            if (pr.IsActive() && DateTime.Now > timeOut)
                            {
                                string message = string.Format("LastThreadTest: {0} | Current Time: {1} | Process Time (s): {2}",
                                    pr.LastThreadTest.ToString("HH:mm:ss"),
                                    DateTime.Now.ToString("HH:mm:ss"),
                                    pr.Processor.ProcessTime.TotalSeconds);

                                Trace.TraceWarning(string.Format("{0} failed health check.", pr.Name));
                                Trace.TraceWarning(message);

                                pr.ResetProcessor();
                            }
                        }
                        else
                        {
                            if (pr.LastProcessCounter < pr.Processor.ProcessCounter ||
                                pr.LastCounterChange == DateTime.MinValue)
                            {
                                pr.LastCounterChange = DateTime.Now;

                                Trace.TraceInformation("LastCounterChange updated to " +
                                    pr.LastCounterChange.ToString("HH:mm:ss"));
                            }

                            //set an acceptable timeout value.
                            DateTime timeOut = pr.LastCounterChange.AddMinutes(pr.Processor.ProcessTime.TotalMinutes);

                            if (pr.IsActive() && DateTime.Now > timeOut)
                            {
                                string message = string.Format("LastProcessCounter: {0} | Current Time: {1} | Last Counter: {2}",
                                    pr.LastCounterChange.ToString("HH:mm:ss"),
                                    DateTime.Now.ToString("HH:mm:ss"),
                                    pr.LastProcessCounter.ToString());

                                Trace.TraceWarning(string.Format("{0} failed health check by counter.", pr.Name));
                                Trace.TraceWarning(message);

                                pr.ResetProcessor();
                            }

                            pr.LastProcessCounter = pr.Processor.ProcessCounter;
                            Trace.TraceInformation("Counter is currently " + pr.LastProcessCounter.ToString());
                        }

                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }


                // If OnStop has been called, return to do a graceful shutdown.
                if (onStopCalled == true)
                {
                    Trace.TraceInformation("onStopCalled WorkerRole");

                    foreach (ProcessorRecord pr in processors)
                    {
                        pr.Stop();

                        if (pr.Processor.IsEventDriven)
                        {
                            pr.Processor.Cleanup();
                        }
                    }

                    bool stillRunning = true;

                    while (stillRunning)
                    {
                        stillRunning = false;

                        foreach (ProcessorRecord pr in processors)
                        {
                            if (pr.Processor.IsStoppedCleanly == false)
                            {
                                stillRunning = true;
                                System.Threading.Thread.Sleep(1000);
                                break;
                            }
                        }
                    }

                    returnedFromRunMethod = true;
                    return;
                } 
            }
        }

        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            Trace.TraceInformation("OnStart");

            return base.OnStart();
        }

        public override void OnStop()
        {
            onStopCalled = true;
            Trace.TraceInformation("OnStop called from Worker Role.");
            while (returnedFromRunMethod == false)
            {
                Trace.TraceInformation("Waiting for returnedFromRunMethod");
                System.Threading.Thread.Sleep(1000);
            }
            Trace.TraceInformation("returnedFromRunMethod is true, so restarting"); 

            base.OnStop();
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                // Set e.Cancel to true to restart this role instance
                e.Cancel = true;
            }
        }
    }
}
