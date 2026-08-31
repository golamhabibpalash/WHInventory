namespace Application.Common.Extensions;

/// <summary>
/// Monetary amounts are held to two decimals everywhere. Anything finer cannot be invoiced,
/// paid or reconciled, and the fractions a double carries would otherwise accumulate through
/// line totals into a document total that no payment settles exactly.
/// </summary>
public static class MoneyExtensions
{
    public const int MoneyDecimals = 2;

    /// <summary>
    /// Rounds away from zero, the way money is rounded on an invoice — .NET's default is
    /// banker's rounding, which would send 0.125 down to 0.12.
    /// </summary>
    public static double ToMoney(this double value)
    {
        return Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);
    }

    public static double? ToMoney(this double? value)
    {
        return value.HasValue ? value.Value.ToMoney() : null;
    }
}
