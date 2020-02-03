using System;
using System.Windows.Forms;
using System.Reflection;

namespace LeakShell
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void About_Load(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            lblTitle.Text =
                string.Format("LeakShell v{0}.{1}{2}",
                    assembly.GetName().Version.Major,
                    assembly.GetName().Version.Minor,
                    (assembly.GetName().Version.Build == 0) ? string.Empty : "." + assembly.GetName().Version.Build.ToString()
                    );
            lblVersion.Text = "2011-2020 @ Christophe Nasarre";
        }

        private void About_DoubleClick(object sender, EventArgs e)
        {
            Close();
        }

        private void AboutBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
            }
        }
    }
}
