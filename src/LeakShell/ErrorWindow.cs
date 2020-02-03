using System;
using System.Windows.Forms;

namespace LeakShell
{
    public partial class ErrorWindow : Form
    {
        private ErrorWindow()
        {
            InitializeComponent();
        }

        private string Title { get; set; }
        private string Message { get; set; }
        private string LinkText { get; set; }
        private string LinkAddress { get; set; }

        public static void ShowMessage(Form owner, string title, string message)
        {
            ShowMessage(owner, title, message, null, null);
        }
        public static void ShowMessage(Form owner, string title, string message, string linkText, string linkAddress)
        {
            ErrorWindow dlg = new ErrorWindow();
            dlg.Title = title;
            dlg.Message = message;
            dlg.LinkText = linkText;
            dlg.LinkAddress = linkAddress;

            dlg.ShowDialog(owner);
        }

        private void ErrorWindow_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Title))
            {
                Text = Title;
            }
            if (!string.IsNullOrEmpty(Message))
            {
                tbMessage.Text = Message;
            }
            if (!string.IsNullOrEmpty(LinkText))
            {
                llLink.Text = LinkText;
            }
        }

        private void llLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LinkAddress))
            {
                System.Diagnostics.Process.Start(LinkAddress);
            }
        }
    }
}
