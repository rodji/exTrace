using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace exTrace
{
    public class EventWrap
    {
        public EventWrap(ExceptionTraceData etd)
        {
            DataAsString = etd.ToString();
            ExceptionMessage = etd.ExceptionMessage;
            ProcessID = etd.ProcessID;
            ExceptionType = etd.ExceptionType;
            ProcessName = etd.ProcessName;
        }

        public string ProcessName { get; set; }

        public string ExceptionType { get; set; }

        public int ProcessID { get; set; }

        public string ExceptionMessage { get; set; }

        public override string ToString()
        {
            return DataAsString;
        }

        public string DataAsString { get; set; }
    }
}