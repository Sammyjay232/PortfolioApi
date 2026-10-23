using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PortfolioApi.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly PortfolioDbContext _db;
    public AssetsController(PortfolioDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetDto dto, CancellationToken ct)
    {
        var symbol = dto.Symbol?.Trim().ToUpper() ?? "";

        // Enforce structural validation rules: A-Z and 0-9 up to 10 characters
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Length > 10 || !Regex.IsMatch(symbol, "^[A-Z0-9]+$"))
        {
            throw new ValidationException("symbol", "Symbol must be 1-10 alphanumeric characters (A-Z, 0-9).");
        }

        if (await _db.Assets.AnyAsync(a => a.Symbol == symbol, ct))
        {
            throw new ConflictException($"Asset with symbol '{symbol}' already exists.");
        }

        var asset = new Asset
        {
            Symbol = symbol,
            Name = dto.Name.Trim(),
            AssetType = dto.AssetType.Trim(), // E.g., Stock, Bond, Fund
            CurrentPrice = dto.CurrentPrice,
            PriceUpdatedAtUtc = DateTime.UtcNow
        };

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(ct);

        var response = new AssetResponseDto(asset.Id, asset.Symbol, asset.Name, asset.AssetType, asset.CurrentPrice, asset.PriceUpdatedAtUtc);
        return Created("", response);
    }
}
