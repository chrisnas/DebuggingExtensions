using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ClrMDStudio
{
    public partial class GCMemoryWindow : Window
    {
    #region implementation details
    #endregion
        protected IAnalyzer _analyzer;
        private DebuggingSession _session;
        private TaskScheduler _scheduler;
        private bool _canClose = false;

        private IReadOnlyList<SegmentInfo> _segments;

        public GCMemoryWindow(string title, DebuggingSession session, TaskScheduler taskScheduler)
        {
            InitializeComponent();

            Icon = BitmapFrame.Create(new Uri(@"pack://application:,,,/Panes/GCMemory.png", UriKind.RelativeOrAbsolute));

            Title = title;
            _scheduler = taskScheduler;
            _session = session;

            // start the progress bar: it will be stopped at the end of the analysis
            StartProgress();

            RunAsync((Action)(() => Initialize()));
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

        private void Initialize()
        {
            var helper = new ClrMDHelper(_session.Clr);
            var segments = helper.ComputeGCSegments(needPinned:true);
            SetResultAsync(segments);
        }

        private void SetResultAsync(IReadOnlyList<SegmentInfo> segments)
        {
            App.Current.Dispatcher.BeginInvoke((Action)(
                () => { UpdateUI(segments); }),
                DispatcherPriority.Background,
                null
                );
        }

        private void UpdateUI(IReadOnlyList<SegmentInfo> segments)
        {
            ShowSegments(segments);
            StopProgress();
        }

        private void SetSegmentModelResult(SegmentModel segment)
        {
            var sb = new StringBuilder(30 * 1024);
            sb.AppendLine($"   {GetGenerationType(segment.Generation)} | {segment.Start.ToString("X")} - {segment.End.ToString("X")} ({(segment.End - segment.Start).ToString("N0").PadLeft(14)})");

            var pinnedObjects = segment.PinnedObjects;
            for (int currentPinnedObject = 0; currentPinnedObject < pinnedObjects.Count; currentPinnedObject++)
            {
                var pinnedObject = pinnedObjects[currentPinnedObject];
                sb.AppendFormat("          {0,11} | {1:x} {2}\r\n",
                    pinnedObject.handle.HandleType,
                    pinnedObject.handle.Object,
                    pinnedObject.typeDescription
                    );
            }

            var freeBlocks = segment.FreeBlocks;
            for (int currentFreeBlock = 0; currentFreeBlock < freeBlocks.Count; currentFreeBlock++)
            {
                var freeBlock = freeBlocks[currentFreeBlock];
                sb.AppendFormat($"                      ~ {freeBlock.Address.ToString("x")}  {freeBlock.Size.ToString("N0").PadLeft(14)}\r\n");
            }

            tbResults.Text = sb.ToString();
        }

        private void ShowSegments(IReadOnlyList<SegmentInfo> segments)
        {
            // gen0, 1, 2 and LOH
            const int MaxGeneration = 4;

            _segments = segments;
            var sizeByGeneration = new ulong[MaxGeneration];
            var segmentsGroupedByGeneration = new List<GenerationInSegment>[MaxGeneration];
            for (int currentGeneration = 0; currentGeneration < segmentsGroupedByGeneration.Length; currentGeneration++)
            {
                segmentsGroupedByGeneration[currentGeneration] = new List<GenerationInSegment>();
            }

            ulong maxLength = 0;
            for (int currentSegment = 0; currentSegment < segments.Count; currentSegment++)
            {
                var segment = segments[currentSegment];
                foreach (var generation in segment.Generations.OrderBy(g => g.Start))
                {
                    if (maxLength < generation.Length)
                        maxLength = generation.Length;
                    segmentsGroupedByGeneration[generation.Generation].Add(generation);

                    sizeByGeneration[generation.Generation] += generation.Length;
                }
            }

            var itemsControls = new ItemsControl[MaxGeneration];
            itemsControls[3] = icLoh;
            itemsControls[2] = icGen2;
            itemsControls[1] = icGen1;
            itemsControls[0] = icGen0;

            for (int currentGeneration = 0; currentGeneration < segmentsGroupedByGeneration.Length; currentGeneration++)
            {
                var segmentsModel = new List<SegmentModel>();
                foreach (var segment in segmentsGroupedByGeneration[currentGeneration])
                {
                    var model = new SegmentModel()
                    {
                        Generation = segment.Generation,
                        PinnedObjects = segment.PinnedObjects,
                        FreeBlocks = segment.FreeBlocks,
                        Start = segment.Start,
                        End = segment.End,
                        ControlWidth = new GridLength(100D * segment.Length / maxLength, GridUnitType.Star),
                        EmptyColumnWidth = new GridLength(100D - (100D * segment.Length / maxLength), GridUnitType.Star)
                    };

                    segmentsModel.Add(model);
                }
                itemsControls[currentGeneration].ItemsSource = segmentsModel;
            }

            xpGen0.Header = $"Gen 0  ({sizeByGeneration[0].ToString("N0")})";
            xpGen1.Header = $"Gen 1  ({sizeByGeneration[1].ToString("N0")})";
            xpGen2.Header = $"Gen 2  ({sizeByGeneration[2].ToString("N0")})";
            xpLoh.Header = $"LOH    ({sizeByGeneration[3].ToString("N0")})";
        }

        private string GetGenerationType(GenerationInSegment generation)
        {
            return GetGenerationType(generation.Generation);
        }

        private string GetGenerationType(int generation)
        {
            return (generation == 3) ? " LOH" : $"gen{generation}";
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

        private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var model = element.DataContext as SegmentModel;

            SetSegmentModelResult(model);
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
    }
}
