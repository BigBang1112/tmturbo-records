using TmEssentials;

namespace TMTurboRecords.Shared.Models;

public readonly record struct Record(int PlatformRank, TimeInt32? Time, int Count, Platform Platform) : IComparable<Record>
{
    public int GetSkillpoints(int totalRecordCount)
    {
        return (totalRecordCount - (PlatformRank + Count - 1)) * 100 / (PlatformRank + Count - 1);
    }

    public int CompareTo(Record other)
    {
        if (Time == other.Time)
        {
            return Count.CompareTo(other.Count);
        }

        if (Time is null)
        {
            return 1;
        }

        if (other.Time is null)
        {
            return -1;
        }

        return Time.Value.CompareTo(other.Time.Value);
    }
}
