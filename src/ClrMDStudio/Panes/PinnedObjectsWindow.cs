using ClrMDStudio.Analyzers;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ClrMDStudio
{
    public class PinnedObjectsWindow : AnalysisWindow
    {
        public PinnedObjectsWindow(string title, DebuggingSession session, TaskScheduler taskScheduler)
        : base(title, session, taskScheduler)
        {
            _analyzer = new PinnedObjectsAnalyzer(this);

            Icon = BitmapFrame.Create(new Uri(@"pack://application:,,,/Panes/PinkPin.png", UriKind.RelativeOrAbsolute));
        }
    }
}
