using System;
using System.Collections.Concurrent;

namespace exTrace
{
    public class PrintOptions
    {
        [Flags]
        public enum ProcessDetails
        {
            None,
            Pid = 1,
            ProcessName = 2,
        }

        public enum ExceptionDetails
        {
            Short = 1,
            Full = 2,
        }

        public ConcurrentBag<string> SimpleFilter = new ConcurrentBag<string>();
        public ProcessDetails ProcessInfo { get; set; } = ProcessDetails.ProcessName;
        public ExceptionDetails ExceptionInfo { get; set; } =  ExceptionDetails.Short;

        public bool MessagePrint { get; set; } = true;
        public bool DataPrint { get; set; } = false;

        public T EnumRotate<T>(T value)
            where T : struct, Enum, IConvertible
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var idx = Array.FindIndex<T>(values, w => w.Equals(value));
            idx++;
            if (idx >= values.Length)
                idx = 0;
            return values[idx];
        }
    }
}