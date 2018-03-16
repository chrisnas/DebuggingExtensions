using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClrMDStudio
{
    public class StringsWindow : AnalysisWindow
    {
        public StringsWindow(string title, DebuggingSession session, TaskScheduler taskScheduler)
            : base(title, session, taskScheduler)
        {
            _analyzer = new DuplicatedStringAnalyzer(this);
            ((DuplicatedStringAnalyzer)_analyzer).MinCountThreshold = 100;

            Icon = BitmapFrame.Create(new Uri(@"pack://application:,,,/Panes/strings.png", UriKind.RelativeOrAbsolute));
        }
    }
}
