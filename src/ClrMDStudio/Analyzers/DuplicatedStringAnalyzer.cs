using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace ClrMDStudio
{
    public class DuplicatedStringAnalyzer : IAnalyzer
    {
        public DuplicatedStringAnalyzer(IClrMDHost host)
        {
            _host = host;
        }

        public int MinCountThreshold { get; set; }


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
            try
            {
                ClrMDHelper helper = new ClrMDHelper(_host.Session.Clr);

                var strings = helper.ComputeDuplicatedStrings();
                if (strings == null)
                {
                    _host.WriteLine("Impossible to enumerate strings...");
                    return;
                }

                int totalSize = 0;

                // sort by size taken by the instances of string
                foreach (var element in strings.Where(s => s.Value > MinCountThreshold).OrderBy(s => 2 * s.Value * s.Key.Length))
                {
                    _host.WriteLine(string.Format(
                        "{0,8} {1,12} {2}",
                        element.Value.ToString(),
                        (2 * element.Value * element.Key.Length).ToString(),
                        element.Key.Replace("\n", "## ").Replace("\r", " ##")
                        ));
                    totalSize += 2 * element.Value * element.Key.Length;
                }

                _host.WriteLine("-------------------------------------------------------------------------");
                _host.WriteLine(string.Format("         {0,12} MB", (totalSize/(1024*1024)).ToString()));
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.ToString());
                success = false;
            }
            finally
            {
                _host.OnAnalysisDone(success);
            }
        }
    }
}
