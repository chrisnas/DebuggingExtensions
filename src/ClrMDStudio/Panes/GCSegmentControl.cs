using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClrMDStudio
{
    public class GcSegmentControl : Control
    {
        private readonly Pen _pinnedOutlinePen;
        private readonly Brush _pinnedFillBrush;

        public GcSegmentControl()
        {
            _pinnedFillBrush = Brushes.Red;
            _pinnedOutlinePen = new Pen(Brushes.Red, 1.0);
        }

        public ulong SegmentStart
        {
            get
            {
                return (ulong)this.GetValue(SegmentStartProperty);
            }

            set
            {
                this.SetValue(SegmentStartProperty, value);
            }
        }
        public ulong SegmentEnd
        {
            get
            {
                return (ulong)this.GetValue(SegmentEndProperty);
            }

            set
            {
                this.SetValue(SegmentEndProperty, value);
            }
        }

        public List<ulong> PinnedObjects
        {
            get
            {
                return (List<ulong>)this.GetValue(PinnedObjectsProperty);
            }
            set
            {
                this.SetValue(PinnedObjectsProperty, value);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (PinnedObjects != null)
            {
                dc.DrawRoundedRectangle(Background, new Pen(Background, 1.0), new Rect(0, 0, ActualWidth, ActualHeight), 1, 1);

                var segmentSize = SegmentEnd - SegmentStart;

                var sizePerPixel = segmentSize / ActualWidth;

                var paintedObjects = new HashSet<double>();

                foreach (var pinnedObject in PinnedObjects)
                {
                    var relativeAddress = pinnedObject - SegmentStart;

                    var pixel = relativeAddress / sizePerPixel;

                    if (paintedObjects.Add(pixel))
                    {
                        dc.DrawRectangle(_pinnedFillBrush, _pinnedOutlinePen, new Rect(new Point(pixel, 0), new Size(1, ActualHeight)));
                    }
                }
            }

            base.OnRender(dc);
        }

        public static readonly DependencyProperty PinnedObjectsProperty = DependencyProperty.Register(
            "PinnedObjects", typeof(List<ulong>), typeof(GcSegmentControl));

        public static readonly DependencyProperty SegmentStartProperty = DependencyProperty.Register(
            "SegmentStart", typeof(ulong), typeof(GcSegmentControl));

        public static readonly DependencyProperty SegmentEndProperty = DependencyProperty.Register(
            "SegmentEnd", typeof(ulong), typeof(GcSegmentControl));
    }
}
