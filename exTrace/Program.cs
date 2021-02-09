using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;

namespace exTrace
{

    /// <summary>
    /// todo:https://coderoad.ru/63464266/%D0%9E%D1%82%D1%81%D1%83%D1%82%D1%81%D1%82%D0%B2%D1%83%D1%8E%D1%89%D0%B8%D0%B5-%D0%BA%D0%B0%D0%B4%D1%80%D1%8B-%D1%81%D1%82%D0%B5%D0%BA%D0%B0-%D0%B8%D0%B7-%D1%81%D0%BE%D0%B1%D1%8B%D1%82%D0%B8%D1%8F-ClrStackWalk
    /// todo:https://medium.com/criteo-engineering/build-your-own-net-memory-profiler-in-c-call-stacks-2-2-1-f67b440a8cc
    /// </summary>
    class Program
    {
        private static TraceEventSession _session;
        private static Task _handlerTask;
        private static bool _outputEnabled = true;
        private static ConcurrentQueue<EventWrap> _buffer = new ConcurrentQueue<EventWrap>();
        private static PrintOptions _printOptions = new PrintOptions();

        static void Main(string[] args)
        {
            _session = new TraceEventSession("exceptionTrace");
            _session.EnableProvider(ClrTraceEventParser.ProviderGuid);

            _session.Source.Clr.ExceptionStart += OnClrOnExceptionStart;

            _handlerTask = Task.Run(() => _session.Source.Process());

            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                switch (keyInfo.KeyChar)
                {
                    case '?': Console.WriteLine("q - quit, p - prcess, e - exception,  f - filter (F-clear, Alt-f - print), d - data, ' ' - pause (buffered)");
                        break;
                    case ' ': if (_outputEnabled) Disable(); else Enable();
                        break;
                    case 'q': goto exit;
                    case 'p':
                        _printOptions.ProcessInfo = _printOptions.EnumRotate(_printOptions.ProcessInfo);
                        break;
                    case 'e':
                        _printOptions.ExceptionInfo = _printOptions.EnumRotate(_printOptions.ExceptionInfo);
                        break;
                    case 'd':
                        _printOptions.DataPrint = !_printOptions.DataPrint;
                        break;
                    case 'F':
                        _printOptions.SimpleFilter.Clear();
                        break;
                    case 'f':
                        if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0)
                        {
                            Console.WriteLine(string.Join(Environment.NewLine, _printOptions.SimpleFilter));
                            break;
                        }
                        Disable();
                        Console.Write("Enter filter:");
                        var filter = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(filter)) break;
                        filter = filter.Trim();
                        _printOptions.SimpleFilter.Add(filter);
                        Enable();
                        break;

                }
            }

            exit:
            _session.Stop();
            _handlerTask.Wait();
            _session?.Dispose();
        }

        private static void OnClrOnExceptionStart(ExceptionTraceData data)
        {
            var @event = new EventWrap(data);
            Hydrate(@event);

            if (_outputEnabled)
                Print(@event);
            else 
                _buffer.Enqueue(@event);
        }

        private static void Hydrate(EventWrap data)
        {
            if (string.IsNullOrWhiteSpace(data.ProcessName))
            {
                using (var process = Process.GetProcessById(data.ProcessID))
                {
                    //data.ProcessName = process.ProcessName;
                    data.ProcessName = process.MainModule?.ModuleName;
                }
            }
        }

        private static void Print(EventWrap data)
        {
            var fColor = Console.ForegroundColor;
            var bColor = Console.BackgroundColor;

            PrintInt(data);

            SetConsoleColors(fColor, bColor);

        }

        private static void PrintInt(EventWrap data)
        {
            var dataAsStr = data.ToString();

            foreach (var filter in _printOptions.SimpleFilter)
            {
                if (!dataAsStr.Contains(filter)) continue;
                SetConsoleColors(ConsoleColor.Black, ConsoleColor.Yellow);
                Console.Write("f");
                return;
            }


            if (_printOptions.ProcessInfo != PrintOptions.ProcessDetails.None)
            {
                SetConsoleColors(ConsoleColor.Cyan);
                Console.Write('[');
                if (_printOptions.ProcessInfo == PrintOptions.ProcessDetails.Pid)
                    Console.Write(data.ProcessID);
                if (_printOptions.ProcessInfo == PrintOptions.ProcessDetails.ProcessName)
                    Console.Write(data.ProcessName);
                Console.Write("] ");
            }

            if (_printOptions.ExceptionInfo != 0)
            {
                SetConsoleColors(ConsoleColor.Red);
                var exceptionType = data.ExceptionType;
                if (_printOptions.ExceptionInfo == PrintOptions.ExceptionDetails.Full)
                    Console.Write(exceptionType);
                else
                {
                    var idx = exceptionType.LastIndexOf('.');
                    Console.Write(exceptionType.Substring(idx + 1));
                }
            }

            SetConsoleColors(ConsoleColor.Gray);
            Console.Write(" -> ");


            if (_printOptions.MessagePrint)
            {
                SetConsoleColors(ConsoleColor.Green);
                Console.Write(data.ExceptionMessage);
            }

            if (_printOptions.DataPrint)
            {
                Console.Write(dataAsStr);
            }

            Console.WriteLine();

        }

        private static void SetConsoleColors(ConsoleColor fg, ConsoleColor? bg = null)
        {
            Console.ForegroundColor = fg;
            if (bg != null)
                Console.BackgroundColor = bg.Value;
        }

        private static void Disable()
        {
            _outputEnabled = false;
        }

        private static void Enable()
        {
            while (_buffer.TryDequeue(out EventWrap data))
            {
                Print(data);
            }

            _outputEnabled = true;

            while (_buffer.TryDequeue(out EventWrap data))
            {
                Print(data);
            }
        }
    }
}
