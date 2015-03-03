using System;
using System.Diagnostics;

namespace TestExecutor
{
    /// <summary>
    /// Wrapper class for the tracing objects. 
    /// This is required in order to trace in different app-domain.
    /// </summary>
    [Serializable]
    public class TracerWrapper : MarshalByRefObject
    {
        public void TraceInformation(string message, params object[] args)
        {
            Trace.TraceInformation(message, args);
        }

        public void TraceInformation(string message)
        {
            Trace.TraceInformation(message);
        }

        public void TraceError(string message)
        {
            Trace.TraceError(message);
        }

        public void TraceError(string message, params object[] args)
        {
            Trace.TraceError(message, args);
        }
    }
}
