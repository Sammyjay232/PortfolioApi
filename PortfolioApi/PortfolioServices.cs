using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PortfolioApi;

public static class FinanceMath
{
    public static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    public static decimal RoundAvgCost(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}

public class PortfolioService
{
    private readonly PortfolioDbContext _db;
    public PortfolioService(PortfolioDbContext db) => _db = db;

    public async Task<PortfolioResponseDto> CreatePortfolioAsync(CreatePortfolioDto dto, CancellationToken ct)
    {
        var normalizedEmail = dto.OwnerEmail.Trim().ToLower();
        var normalizedName = dto.Name.Trim();

        if (await _db.Portfolios.AnyAsync(p => p.Name == normalizedName && p.OwnerEmail == normalizedEmail, ct))
            throw new ConflictException("A portfolio with this name already exists for this owner.");

        var portfolio = new Portfolio
        {
            Name = normalizedName,
            OwnerEmail = normalizedEmail,
            BaseCurrency = string.IsNullOrWhiteSpace(dto.BaseCurrency) ? "USD" : dto.BaseCurrency,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Portfolios.Add(portfolio);
        await _db.SaveChangesAsync(ct);
        return new PortfolioResponseDto(portfolio.Id, portfolio.Name, portfolio.OwnerEmail, portfolio.BaseCurrency, portfolio.CreatedAtUtc);
    }

    public async Task DeletePortfolioAsync(int id, CancellationToken ct)
    {
        var portfolio = await _db.Portfolios.Include(p => p.Transactions).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException($"Portfolio {id} does not exist.");

        if (portfolio.Transactions.Any())
            throw new ConflictException("A portfolio that has transactions cannot be deleted.");

        _db.Portfolios.Remove(portfolio);
        await _db.SaveChangesAsync(ct);
    }
}

public class TransactionService
{
    private readonly PortfolioDbContext _db;
    public TransactionService(PortfolioDbContext db) => _db = db;

    public async Task<TransactionResponseDto> ProcessTransactionAsync(int portfolioId, CreateTransactionDto dto, CancellationToken ct)
    {
        var symbol = dto.Symbol.Trim().ToUpper();
        var type = dto.Type.Trim();

        var portfolio = await _db.Portfolios.FindAsync(new object[] { portfolioId }, ct)
            ?? throw new NotFoundException($"Portfolio {portfolioId} does not exist.");

        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Symbol == symbol, ct)
            ?? throw new NotFoundException($"Asset '{symbol}' does not exist.");

        decimal unitPrice = dto.UnitPrice ?? asset.CurrentPrice;
        decimal fee = dto.Fee;
        decimal qty = dto.Quantity;

        if (qty <= 0) throw new ValidationException("quantity", "Quantity must be greater than 0.");
        if (fee < 0) throw new ValidationException("fee", "Fee cannot be negative.");

        var holding = await _db.Holdings.FirstOrDefaultAsync(h => h.PortfolioId == portfolioId && h.AssetId == asset.Id, ct);

        decimal oldQty = holding?.Quantity ?? 0m;
        decimal oldAvg = holding?.AverageCost ?? 0m;
        decimal? realizedPnl = null;

        if (type.Equals("Buy", StringComparison.OrdinalIgnoreCase))
        {
            decimal newQty = oldQty + qty;
            decimal newAvg = (oldQty * oldAvg + qty * unitPrice + fee) / newQty;

            if (holding == null)
            {
                holding = new Holding { PortfolioId = portfolioId, AssetId = asset.Id, Quantity = newQty, AverageCost = FinanceMath.RoundAvgCost(newAvg) };
                _db.Holdings.Add(holding);
            }
            else
            {
                holding.Quantity = newQty;
                holding.AverageCost = FinanceMath.RoundAvgCost(newAvg);
            }
        }
        else if (type.Equals("Sell", StringComparison.OrdinalIgnoreCase))
        {
            if (oldQty < qty)
                throw new ConflictException($"Cannot sell {qty:G29} {symbol}; portfolio {portfolioId} holds {oldQty:G29}.");

            decimal cashIn = (qty * unitPrice) - fee;
            realizedPnl = cashIn - (qty * oldAvg);

            holding!.Quantity = oldQty - qty;
            holding.RealizedPnl += realizedPnl.Value;

            if (holding.Quantity == 0m)
            {
                holding.AverageCost = 0m;
            }
        }
        else
        {
            throw new ValidationException("type", "Unknown transaction type.");
        }

        var transaction = new Transaction
        {
            PortfolioId = portfolioId,
            AssetId = asset.Id,
            Type = type,
            Quantity = qty,
            UnitPrice = unitPrice,
            Fee = fee,
            ExecutedAtUtc = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("A concurrent updates structural conflict occurred. Please retry transaction execution.");
        }

        decimal grossAmount = qty * unitPrice;
        decimal netAmountVal = type.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? (grossAmount + fee) : (grossAmount - fee);

        return new TransactionResponseDto(
            transaction.Id, portfolioId, symbol, type, qty, unitPrice, fee,
            netAmountVal.ToString("F2"),
            realizedPnl.HasValue ? FinanceMath.RoundMoney(realizedPnl.Value) : null,
            transaction.ExecutedAtUtc);
    }
}

public class PortfolioQueryService
{
    private readonly PortfolioDbContext _db;
    public PortfolioQueryService(PortfolioDbContext db) => _db = db;

    public async Task<List<HoldingResponseDto>> GetHoldingsAsync(int portfolioId, CancellationToken ct)
    {
        var exists = await _db.Portfolios.AnyAsync(p => p.Id == portfolioId, ct);
        if (!exists) throw new NotFoundException($"Portfolio {portfolioId} does not exist.");

        var rawHoldings = await _db.Holdings
            .Where(h => h.PortfolioId == portfolioId && h.Quantity > 0m)
            .Select(h => new
            {
                h.Asset.Symbol,
                Qty = h.Quantity,
                AvgCost = h.AverageCost,
                Price = h.Asset.CurrentPrice,
                Realized = h.RealizedPnl
            })
            .ToListAsync(ct);

        return rawHoldings.Select(h =>
        {
            var costBasis = h.Qty * h.AvgCost;
            var marketValue = h.Qty * h.Price;
            var unrealized = marketValue - costBasis;
            var unrealizedPercent = costBasis == 0m ? 0m : (unrealized / costBasis) * 100m;

            return new HoldingResponseDto(
                h.Symbol, h.Qty,
                FinanceMath.RoundAvgCost(h.AvgCost),
                FinanceMath.RoundMoney(costBasis),
                FinanceMath.RoundMoney(h.Price),
                FinanceMath.RoundMoney(marketValue),
                FinanceMath.RoundMoney(unrealized),
                FinanceMath.RoundMoney(unrealizedPercent),
                FinanceMath.RoundMoney(h.Realized)
            );
        }).OrderByDescending(x => x.MarketValue).ToList();
    }

    public async Task<SummaryResponseDto> GetSummaryAsync(int portfolioId, CancellationToken ct)
    {
        var portfolio = await _db.Portfolios
            .Select(p => new { p.Id, p.Name, p.BaseCurrency })
            .FirstOrDefaultAsync(p => p.Id == portfolioId, ct)
            ?? throw new NotFoundException($"Portfolio {portfolioId} does not exist.");

        var items = await _db.Holdings
            .Where(h => h.PortfolioId == portfolioId)
            .Select(h => new
            {
                h.Asset.AssetType,
                h.Quantity,
                h.AverageCost,
                h.Asset.CurrentPrice,
                h.RealizedPnl
            }).ToListAsync(ct);

        decimal totalCostBasis = 0;
        decimal totalMarketValue = 0;
        decimal totalRealized = items.Sum(i => i.RealizedPnl);
        int openHoldingsCount = 0;

        var typeGroupedMarketValues = new Dictionary<string, decimal>();

        foreach (var item in items)
        {
            if (item.Quantity > 0)
            {
                openHoldingsCount++;
                var cb = item.Quantity * item.AverageCost;
                var mv = item.Quantity * item.CurrentPrice;

                totalCostBasis += cb;
                totalMarketValue += mv;

                if (!typeGroupedMarketValues.ContainsKey(item.AssetType))
                    typeGroupedMarketValues[item.AssetType] = 0;
                typeGroupedMarketValues[item.AssetType] += mv;
            }
        }

        decimal totalUnrealized = totalMarketValue - totalCostBasis;
        decimal totalUnrealizedPercent = totalCostBasis == 0 ? 0 : (totalUnrealized / totalCostBasis) * 100m;
        decimal totalReturn = totalUnrealized + totalRealized;

        var allocations = typeGroupedMarketValues.Select(kvp => new AllocationDto(
            kvp.Key,
            FinanceMath.RoundMoney(kvp.Value),
            FinanceMath.RoundMoney(totalMarketValue == 0 ? 0 : (kvp.Value / totalMarketValue) * 100m)
        )).OrderBy(a => a.AssetType).ToList();

        return new SummaryResponseDto(
            portfolio.Id, portfolio.Name, portfolio.BaseCurrency, openHoldingsCount,
            FinanceMath.RoundMoney(totalMarketValue),
            FinanceMath.RoundMoney(totalCostBasis),
            FinanceMath.RoundMoney(totalUnrealized),
            FinanceMath.RoundMoney(totalUnrealizedPercent),
            FinanceMath.RoundMoney(totalRealized),
            FinanceMath.RoundMoney(totalReturn),
            allocations
        );
    }
}
