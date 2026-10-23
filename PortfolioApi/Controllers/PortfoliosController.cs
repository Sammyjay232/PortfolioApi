using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PortfolioApi.Controllers;

[ApiController]
[Route("api/portfolios")]
public class PortfoliosController : ControllerBase
{
    private readonly PortfolioService _portfolioService;
    private readonly TransactionService _transactionService;
    private readonly PortfolioQueryService _queryService;

    public PortfoliosController(PortfolioService ps, TransactionService ts, PortfolioQueryService qs)
    {
        _portfolioService = ps;
        _transactionService = ts;
        _queryService = qs;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioDto dto, CancellationToken ct)
    {
        var result = await _portfolioService.CreatePortfolioAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id) => Ok(new { Id = id });

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _portfolioService.DeletePortfolioAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/transactions")]
    public async Task<IActionResult> PostTransaction(int id, [FromBody] CreateTransactionDto dto, CancellationToken ct)
    {
        var result = await _transactionService.ProcessTransactionAsync(id, dto, ct);
        return Created("", result);
    }

    [HttpGet("{id}/holdings")]
    public async Task<IActionResult> GetHoldings(int id, CancellationToken ct)
    {
        var result = await _queryService.GetHoldingsAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("{id}/summary")]
    public async Task<IActionResult> GetSummary(int id, CancellationToken ct)
    {
        var result = await _queryService.GetSummaryAsync(id, ct);
        return Ok(result);
    }
}
