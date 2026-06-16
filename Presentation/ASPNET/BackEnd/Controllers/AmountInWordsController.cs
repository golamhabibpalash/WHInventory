using Application.Common.Services.AmountInWordsManager;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class AmountInWordsController : BaseApiController
{
    private readonly IAmountInWordsService _amountInWordsService;

    public AmountInWordsController(ISender sender, IAmountInWordsService amountInWordsService) : base(sender)
    {
        _amountInWordsService = amountInWordsService;
    }

    [Authorize]
    [HttpGet("ConvertAmountInWords")]
    public ActionResult<ApiSuccessResult<ConvertAmountInWordsResult>> ConvertAmountInWordsAsync(
        [FromQuery] decimal amount,
        [FromQuery] string currencyCode = "BDT")
    {
        var words = _amountInWordsService.Convert(amount, currencyCode);

        return Ok(new ApiSuccessResult<ConvertAmountInWordsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ConvertAmountInWordsAsync)}",
            Content = new ConvertAmountInWordsResult { AmountInWords = words }
        });
    }
}

public record ConvertAmountInWordsResult
{
    public string? AmountInWords { get; init; }
}
