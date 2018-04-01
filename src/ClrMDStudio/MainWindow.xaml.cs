using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows;
using System.Windows.Controls;

namespace ClrMDStudio
{
    public partial class MainWindow : Window
    {
    #region implementation details
    #endregion
        // wrap the accesses to ClrMD and provide higher level helpers
        private DebuggingSession _session;

        // leverage this STA task scheduler to run the analysis in another thread without any COM related issue
        // --> all calls to the DebuggingSession must be done in the same thread as the one used to create it
        private TaskScheduler _staTaskScheduler = new StaTaskScheduler(1);

        private AnalysisWindow _threadPoolWnd;
        private AnalysisWindow _threadsWnd;
        private AnalysisWindow _stringsWnd;
        private AnalysisWindow _timersWnd;
        private AnalysisWindow _pinnedObjectsWnd;
        private GCMemoryWindow _gcMemoryWnd;


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
            App.Current.Dispatcher.BeginInvoke((Action) (() =>
            {
                AddLine(line);
            }));
        }
        public void Write(string text)
        {
            App.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                AddString(text);
            }));
        }


    #region initialization
    #endregion
        public MainWindow()
        {
            InitializeComponent();
        }


    #region internal helpers
    #endregion
        private string SelectDumpFile()
        {
            // select the dump file to open
            OpenFileDialog ofd = new OpenFileDialog()
            {
                DefaultExt = ".dmp",
                Filter = "Dump files (.dmp)|*.dmp",
            };

            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                if (string.IsNullOrEmpty(ofd.FileName))
                    return null;
            }
            else
            {
                return null;
            }

            return ofd.FileName;
        }
        private void OpenDumpFile(string filename, string localSymbolCache)
        {
            if (_session == null)
            {
                _session = new DebuggingSession(localSymbolCache);
            }

            CloseAnalysisPanes();

            string errorMessage = null;
            try
            {
                if (!_session.Open(filename))
                {
                    MessageBox.Show(this, "Impossible to load SOS");
                    _session = null;
                }
            }
            catch (FileNotFoundException x)
            {
                errorMessage = "Impossible to load " + x.Message + "\r\nCopy it (with sos.dll) from the machine where the dump was taken\r\ninto the same folder as the .dmp file";
                _session = null;
            }
            catch (Exception x)
            {
                errorMessage = "Impossible to load dump file: " + x.Message;
                _session = null;
            }

            if (errorMessage != null)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show(this, errorMessage)
                );
            }
        }
        private void CloseAnalysisPanes()
        {
            if (_threadPoolWnd != null)
            {
                _threadPoolWnd.ForceClose();
                _threadPoolWnd = null;
            }

            if (_threadsWnd != null)
            {
                _threadsWnd.ForceClose();
                _threadsWnd = null;
            }

            if (_stringsWnd != null)
            {
                _stringsWnd.ForceClose();
                _stringsWnd = null;
            }

            if (_timersWnd != null)
            {
                _timersWnd.ForceClose();
                _timersWnd = null;
            }

            if (_pinnedObjectsWnd != null)
            {
                _pinnedObjectsWnd.ForceClose();
                _pinnedObjectsWnd = null;
            }

            if (_gcMemoryWnd != null)
            {
                _gcMemoryWnd.ForceClose();
                _gcMemoryWnd = null;
            }
        }

        async private Task RunAsync<T>(Action<T> action, T parameter)
        {
            await Task.Factory.StartNew(_ => action(parameter), null, CancellationToken.None, TaskCreationOptions.DenyChildAttach, _staTaskScheduler);
        }
        async private Task RunAsync(Action action)
        {
            await Task.Factory.StartNew(_ => action(), null, CancellationToken.None, TaskCreationOptions.DenyChildAttach, _staTaskScheduler);
        }

        private void AddLine(string line)
        {
            Debug.WriteLine(line);
        }
        private void AddString(string text)
        {
            Debug.Write(text);
        }


        #region event handlers
        #endregion
        private void OnDumpFilenameDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            tbDumpFilename.Clear();
            OnOpenDumpFile(sender, null);
        }
        private void OnShowContextMenu(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = btnAnalyze.ContextMenu;
            if (cm == null)
                return;

            cm.PlacementTarget = sender as Button;
            cm.IsOpen = true;

        }
        async private void OnOpenDumpFile(object sender, RoutedEventArgs e)
        {
            string filename = tbDumpFilename.Text;

            if (string.IsNullOrEmpty(filename))
            {
                filename = SelectDumpFile();
                if (!string.IsNullOrEmpty(filename))
                {
                    tbDumpFilename.Text = filename;
                }
                else
                {
                    tbDumpFilename.Focus();
                    return;
                }
            }

            if (!File.Exists(filename))
            {
                tbDumpFilename.Focus();
                return;
            }

            string file = tbDumpFilename.Text;
            string localSymbolCache = tbSymbolPath.Text;

            // it is mandatory to open the DebuggingSession in the same thread as the one that is used to create it or simply an STA thread?
            await RunAsync(() => OpenDumpFile(file, localSymbolCache));
        }

        async private void OnAnalyzeThreadPool(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                tbDumpFilename.Focus();
                return;
            }

            try
            {
                miThreadPool.IsEnabled = false;
                if (_threadPoolWnd == null)
                {
                    _threadPoolWnd = new ThreadPoolWindow(
                        "ClrMD Studio - Thread Pool Analysis",
                        _session,
                        _staTaskScheduler
                        );
                    _threadPoolWnd.ShowActivated = true;
                    _threadPoolWnd.ShowInTaskbar = true;
                    _threadPoolWnd.Owner = this;
                    _threadPoolWnd.Show();
                    await _threadPoolWnd.StartAnalysisAsync(null);
                }
                else
                {
                    _threadPoolWnd.WindowState = WindowState.Normal;
                    _threadPoolWnd.Activate();
                }

            }
            catch (Exception x)
            {
                AddLine(x.Message);
            }
            finally
            {
                miThreadPool.IsEnabled = true;
            }
        }

        async private void OnAnalyzeThread(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                tbDumpFilename.Focus();
                return;
            }

            try
            {
                miThreads.IsEnabled = false;

                if (_threadsWnd == null)
                {
                    _threadsWnd = new ThreadsWindow(
                        "ClrMD Studio - Threads Analysis",
                        _session,
                        _staTaskScheduler
                        );
                    _threadsWnd.ShowActivated = true;
                    _threadsWnd.ShowInTaskbar = true;
                    _threadsWnd.Owner = this;
                    _threadsWnd.Show();
                    await _threadsWnd.StartAnalysisAsync(null);
                }
                else
                {
                    _threadsWnd.WindowState = WindowState.Normal;
                    _threadsWnd.Activate();
                }
            }
            catch (Exception x)
            {
                AddLine(x.Message);
            }
            finally
            {
                miThreads.IsEnabled = true;
            }
        }

        async private void OnAnalyzeStrings(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                tbDumpFilename.Focus();
                return;
            }

            try
            {
                miStrings.IsEnabled = false;
                if (_stringsWnd == null)
                {
                    _stringsWnd = new StringsWindow(
                        "ClrMD Studio - Strings Analysis",
                        _session,
                        _staTaskScheduler
                        );
                    _stringsWnd.ShowActivated = true;
                    _stringsWnd.ShowInTaskbar = true;
                    _stringsWnd.Owner = this;
                    _stringsWnd.Show();
                    await _stringsWnd.StartAnalysisAsync(null);
                }
                else
                {
                    _stringsWnd.WindowState = WindowState.Normal;
                    _stringsWnd.Activate();
                }
            }
            catch (Exception x)
            {
                AddLine(x.Message);
            }
            finally
            {
                miStrings.IsEnabled = true;
            }

        }

        async private void OnAnalyzeTimers(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                tbDumpFilename.Focus();
                return;
            }

            try
            {
                miTimers.IsEnabled = false;
                if (_timersWnd == null)
                {
                    _timersWnd = new TimersWindow(
                        "ClrMD Studio - Timers Analysis",
                        _session,
                        _staTaskScheduler
                        );
                    _timersWnd.ShowActivated = true;
                    _timersWnd.ShowInTaskbar = true;
                    _timersWnd.Owner = this;
                    _timersWnd.Show();
                    await _timersWnd.StartAnalysisAsync(null);
                }
                else
                {
                    _timersWnd.WindowState = WindowState.Normal;
                    _timersWnd.Activate();
                }
            }
            catch (Exception x)
            {
                AddLine(x.Message);
            }
            finally
            {
                miTimers.IsEnabled = true;
            }
        }

        async private void OnAnalyzePinnedObjects(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                tbDumpFilename.Focus();
                return;
            }

            try
            {
                miPinnedObjects.IsEnabled = false;
                if (_pinnedObjectsWnd == null)
                {
                    _pinnedObjectsWnd = new PinnedObjectsWindow(
                        "ClrMD Studio - Pinned Objects Analysis",
                        _session,
                        _staTaskScheduler
                        );
                    _pinnedObjectsWnd.ShowActivated = true;
                    _pinnedObjectsWnd.ShowInTaskbar = true;
                    _pinnedObjectsWnd.Owner = this;
                    _pinnedObjectsWnd.Show();
                    await _pinnedObjectsWnd.StartAnalysisAsync(null);
                }
                else
                {
                    _pinnedObjectsWnd.WindowState = WindowState.Normal;
                    _pinnedObjectsWnd.Activate();
                }
            }
            catch (Exception x)
            {
                AddLine(x.Message);
            }
            finally
            {
                miPinnedObjects.IsEnabled = true;
            }
        }

        private void OnShowGCMemory(object sender, RoutedEventArgs e)
        {
            if (_session == null)
            {
                tbDumpFilename.Focus();
                return;
            }

            try
            {
                miGCMemory.IsEnabled = false;
                if (_gcMemoryWnd == null)
                {
                    _gcMemoryWnd = new GCMemoryWindow(
                        "ClrMD Studio - Show GC Memory",
                        _session,
                        _staTaskScheduler
                        );
                    _gcMemoryWnd.ShowActivated = true;
                    _gcMemoryWnd.ShowInTaskbar = true;
                    _gcMemoryWnd.Owner = this;
                    _gcMemoryWnd.Show();
                }
                else
                {
                    _gcMemoryWnd.WindowState = WindowState.Normal;
                    _gcMemoryWnd.Activate();
                }
            }
            catch (Exception x)
            {
                AddLine(x.Message);
            }
            finally
            {
                miGCMemory.IsEnabled = true;
            }
        }
    }
}
