using System.Collections.Generic;
using System.Windows;

namespace ClrMDStudio
{
    public class SegmentModel
    {
        public GridLength ControlWidth { get; set; }

        public GridLength EmptyColumnWidth { get; set; }

        public List<ulong> PinnedAddresses { get; set; }

        public ulong Start { get; set; }

        public ulong End { get; set; }
    }

}
