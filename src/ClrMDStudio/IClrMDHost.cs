namespace ClrMDStudio
{
    public interface IClrMDHost
    {
        DebuggingSession Session { get; set; }

        void WriteLine(string line);
        void Write(string text);

        void OnAnalysisDone(bool success);
    }
}
