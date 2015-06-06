using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Thoughtpost.Azure.Processors
{
    // BASED ON BRIAN HITNEYS EXAMPLES
    public class RunProcessor
    {        
        public static void Run(Object state)
        {
            ProcessorRecord pr = (ProcessorRecord)state;

            while (true)
            {
                try
                {
                    //check for a stop flag to exit gracefully
                    if (pr.StopFlag == true)
                    {
                        pr.Processor.Cleanup();

                        break;
                    }

                    if (pr.Processor.Process() == false)
                    {
                        //check for a stop flag to exit when no work to do
                        if (pr.StopWhenCompleteFlag == true)
                        {
                            pr.Processor.Cleanup();

                            break;
                        }

                        if (pr.Processor.IsEventDriven)
                        {
                            pr.CompletedEvent.WaitOne();
                        }
                        else
                        {
                            Thread.Sleep(pr.CurrentSleepTimeMs);
                        }
                    }

                    pr.LastThreadTest = DateTime.Now;
                }
                catch (ThreadInterruptedException ex)
                {
                    //occurs when the thread is awoken 
                    Trace.TraceError(ex.Message); 
                    break;
                }
                catch (ThreadAbortException ex)
                {
                    //occurs when a timeout occurs
                    Trace.TraceError(ex.Message); 
                    break;
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message); 
                    Thread.Sleep(2000);
                }
            }
        }
    }
}
