using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClrMDStudio
{
    class ThreadPoolWindow : AnalysisWindow
    {
        public ThreadPoolWindow(string title, DebuggingSession session, TaskScheduler taskScheduler)
            : base(title, session, taskScheduler)
        {
            _analyzer = new ThreadPoolAnalyzer(this);

            Icon = BitmapFrame.Create(new Uri(@"pack://application:,,,/Panes/deadpool.png", UriKind.RelativeOrAbsolute));
        }
    }
}
