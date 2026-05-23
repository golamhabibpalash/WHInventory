namespace Domain.Common;

public class BaseEntity : IHasSequentialId, IHasIsDeleted, IHasAudit
{
    public string Id { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedById { get; set; }

    public BaseEntity()
    {
        Id = GenerateSequentialGuid();
        IsDeleted = false;
    }



    private static readonly object _lock = new object();

    private static string GenerateSequentialGuid()
    {
        var guidArray = Guid.NewGuid().ToByteArray();

        var baseDate = new DateTime(1900, 1, 1);
        var now = DateTime.UtcNow;

        var timeSpan = now - baseDate;
        var daysArray = BitConverter.GetBytes(timeSpan.Days);
        var msecsArray = BitConverter.GetBytes((long)(timeSpan.TotalMilliseconds % 86400000));

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);
        }

        lock (_lock)
        {
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);
        }

        return new Guid(guidArray).ToString();
    }
}

