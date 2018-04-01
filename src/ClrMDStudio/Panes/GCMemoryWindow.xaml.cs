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
            var segments = helper.ComputeGCSegments();
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
            SetTextResult(segments);
            ShowSegments(segments);
        }

        private void SetTextResult(IReadOnlyList<SegmentInfo> segments)
        {
            var sb = new StringBuilder(30*1024);
            for (int currentSegment = 0; currentSegment < segments.Count; currentSegment++)
            {
                var segment = segments[currentSegment];
                var generations = segment.Generations.OrderBy(g => g.Start).ToList();
                sb.AppendLine($"\r\n{segment.Number} - {generations.Count} generations");

                for (int currentGeneration = 0; currentGeneration < generations.Count; currentGeneration++)
                {
                    var generation = generations[currentGeneration];                                                                                                                                //      V---- up to 99 GB
                    sb.AppendLine($"   {GetGenerationType(generation)} | {generation.Start.ToString("X")} - {generation.End.ToString("X")} ({(generation.End - generation.Start).ToString("N0").PadLeft(14)})");

                    var pinnedObjects = generation.PinnedObjects;
                    for (int currentPinnedObject = 0; currentPinnedObject < pinnedObjects.Count; currentPinnedObject++)
                    {
                        var pinnedObject = pinnedObjects[currentPinnedObject];
                        sb.AppendFormat("          {0,11} | {1:x} {2}\r\n",
                            pinnedObject.HandleType,
                            pinnedObject.Object,
                            pinnedObject.Type
                            );
                    }
                }
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
                        PinnedAddresses = segment.PinnedObjects.Select(po => po.Object).ToList(),
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
            return (generation.Generation == 3) ? " LOH" : $"gen{generation.Generation}";
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
