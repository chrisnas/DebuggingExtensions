using System;
using Microsoft.Diagnostics.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace ClrMDStudio.Analyzers
{
    public class GenerationInSegment
    {
        public int Generation { get; set; }
        public ulong Start { get; set; }
        public ulong End { get; set; }
        public ulong Length { get; set; }
    }

    public class SegmentInfo
    {
        private readonly ClrSegment _segment;
        private List<GenerationInSegment> _generations;

        public SegmentInfo(ClrSegment segment)
        {
            _segment = segment;
            Number = _segment.ProcessorAffinity;
            IsEphemeral = _segment.IsEphemeral;
            IsLoh = _segment.IsLarge;
            IsGen2 = !(IsEphemeral || IsLoh);

            ComputeGenerations(segment);
        }

        public int Number { get; }
        public bool IsEphemeral { get; set; }
        public bool IsLoh { get; set; }
        public bool IsGen2 { get; set; }

        public GenerationInSegment GetGeneration(int generation)
        {
            if (generation > 3)
                throw new ArgumentOutOfRangeException(nameof(generation), $"generation {generation} must be less or equal to 3");

            return _generations.FirstOrDefault(g => g.Generation == generation);
        }

        public int GetGenerationFromAddress(uint address)
        {
            // in a LOH segment, Gen2Start and Gen2Length define the possible memory values
            if (IsLoh)
                return ((address > _segment.Gen2Start) && (address < _segment.Gen2Start + _segment.Gen2Length)) ? 3 : -1;

            // in a segment, generation 2 starts at the lowest address and generation 0 at the highest, up to Gen0Start + Gen0Length
            if (address < _segment.Gen2Start)
                return -1;
            if (address < _segment.Gen1Start)
                return 2;
            if (address < _segment.Gen0Start)
                return 1;
            if (address < _segment.Gen0Start + _segment.Gen0Length)
                return 0;

            return -1;
        }

        private void ComputeGenerations(ClrSegment segment)
        {
            _generations = new List<GenerationInSegment>();

            if (segment.IsLarge)
            {
                _generations.Add(
                    new GenerationInSegment()
                    {
                        Generation = 3,
                        Start = segment.Gen2Start,
                        End = segment.Gen2Start + segment.Gen2Length,
                        Length = segment.Gen2Length
                    }
                );
            }
            else if (segment.IsEphemeral)
            {
                _generations.Add(
                    new GenerationInSegment()
                    {
                        Generation = 0,
                        Start = segment.Gen0Start,
                        End = segment.Gen0Start + segment.Gen0Length,
                        Length = segment.Gen0Length
                    }
                );
                _generations.Add(
                    new GenerationInSegment()
                    {
                        Generation = 1,
                        Start = segment.Gen1Start,
                        End = segment.Gen1Start + segment.Gen1Length,
                        Length = segment.Gen1Length
                    }
                );
                _generations.Add(
                    new GenerationInSegment()
                    {
                        Generation = 2,
                        Start = segment.Gen2Start,
                        End = segment.Gen2Start + segment.Gen2Length,
                        Length = segment.Gen2Length
                    }
                );
            }
        }
    }


    public class HeapAnalyzer : IAnalyzer
    {
        public HeapAnalyzer(IClrMDHost host)
        {
            _host = host;
        }

        private IClrMDHost _host;
        public IClrMDHost Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public void Run(string args)
        {
            bool success = true;
            try
            {
                ClrMDHelper helper = new ClrMDHelper(_host.Session.Clr);
                var clr = _host.Session.Clr;
                var heap = clr.GetHeap();

                //var segments = heap.Segments;
                //var segmentsInfo = new List<SegmentInfo>(segments.Count);
                //_host.WriteLine($"Segments = {segments.Count}");
                //foreach (ClrSegment segment in segments)
                //{
                //    var segmentInfo = new SegmentInfo(segment);
                //    _host.WriteLine($"{segmentInfo.Number} | {GetSegmentType(segment)} ");
                //    DumpGenerations(segmentInfo);
                //    _host.WriteLine("");
                //}
            }
            catch (Exception x)
            {
                _host.WriteLine(x.ToString());
                success = false;
            }
            finally
            {
                _host.OnAnalysisDone(success);
            }
        }

        //private string GetSegmentType(ClrSegment segment)
        //{
        //    return
        //        (segment.IsLarge) ? "LOH" :
        //        (segment.IsEphemeral) ? "SOH" :
        //        "Gn2";
        //}

        //private void DumpGenerations(SegmentInfo segmentInfo)
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        var genInfo = segmentInfo.GetGeneration(i);
        //        if (genInfo == null)
        //            continue;

        //        _host.WriteLine($"  {genInfo.Generation} | {genInfo.Start.ToString("X")} - {genInfo.End.ToString("X")} ({genInfo.End - genInfo.Start + 1})");
        //    }
        //}
    }
}
