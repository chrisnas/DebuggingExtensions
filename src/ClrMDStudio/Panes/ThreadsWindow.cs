using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClrMDStudio
{
    public class ThreadsWindow : AnalysisWindow
    {
        public ThreadsWindow(string title, DebuggingSession session, TaskScheduler taskScheduler)
            : base(title, session, taskScheduler)
        {
            _analyzer = new ThreadAnalyzer(this);

            Icon = BitmapFrame.Create(new Uri(@"pack://application:,,,/Panes/threads.png", UriKind.RelativeOrAbsolute));
        }
    }
}
