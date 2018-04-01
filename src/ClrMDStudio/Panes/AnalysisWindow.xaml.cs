using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ClrMDStudio
{
    public abstract partial class AnalysisWindow : Window, IClrMDHost
    {
    #region implementation details
    #endregion
        protected IAnalyzer _analyzer;
        private DebuggingSession _session;
        private TaskScheduler _scheduler;
        private bool _canClose = false;

        public AnalysisWindow(string title, DebuggingSession session, TaskScheduler taskScheduler)
        {
            InitializeComponent();

            Title = title;
            _scheduler = taskScheduler;
            _session = session;
        }

        public void ForceClose()
        {
            _canClose = true;
            App.Current.Dispatcher.BeginInvoke((Action)(
                () => { Close(); }),
                DispatcherPriority.Background,
                null
                );
        }

        async public Task StartAnalysisAsync(string args)
        {
            if (_analyzer == null)
                throw new InvalidOperationException("Don't forget to set the analyzer in the derived analysis window class constructor.");

            // start the progress bar: it will be stopped at the end of the analysis
            StartProgress();

            AddLine("Starting analysis...\r\n");
            await RunAsync((Action)(() => _analyzer.Run(args)));
        }

    #region IClrMDHost implementation
    #endregion
        public DebuggingSession Session
        {
            get
            {
                return _session;
            }

            set
            {
                throw new NotImplementedException();
            }
        }
        public void WriteLine(string line)
        {
            App.Current.Dispatcher.BeginInvoke((Action)(
                () => { AddLine(line); }),
                DispatcherPriority.Background,
                null
                );
        }
        public void Write(string text)
        {
            App.Current.Dispatcher.BeginInvoke((Action)(
                () => { AddString(text); }),
                DispatcherPriority.Background,
                null
                );
        }
        public void OnAnalysisDone(bool success)
        {
            App.Current.Dispatcher.BeginInvoke((Action)(
                () => { StopProgress(); }),
                DispatcherPriority.Background,
                null
                );
        }

    #region internal helpers
    #endregion
        private void StartProgress()
        {
            progressBar.Height = 24;
            progressBar.Visibility = Visibility.Visible;
        }
        private void StopProgress()
        {
            progressBar.Height = 0;
            progressBar.Visibility = Visibility.Hidden;
        }
        private void AddLine(string line)
        {
            tbResults.Text = tbResults.Text + line + "\r\n";
            tbResults.ScrollToEnd();
        }
        private void AddString(string text)
        {
            tbResults.Text = tbResults.Text + text;
            tbResults.ScrollToEnd();
        }
        async private Task RunAsync<T>(Action<T> action, T parameter)
        {
            await Task.Factory.StartNew(_ => action(parameter), null, CancellationToken.None, TaskCreationOptions.DenyChildAttach, _scheduler);
        }
        async private Task RunAsync(Action action)
        {
            await Task.Factory.StartNew(_ => action(), null, CancellationToken.None, TaskCreationOptions.DenyChildAttach, _scheduler);
        }

    #region event handlers
    #endregion
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_canClose)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
        }
    }
}
