using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClrMDStudio.Analyzers;

namespace ClrMDStudio
{
    public class TimersWindow : AnalysisWindow
    {
        public TimersWindow(string title, DebuggingSession session, TaskScheduler taskScheduler)
            : base(title, session, taskScheduler)
        {
            _analyzer = new TimerAnalyzer(this);

            Icon = BitmapFrame.Create(new Uri(@"pack://application:,,,/Panes/Timers.png", UriKind.RelativeOrAbsolute));
        }
    }
}
