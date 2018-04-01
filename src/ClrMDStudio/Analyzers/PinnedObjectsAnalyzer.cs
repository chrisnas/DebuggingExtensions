using System;
using System.Linq;

namespace ClrMDStudio.Analyzers
{
    public class PinnedObjectsAnalyzer : IAnalyzer
    {
        public PinnedObjectsAnalyzer(IClrMDHost host)
        {
            _host = host;
        }

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
                var clr = _host.Session.Clr;
                ClrMDHelper helper = new ClrMDHelper(_host.Session.Clr);

                var pinnedObjectsCount = 0;

                foreach (var generation in helper.ComputePinnedObjects())
                {
                    if (generation.Number != 3)
                    {
                        _host.WriteLine($"Gen{generation.Number}: {generation.HandleCount}");
                    }
                    else
                    {
                        _host.WriteLine($"LOH: {generation.HandleCount}");
                    }

                    foreach (var type in generation.Types)
                    {
                        var handles = generation.GetHandles(type).OrderBy(h => h.Object).ToList();
                        pinnedObjectsCount += handles.Count;

                        // show only types with more than 2 instances just to avoid swamping the output with global pinned handles (especially for WPF applications)
                        if (handles.Count < 3)
                            continue;

                        _host.WriteLine($"   {type} : {handles.Count}");
                        for (int i = 0; i < handles.Count; i++)
                        {
                            _host.WriteLine(string.Format("   - {0,11} | {1:x}", handles[i].HandleType, handles[i].Object));
                        }
                    }
                }
                _host.WriteLine("-------------------------------------------------------------------------");
                _host.WriteLine($"Total: {pinnedObjectsCount} pinned object");
            }
            catch (Exception x)
            {
                _host.WriteLine(x.ToString());
                success = false;
            }
            finally
            {
                _host.OnAnalysisDone(success);
            }
        }
    }
}
