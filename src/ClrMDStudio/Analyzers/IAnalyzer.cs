using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrMDStudio
{
    public interface IAnalyzer
    {
        IClrMDHost Host { get; set; }

        void Run(string args);
    }
}
