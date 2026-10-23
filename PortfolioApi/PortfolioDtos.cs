using System;
using System.Collections.Generic;

namespace PortfolioApi;

// --- Asset DTOs ---
public record CreateAssetDto(string Symbol, string Name, string AssetType, decimal CurrentPrice);
public record AssetResponseDto(int Id, string Symbol, string Name, string AssetType, decimal CurrentPrice, DateTime PriceUpdatedAtUtc);

// --- Portfolio DTOs ---
public record CreatePortfolioDto(string Name, string OwnerEmail, string? BaseCurrency);
public record PortfolioResponseDto(int Id, string Name, string OwnerEmail, string BaseCurrency, DateTime CreatedAtUtc);

// --- Transaction DTOs ---
public record CreateTransactionDto(string Symbol, string Type, decimal Quantity, decimal? UnitPrice, decimal Fee);
public record TransactionResponseDto(int Id, int PortfolioId, string Symbol, string Type, decimal Quantity, decimal UnitPrice, decimal Fee, string NetAmount, decimal? RealizedPnl, DateTime ExecutedAtUtc);

// --- Analytics Projections DTOs ---
public record HoldingResponseDto(string Symbol, decimal Qty, decimal AvgCost, decimal CostBasis, decimal Price, decimal MarketValue, decimal Unrealized, decimal UnrealizedPercent, decimal Realized);
public record AllocationDto(string AssetType, decimal MarketValue, decimal Percent);
public record SummaryResponseDto(int PortfolioId, string Name, string BaseCurrency, int HoldingsCount, decimal TotalMarketValue, decimal TotalCostBasis, decimal UnrealizedPnl, decimal UnrealizedPnlPercent, decimal RealizedPnl, decimal TotalReturn, List<AllocationDto> Allocation);
