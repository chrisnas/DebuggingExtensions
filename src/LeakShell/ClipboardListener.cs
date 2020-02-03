using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace LeakShell  {

// Take a look at http://msdn.microsoft.com/en-us/library/ms649016(v=VS.85).aspx for 
// a description of the Win32 way to access the Windows Clipboard
//
// The method used here to monitor the Clipboard is the "old fashion way" instead of taking the Vista and later versions of Windows path
// 
public partial class ClipboardListener : Component
{
#region implementation details
#endregion
    ClipboardMonitorControl _monitorControl;


#region creation/initialization
#endregion
    public ClipboardListener()
    {
        InitializeComponent();
        CreateClipboardMonitorControl();
    }
    public ClipboardListener(IContainer container)
    {
        container.Add(this);

        InitializeComponent();
        CreateClipboardMonitorControl();
    }
    private void CreateClipboardMonitorControl()
    {
        _monitorControl = new ClipboardMonitorControl(this);
        _monitorControl.CreateControl();
    }


#region public API
#endregion
    public event EventHandler<ClipboardChangedEventArgs> Changed;


#region internal helpers
#endregion
    private void OnClipboardChanged()
    {
        var listeners = Changed;
        if (listeners != null)
            listeners(this, new ClipboardChangedEventArgs());
    }


#region inner class
#endregion
    class ClipboardMonitorControl : Control
    {
    #region P/Invoke defintions
    #endregion
        // Note: goto http://pinvoke.net for native signatures
        const int WM_CHANGECBCHAIN   = 0x030D;
        const int WM_DRAWCLIPBOARD   = 0x0308;


        // legacy clipboard chain
        // --------------------------------------------------------------
        [DllImport("user32.dll")] static extern 
        IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll")] static extern 
        bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll")] static extern 
        int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);


        // Vista+ clipboard chain   --  this is not used here 
        // --------------------------------------------------------------
        const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)] static extern 
         bool AddClipboardFormatListener(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RemoveClipboardFormatListener(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetClipboardSequenceNumber();


    #region implementation details
    #endregion
        // keep track of the next listener in the Clipboard chain
        // Note: will be IntPtr.Zero if there is no other clipboard listener
        private IntPtr _nextInChain;

        // object really attached to the Clipboard chain
        private ClipboardListener _listener;
        
        // When SetClipboardViewer() is called, a WM_DRAWCLIPBOARD is received
        // --> no need to notify at that time
        private bool _attachedToClipboard;


    #region initialization
    #endregion
        public ClipboardMonitorControl(ClipboardListener listener)
        {
            _listener = listener;
        }


    #region overrides
    #endregion
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // register the windows to the clipboard chain
            _nextInChain = SetClipboardViewer(Handle);
            _attachedToClipboard = true;
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            // unregister it from the clipboard chain
            ChangeClipboardChain(Handle, _nextInChain);

            base.OnHandleDestroyed(e);
        }
        protected override void WndProc(ref Message m)
        {
            switch(m.Msg)
            {
                // from http://msdn.microsoft.com/en-us/library/ms649016(v=VS.85).aspx#_win32_Processing_the_WM_CHANGECBCHAIN_Message
                case WM_CHANGECBCHAIN:
                    // If the next window is closing, repair the chain. 
                    if (m.WParam == _nextInChain)
                    {
                        _nextInChain = m.LParam;
                    }
                    // Otherwise, pass the message to the next link if exists 
                    else if (_nextInChain != IntPtr.Zero)
                    {
                        SendMessage(_nextInChain, m.Msg, m.WParam, m.LParam); 
                    }
                break;
                
                case WM_DRAWCLIPBOARD:
                    // notify the listener
                    if (_attachedToClipboard && (_listener != null))
                    {
                        Debug.WriteLine(string.Format("WM_DRAWCLIPBOARD({0}, {1})", m.WParam, m.LParam));
                        // When SetClipboardViewer() is called, a WM_DRAWCLIPBOARD is received
                        // --> no need to notify at that time
                        _listener.OnClipboardChanged();

                        // don't forget to notify the next in the Clipboard chain if any
                        if (_nextInChain != IntPtr.Zero)
                        {
                            SendMessage(_nextInChain, m.Msg, m.WParam, m.LParam);
                        }
                    }
                    else
                    {
                        Debug.WriteLine(string.Format("** WM_DRAWCLIPBOARD({0}, {1})", m.WParam, m.LParam));
                    }
                break;
                
                default:
                    base.WndProc(ref m);
                break;
            }
        }
    }
}


public class ClipboardChangedEventArgs : EventArgs
{
}

}
