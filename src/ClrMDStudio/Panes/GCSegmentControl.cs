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
        private readonly Pen _freeOutlinePen;
        private readonly Brush _freeFillBrush;

        public GcSegmentControl()
        {
            _pinnedFillBrush = Brushes.Red;
            _pinnedOutlinePen = new Pen(Brushes.Red, 1.0);
            _freeFillBrush = Brushes.White;
            _freeOutlinePen = new Pen(Brushes.White, 1.0);
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

        public List<FreeBlock> FreeBlocks
        {
            get
            {
                return (List<FreeBlock>)this.GetValue(FreeBlocksProperty);
            }
            set
            {
                this.SetValue(FreeBlocksProperty, value);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRoundedRectangle(
                Background, new Pen(Background, 1.0),
                new Rect(0, 0, ActualWidth, ActualHeight), 1, 1
                );

            var segmentSize = SegmentEnd - SegmentStart;
            var sizePerPixel = segmentSize / ActualWidth;

            RenderPinnedObjects(dc, sizePerPixel);
            RenderFreeBlocks(dc, sizePerPixel);

            base.OnRender(dc);
        }

        private void RenderFreeBlocks(DrawingContext dc, double sizePerPixel)
        {
            if (FreeBlocks == null)
                return;

            var height = ActualHeight * 0.6;
            var y = ActualHeight * 0.2;

            var freeBlocks = new HashSet<double>();
            foreach (var freeBlock in FreeBlocks)
            {
                var relativeAddress = freeBlock.Address - SegmentStart;
                var posInPixel = relativeAddress / sizePerPixel;
                var widthInPixel = freeBlock.Size / sizePerPixel;
                widthInPixel = (widthInPixel == 0) ? 1 : widthInPixel;
                if (freeBlocks.Add(posInPixel))
                {
                    dc.DrawRectangle(
                        _freeFillBrush, _freeOutlinePen,
                        new Rect(new Point(posInPixel, y), new Size(widthInPixel, height))
                        );
                }
            }
        }

        private void RenderPinnedObjects(DrawingContext dc, double sizePerPixel)
        {
            if (PinnedObjects == null)
                return;
            var pinnedObjects = new HashSet<double>();
            foreach (var pinnedObject in PinnedObjects)
            {
                var relativeAddress = pinnedObject - SegmentStart;
                var posInPixel = relativeAddress / sizePerPixel;

                if (pinnedObjects.Add(posInPixel))
                {
                    dc.DrawRectangle(
                        _pinnedFillBrush, _pinnedOutlinePen,
                        new Rect(new Point(posInPixel, 0), new Size(1, ActualHeight))
                        );
                }
            }
       }

        public static readonly DependencyProperty FreeBlocksProperty = DependencyProperty.Register(
            "FreeBlocks", typeof(List<FreeBlock>), typeof(GcSegmentControl));

        public static readonly DependencyProperty PinnedObjectsProperty = DependencyProperty.Register(
            "PinnedObjects", typeof(List<ulong>), typeof(GcSegmentControl));

        public static readonly DependencyProperty SegmentStartProperty = DependencyProperty.Register(
            "SegmentStart", typeof(ulong), typeof(GcSegmentControl));

        public static readonly DependencyProperty SegmentEndProperty = DependencyProperty.Register(
            "SegmentEnd", typeof(ulong), typeof(GcSegmentControl));
    }
}
