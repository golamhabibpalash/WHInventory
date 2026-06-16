namespace Application.Common.Services.AmountInWordsManager;

public interface IAmountInWordsService
{
    string Convert(decimal amount, string currencyCode = "BDT");
}
