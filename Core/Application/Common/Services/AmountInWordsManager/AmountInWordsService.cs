namespace Application.Common.Services.AmountInWordsManager;

public class AmountInWordsService : IAmountInWordsService
{
    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty",
        "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    public string Convert(decimal amount, string currencyCode = "BDT")
    {
        if (amount == 0)
            return "Zero Taka Only";

        var taka = (long)Math.Floor(amount);
        var paisa = (int)Math.Round((amount - taka) * 100);

        var parts = new List<string>();

        if (taka > 0)
            parts.Add(ConvertInteger(taka) + " Taka");

        if (paisa > 0)
            parts.Add(ConvertBelowHundred(paisa) + " Paisa");

        return string.Join(" and ", parts) + " Only";
    }

    private static string ConvertInteger(long number)
    {
        if (number == 0) return "";

        var parts = new List<string>();

        if (number >= 10_000_000)
        {
            parts.Add(ConvertInteger(number / 10_000_000) + " Crore");
            number %= 10_000_000;
        }

        if (number >= 100_000)
        {
            parts.Add(ConvertInteger(number / 100_000) + " Lakh");
            number %= 100_000;
        }

        if (number >= 1_000)
        {
            parts.Add(ConvertInteger(number / 1_000) + " Thousand");
            number %= 1_000;
        }

        if (number >= 100)
        {
            parts.Add(Ones[number / 100] + " Hundred");
            number %= 100;
        }

        if (number > 0)
            parts.Add(ConvertBelowHundred((int)number));

        return string.Join(" ", parts);
    }

    private static string ConvertBelowHundred(int number)
    {
        if (number < 20)
            return Ones[number];

        return Tens[number / 10] + (number % 10 > 0 ? " " + Ones[number % 10] : "");
    }
}
