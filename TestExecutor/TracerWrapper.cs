using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestExecutor
{
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
