using Microsoft.Diagnostics.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ClrMDStudio
{
    public class SegmentModel
    {
        public GridLength ControlWidth { get; set; }

        public GridLength EmptyColumnWidth { get; set; }

        public List<ulong> PinnedAddresses
        {
            get
            {
                return PinnedObjects.Select(po => po.handle.Object).ToList();
            }
        }

        public IReadOnlyList<(ClrHandle handle, string typeDescription)> PinnedObjects{ get; set; }

        public ulong Start { get; set; }

        public ulong End { get; set; }

        public int Generation { get; set; }
    }
}
