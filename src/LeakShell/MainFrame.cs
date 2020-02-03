using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;


namespace LeakShell  {

// TODO:
// - support a list of namespace exclusion from the configuration file instead of hardcoded System./MS./Microsoft. prefixes
//

public partial class MainFrame : Form
{
    private HeapSnapshot _reference;
    private HeapSnapshot _current;
    private ClipboardListener _listener;
    private List<TypeEntry> _filteredEntries;
    private TypeEntryListViewItemComparer _compareSorter;
    private TypeEntryListViewItemComparer _filteredSorter;

    // Note: WinDBG copies the same content TWICE each time CTRL+C | Menu/Edit/Copy is used.
    //       So, a notification that happens too close to the previous one is skipped
    private DateTime _lastClipboardNotificationTime;
    private readonly TimeSpan MIN_ELAPSE_SINCE_CLIPBOARD_NOTIFICATION = new TimeSpan(0, 0, 0, 0, 600); // in milliseconds

    private readonly int ReferenceImageIndex = 3;
    private readonly int CurrentImageIndex = 4;


    public MainFrame()
    {
        InitializeComponent();
        _lastClipboardNotificationTime = DateTime.UtcNow;

        // Note: the first column cannot be right aligned
    }

    private void Clear()
    {
        // empty UI
        lvSnapshots.Items.Clear();
        lvCompare.Items.Clear();
        lvFiltered.Items.Clear();
        tbResult.Text = "";
        chartCount.Series["Series2"].Points.Clear();
        chartSize.Series["Series1"].Points.Clear();

        // same for data
        _reference = null;
        _current = null;
        _filteredEntries.Clear();
    }
    private void MainFrame_Load(object sender, EventArgs e)
    {
        // setup clipboard listener
        _listener = new ClipboardListener();
        _listener.Changed += new EventHandler<ClipboardChangedEventArgs>(_listener_Changed);

        // setup listview sorting
        _compareSorter = new TypeEntryListViewItemComparer();
        _compareSorter.Order = SortOrder.Descending;
        _compareSorter.Column = TypeEntryListViewItemComparer.columnSize;
        lvCompare.ListViewItemSorter = _compareSorter;

        _filteredSorter = new TypeEntryListViewItemComparer();
        _filteredSorter.Order = SortOrder.Descending;
        _filteredSorter.Column = TypeEntryListViewItemComparer.columnSize;
        lvFiltered.ListViewItemSorter = _filteredSorter;

        _filteredEntries = new List<TypeEntry>();
    
        // automatically get the version from the metadata 
        Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (appVersion.Build == 0)
        {
            Text = $"LeakShell - v{appVersion.Major}.{appVersion.Minor}  ({GetAppBitness()})";
        }
        else
        {
            Text = $"LeakShell - v{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}  ({GetAppBitness()})";
        }
    }


    private void _listener_Changed(object sender, ClipboardChangedEventArgs e)
    {
        // Note: in WinDBG, the same content is copied to the clipboard TWICE.
        //       so we need to skip the notifications that occurs too close from
        //       the previous one.
        var now = DateTime.UtcNow;
        if (now.Subtract(_lastClipboardNotificationTime) < MIN_ELAPSE_SINCE_CLIPBOARD_NOTIFICATION)
        {
            return;
        }
        _lastClipboardNotificationTime = now;

        // TODO: make this "maybe long" treatment asynchronous if needed
        //
        // Check if this is a supported format
        HeapSnapshot snapshot = GetSnapshotFromClipboard();
        if (snapshot == null)
        {
            return;
        }

        AddOneSnapshot(snapshot);
    }
    private void MainFrame_DoubleClick(object sender, EventArgs e)
    {
        ShowAboutBox();
    }
    private void llBlog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        System.Diagnostics.Process.Start("http://codenasarre.wordpress.com/2011/05/18/leakshell-or-how-to-automatically-find-managed-leaks/");
    }
    private void ShowAboutBox()
    {
        AboutBox about = new AboutBox();
        about.ShowDialog();
    }

    private void btnSetReference_Click(object sender, EventArgs e)
    {
        if (lvSnapshots.SelectedItems.Count == 0)
        {
            MessageBox.Show("You first have to select a snapshot", "Changing Reference snapshot", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // sanity checks
        SnapshotListViewItem item = (SnapshotListViewItem)lvSnapshots.SelectedItems[0];
        if (item.State == SnapshotListViewItemState.None)
        {
            SetReferenceSnapshot((HeapSnapshot)lvSnapshots.SelectedItems[0].Tag);
            CompareSnapshots();
        }
    }
    private void btnClearSnapshots_Click(object sender, EventArgs e)
    {
        Clear();
    }
    private void lvSnapshots_DoubleClick(object sender, EventArgs e)
    {
        if (lvSnapshots.SelectedItems.Count > 0)
        {
            SnapshotListViewItem item = (SnapshotListViewItem)lvSnapshots.SelectedItems[0];

            if (item.State == SnapshotListViewItemState.None)
            {
                SetCurrentSnapshot((HeapSnapshot)item.Tag);
                CompareSnapshots();
            }
        }
    }
    private void lvSnapshots_KeyUp(object sender, KeyEventArgs e)
    {
        // only handle the DEL key
        if (e.KeyCode != Keys.Delete)
            return;

        if (lvSnapshots.SelectedItems.Count > 0)
        {
            SnapshotListViewItem item = (SnapshotListViewItem)lvSnapshots.SelectedItems[0];
            if (item.State == SnapshotListViewItemState.None)
            {
                int index = lvSnapshots.Items.IndexOf(item);
                lvSnapshots.Items.Remove(item);

                // update the chart
                chartCount.Series["Series2"].Points.RemoveAt(index);
                chartSize.Series["Series1"].Points.RemoveAt(index);
            }
        }
    }
    private void cbDontShowBCLTypes_CheckedChanged(object sender, EventArgs e)
    {
        // v1.3.1: automatically trigger a comparison when the check box state changes
        if (_reference == null)
        {
            MessageBox.Show("You have to set the reference first", "Clipboard Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (_current == null)
        {
            MessageBox.Show("You have to set the current first", "Clipboard Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        CompareSnapshots();
    }

    private void lvCompare_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        if (e.Column == _compareSorter.Column)
        {
            // Reverse the current sort direction for this column.
            if (_compareSorter.Order == SortOrder.Ascending)
            {
                _compareSorter.Order = SortOrder.Descending;
            }
            else
            {
                _compareSorter.Order = SortOrder.Ascending;
            }
        }
        else
        {
            // Set the column number that is to be sorted; but keep the same ordering.
            _compareSorter.Column = e.Column;
        }

        lvCompare.Sort();
    }
    private void lvCompare_DoubleClick(object sender, EventArgs e)
    {
        if (lvCompare.SelectedItems.Count > 0)
        {
            TypeEntryListViewItem item = (TypeEntryListViewItem)lvCompare.SelectedItems[0];
            SwapFilteredState(item);
        }
    }
    private void lvFiltered_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        if (e.Column == _filteredSorter.Column)
        {
            // Reverse the current sort direction for this column.
            if (_filteredSorter.Order == SortOrder.Ascending)
            {
                _filteredSorter.Order = SortOrder.Descending;
            }
            else
            {
                _filteredSorter.Order = SortOrder.Ascending;
            }
        }
        else
        {
            // Set the column number that is to be sorted; but keep the same ordering.
            _filteredSorter.Column = e.Column;
        }

        lvFiltered.Sort();

    }
    private void lvFiltered_DoubleClick(object sender, EventArgs e)
    {
        if (lvFiltered.SelectedItems.Count > 0)
        {
            TypeEntryListViewItem item = (TypeEntryListViewItem)lvFiltered.SelectedItems[0];
            TypeEntry entry = (TypeEntry)item.Tag;
            SwapFilteredState(GetItemFromEntry(lvCompare, entry));
        }
    }

    private void lvSnapshots_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] filenames = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (filenames == null)
            {
                MessageBox.Show(this, "Only .dmp files are supported");
                return;
            }

            // check file types
            if (!FilesAreSupported(filenames))
            {
                MessageBox.Show(this, "Only .dmp files are supported");
                return;
            }

            AddDumpFiles(filenames);
        }
    }
    private void lvSnapshots_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : DragDropEffects.None;
    }
    private void lvSnapshots_DragOver(object sender, DragEventArgs e)
    {
        e.Effect = (e.Data.GetDataPresent(DataFormats.FileDrop)) ? DragDropEffects.Copy : DragDropEffects.None;
    }


    private void SwapFilteredState(TypeEntryListViewItem item)
    {
        item.Filtered = !item.Filtered;

        if (item.Filtered)
        {
            item.ImageIndex = 0;
            AddFilteredEntry((TypeEntry)item.Tag);
        }
        else
        {
            item.ImageIndex = -1;
            // Note: the item must be redrawn when the image is reset to -1
            lvCompare.RedrawItems(item.Index, item.Index, "force redraw" == null);

            RemoveFilteredEntry((TypeEntry)item.Tag);
        }
    }
    private void AddFilteredEntry(TypeEntry entry)
    {
        _filteredEntries.Add(entry);

        var item = new TypeEntryListViewItem(entry.Count.ToString("#,#"));
        item.Tag = entry;
        item.Filtered = true;
        item.SubItems.Add(entry.TotalSize.ToString("#,#"));
        item.SubItems.Add(entry.Name);
        lvFiltered.Items.Add(item);
    }
    private void RemoveFilteredEntry(TypeEntry entry)
    {
        _filteredEntries.Remove(entry);

        ListViewItem item = GetItemFromEntry(lvFiltered, entry);
        if (item != null)
        {
            lvFiltered.Items.Remove(item);
        }
    }
    private TypeEntryListViewItem GetItemFromEntry(ListView listview, TypeEntry entry)
    {
        foreach (TypeEntryListViewItem item in listview.Items)
        {
            TypeEntry tagEntry = (TypeEntry)item.Tag;
            if (string.Compare(HeapSnapshot.GetKey(tagEntry), HeapSnapshot.GetKey(entry)) == 0)
                return item;
        }
        return null;
    }
    private bool IsFilteredType(TypeEntry typeEntry)
    {
        string key = HeapSnapshot.GetKey(typeEntry);
        return
            _filteredEntries.Any(e => (string.Compare(HeapSnapshot.GetKey(e), key) == 0));
    }
    
    private string GetClipboardString()
    {
        if (Clipboard.ContainsText())
        {
            return Clipboard.GetText();
        }

        return null;
    }
    private HeapSnapshot GetSnapshotFromClipboard()
    {
        string clipboardString = GetClipboardString();
        if (string.IsNullOrEmpty(clipboardString))
        {
            return null;
        }

        return GetSnapshotFromDumpHeap_Stat(clipboardString);
    }
    private HeapSnapshot GetSnapshotFromDumpHeap_Stat(string heapdump)
    {
        // protect against ill-formatted "!dumpheap -stat" output
        HeapSnapshot snapshot;
        try
        {
            snapshot = HeapSnapshotFactory.CreateFromHeapStat(heapdump);
        }
        catch (InvalidOperationException x)
        {
            MessageBox.Show(this, x.Message, "Error while parsing '!dumpheap -stat' output");
            snapshot = null;
        }

        return snapshot;
    }

    private bool FilesAreSupported(string[] filenames)
    {
        foreach (string filename in filenames)
        {
            if (!FileIsSupported(filename))
                return false;
        }

        return true;
    }
    const string DUMP_EXTENSION = ".DMP";
    private bool FileIsSupported(string filename)
    {
        string extension = Path.GetExtension(filename);
        if (string.IsNullOrEmpty(extension))
            return false;
        return (DUMP_EXTENSION.CompareTo(extension.ToUpper()) == 0);
    }
    private void AddDumpFiles(string[] dumpFilenames)
    {
        foreach (string dumpFile in dumpFilenames)
        {
            AddDumpFile(dumpFile);
        }
    }

    private void AddDumpFile(string dumpFile)
    {
        // try to open the dump file
        try
        {
            var snapshot = HeapSnapshotFactory.CreateFromDumpFile(dumpFile);
            AddOneSnapshot(snapshot);
        }
        catch (InvalidOperationException x)
        {
            MessageBox.Show(this,
                GetExceptionMessages(x),
                $"Error while loading {dumpFile}",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation
            );
        }
    }
    private string GetExceptionMessages(Exception x)
    {
        // sanity checks
        if (x == null)
            return string.Empty;

        if (x.InnerException != null)
        {
            return string.Format($"{x.Message}\r\n\r\n{GetExceptionMessages(x.InnerException)}");
        }
        else
        {
            return x.Message;
        }
    }


    private void SetReferenceSnapshot(HeapSnapshot snapshot)
{
// sanity checks
if (snapshot == null)
{
return;
}

// remove current "reference" item
SnapshotListViewItem referenceItem = GetSnapshotItem(SnapshotListViewItemState.Reference);
if (referenceItem != null)
{
referenceItem.State = SnapshotListViewItemState.None;

referenceItem.ImageIndex = -1;
// Note: the item must be redrawn when the image is reset to -1
lvSnapshots.RedrawItems(referenceItem.Index, referenceItem.Index, "force redraw" == null);
}

// update the new "reference" item
SnapshotListViewItem item = GetSnapshotItem(snapshot);
item.State = SnapshotListViewItemState.Reference;
item.ImageIndex = ReferenceImageIndex;

_reference = snapshot;
}
    private void SetCurrentSnapshot(HeapSnapshot snapshot)
    {
        // sanity checks
        if (snapshot == null)
        {
            return;
        }

        // remove current "Current item"
        SnapshotListViewItem currentItem = GetSnapshotItem(SnapshotListViewItemState.Current);
        if (currentItem != null)
        {
            currentItem.State = SnapshotListViewItemState.None;

            currentItem.ImageIndex = -1;
            // Note: the item must be redrawn when the image is reset to -1
            lvSnapshots.RedrawItems(currentItem.Index, currentItem.Index, "force redraw" == null);
        }

        // update the new "current" item
        SnapshotListViewItem item = GetSnapshotItem(snapshot);
        item.State = SnapshotListViewItemState.Current;
        item.ImageIndex = CurrentImageIndex;

        _current = snapshot;
    }
    private SnapshotListViewItem GetSnapshotItem(HeapSnapshot snapshot)
    {
        foreach (SnapshotListViewItem item in lvSnapshots.Items)
        {
            if (item.Tag == snapshot)
                return item;
        }
        return null;
    }
    private SnapshotListViewItem GetSnapshotItem(SnapshotListViewItemState state)
    {
        foreach (SnapshotListViewItem item in lvSnapshots.Items)
        {
            if (item.State == state)
                return item;
        }
        return null;
    }
    private void AddSnapshot(HeapSnapshot snapshot)
    {
        // update the list
        SnapshotListViewItem lvSnapshot = new SnapshotListViewItem();
        lvSnapshot.Tag = snapshot;
        lvSnapshot.SubItems.Add(snapshot.ObjectCount.ToString("#,#"));
        lvSnapshot.SubItems.Add(snapshot.Size.ToString("#,#"));

        lvSnapshots.Items.Add(lvSnapshot);

        // update the chart
        chartCount.Series["Series2"].Points.AddY(snapshot.ObjectCount);
        chartSize.Series["Series1"].Points.AddY(snapshot.Size);
    }
    private void AddOneSnapshot(HeapSnapshot snapshot)
    {
        // add the snapshot into the list
        AddSnapshot(snapshot);

        // set the reference if not already set
        if (_reference == null)
        {
            SetReferenceSnapshot(snapshot);
        }
        else
        {
            SetCurrentSnapshot(snapshot);
            CompareSnapshots();
        }
    }

    private void CompareSnapshots()
    {
        HeapSnapshot compare = HeapSnapshotFactory.Compare(_reference, _current);
        HeapSnapshotFactory.DumpSnapshot(compare);

        // if the check box is checked, don't show the System.* types
        bool dontShowBCLType = cbDontShowBCLTypes.Checked;

        // update the Raw UI
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("HeapSnapshot at {0}\r\n", compare.TimeStamp);
        sb.AppendFormat("  {0} objects for {1} bytes\r\n", compare.ObjectCount, compare.Size);
        var entries =
            from item in compare.TypeStats.AsParallel()
            where !dontShowBCLType || !IsBclType(item.Value.Name)
            orderby item.Value.Name
            select item.Value
            ;
        foreach (var entry in entries)
        {
            sb.AppendFormat("    {0}\r\n", entry);
        }
        tbResult.Text = sb.ToString();

        // update the List UI
        lvCompare.BeginUpdate();
        lvFiltered.BeginUpdate();
        lvCompare.Items.Clear();
        lvFiltered.Items.Clear();

        foreach (var entry in compare.TypeStats)
        {
            TypeEntryListViewItem item;

            // Create a wrapping ListViewItem for the filtered list if needed
            // Note: always present even though DontShowBCLType is checked
            if (IsFilteredType(entry.Value))
            {
                AddFilteredEntry(entry.Value);
            }

            // skip BCL when needed
            if (dontShowBCLType)
            {
                if (IsBclType(entry.Value.Name))
                    continue;
            }

            // Create a wrapping ListViewItem for the compare list
            item = new TypeEntryListViewItem("");
            item.Tag = entry.Value;
            item.SubItems.Add(entry.Value.Count.ToString("#,#"));
            item.SubItems.Add(entry.Value.TotalSize.ToString("#,#"));
            item.SubItems.Add(entry.Value.Name);
            lvCompare.Items.Add(item);

            // Create a wrapping ListViewItem for the filtered list if needed
            if (IsFilteredType(entry.Value))
            {
                item.Filtered = true;
                item.ImageIndex = 0;
            }
        }
        lvCompare.Sort();
        lvFiltered.Sort();
        lvFiltered.EndUpdate();
        lvCompare.EndUpdate();
    }

    private bool IsBclType(string className)
    {
        // TODO: get the list of namespace prefixes from configuration
        return
        (
            className.StartsWith("System.") ||
            className.StartsWith("MS.") ||
            className.StartsWith("Microsoft.")
        )
        ; 
    }
    private string GetAppBitness()
    {
        if (IntPtr.Size == 4)
        {
            return "32 bit";
        }
        else if (IntPtr.Size == 8)
        {
            return "64 bit";
        }
        else
        {
            return $"{IntPtr.Size * 8} bit";
        }
    }
}



}
