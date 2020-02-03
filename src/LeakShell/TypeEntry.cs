namespace LeakShell    {

public class TypeEntry
{
    public string MethodTable { get; set; }
    public long Count { get; set; }
    public long TotalSize { get; set; }
    public string Name { get; set; }

    public override string ToString()
    {
        return $"{Count,6} {TotalSize.ToString("#,#"),11}  {MethodTable}  {Name}";
    }
}
}
