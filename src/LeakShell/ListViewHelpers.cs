using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;

namespace LeakShell
{
    // reference from http://support.microsoft.com/kb/319401
    class TypeEntryListViewItemComparer : IComparer
    {
        public const int columnFilter = 0;
        public const int columnCount  = 1;
        public const int columnSize   = 2;
        public const int columnClass  = 3;

        public int Column { get; set; }
        public SortOrder Order { get; set; }


        public TypeEntryListViewItemComparer()
        {
            Column = 0;
            Order = SortOrder.None;
        }

        public int Compare(object x, object y)
        {
            int compareResult;
            TypeEntryListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (TypeEntryListViewItem)x;
            listviewY = (TypeEntryListViewItem)y;

            // check special "Filtered" case
            if (Column == columnFilter)
            {
                if (listviewX.Filtered == listviewY.Filtered)
                {
                    compareResult = 0;
                }
                else
                {
                    compareResult = listviewX.Filtered ? 1 : -1;
                }
            }
            else
            {
                // Compare the two items
                compareResult = InternalCompare((TypeEntry)listviewX.Tag, (TypeEntry)listviewY.Tag);
            }


            // Calculate correct return value based on object comparison
            if (Order == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (Order == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        private int InternalCompare(TypeEntry xEntry, TypeEntry yEntry)
        {
            switch (Column)
            {
                case columnCount:
                    if (xEntry.Count > yEntry.Count)
                        return 1;
                    if (xEntry.Count < yEntry.Count)
                        return -1;
                    return 0;

                case columnSize:
                    if (xEntry.TotalSize > yEntry.TotalSize)
                        return 1;
                    if (xEntry.TotalSize < yEntry.TotalSize)
                        return -1;
                    return 0;

                case columnClass:
                    return string.Compare(xEntry.Name, yEntry.Name);

                default:
                    Debug.Fail("Unknown column");
                    break;
            }
            return 0;
        }
    }

    class TypeEntryListViewItem : ListViewItem
    {
        public bool Filtered { get; set; }

        public TypeEntryListViewItem(string text)
            : base(text)
        {
            Filtered = false;
        }
    }


    enum SnapshotListViewItemState
    {
        None = 0,
        Reference,
        Current,
    }

    class SnapshotListViewItem : ListViewItem
    {
        public SnapshotListViewItemState State { get; set; }

        public SnapshotListViewItem()
            : base("")
        {
            State = SnapshotListViewItemState.None;
        }
    }


}
