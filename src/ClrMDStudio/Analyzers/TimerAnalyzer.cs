using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrMDStudio.Analyzers
{

    class TimerStat
    {
        public uint Period { get; set; }
        public String Line { get; set; }
        public int Count { get; set; }
    }

    public class TimerAnalyzer : IAnalyzer
    {
        public TimerAnalyzer(IClrMDHost host)
        {
            _host = host;
        }


    #region IAnalyzer implementation
    #endregion
        private IClrMDHost _host;
        public IClrMDHost Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public void Run(string args)
        {
            bool success = true;

            ClrMDHelper helper = new ClrMDHelper(_host.Session.Clr);

            try
            {
                Dictionary<string, TimerStat> stats = new Dictionary<string, TimerStat>(64);
                int totalCount = 0;
                foreach (var timer in helper.EnumerateTimers().OrderBy(t => t.Period))
                {
                    totalCount++;

                    string line = string.Intern(GetTimerString(timer));
                    string key = string.Intern(string.Format(
                        "@{0,8} ms every {1,8} ms | {2} ({3}) -> {4}",
                        timer.DueTime.ToString(),
                        (timer.Period == 4294967295) ? "  ------" : timer.Period.ToString(),
                        timer.StateAddress.ToString("X16"),
                        timer.StateTypeName,
                        timer.MethodName
                        ));

                    TimerStat stat;
                    if (!stats.ContainsKey(key))
                    {
                        stat = new TimerStat()
                        {
                            Count = 0,
                            Line = line,
                            Period = timer.Period
                        };
                        stats[key] = stat;
                    }
                    else
                    {
                        stat = stats[key];
                    }
                    stat.Count = stat.Count + 1;

                    _host.WriteLine(line);
                }

                // create a summary
                _host.WriteLine("\r\n " + totalCount.ToString() + " timers\r\n-----------------------------------------------");
                foreach (var stat in stats.OrderBy(kvp => kvp.Value.Count))
                {
                    _host.WriteLine(string.Format(
                        "{0,4} | {1}",
                        stat.Value.Count.ToString(),
                        stat.Value.Line
                    ));
                }
            }
            catch (Exception x)
            {
                _host.WriteLine(x.Message);
            }
            finally
            {
                _host.OnAnalysisDone(success);
            }
        }

        string GetTimerString(TimerInfo timer)
        {
            return string.Format(
                "0x{0} @{1,8} ms every {2,8} ms |  {3} ({4}) -> {5}",
                timer.TimerQueueTimerAddress.ToString("X16"),
                timer.DueTime.ToString(),
                (timer.Period == 4294967295) ? "  ------" : timer.Period.ToString(),
                timer.StateAddress.ToString("X16"),
                timer.StateTypeName,
                timer.MethodName
            );
        }
    }
}
