using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Shared;

public sealed class RecordComparer : IComparer<Record>
{
    public int Compare(Record x, Record y)
    {
        if (x.Time == y.Time)
        {
            return x.Count.CompareTo(y.Count);
        }

        if (x.Time is null)
        {
            return 1;
        }

        if (y.Time is null)
        {
            return -1;
        }

        return x.Time.Value.CompareTo(y.Time.Value);
    }
}
