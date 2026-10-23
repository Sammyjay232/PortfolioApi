# Portfolio Management API

A high-performance, asynchronous, code-first ASP.NET Core Web API built with C# and Entity Framework Core to track financial portfolios, assets, trade transactions, and real-time open holdings.

## 🚀 Key Architectural & Engineering Constraints Met
- **Strictly Async Pipeline:** Every service, controller, and query pathway handles fully asynchronous streaming natively via `CancellationToken` integration.
- **Zero Heavy External Tooling dependencies:** Engineered completely cleanly without `AutoMapper` or `MediatR` to keep payload structural operations explicitly declared and fast.
- **N+1 SQL Resolution Engine:** Projections for open holding lists and portfolio value metrics are completely compiled into highly efficient, **single SELECT execution blocks** to drop overhead database roundtrips.
- **Data Concurrency Locking Guard:** Implements automated row-level concurrency via database `RowVersion` timestamp tokens. This blocks race conditions or multi-user transaction collisions natively at the entity tier.
- **High-Precision Financial Math Operations:** Uses high-precision `decimal` variables internally for holding and transaction metrics. All monetary summaries utilize `MidpointRounding.AwayFromZero` math mapping exclusively inside output/storage definitions to prevent ledger skew.

---

## 🛠️ Testing the Live Endpoints (Local Base: http://localhost:5076)

To test the workflow, execute these HTTP request sequences using Postman, Thunder Client, or cURL commands while your server is running:

### 1. Initialize a Trackable Asset
- **HTTP Method:** `POST`
- **URL Path:** `http://localhost:5076/api/assets`
- **JSON Request Body Body:**
```json
{
  "symbol": "AAPL",
  "name": "Apple Inc.",
  "assetType": "Stock",
  "currentPrice": 175.50
}
```

### 2. Provision a New Investor Portfolio Ledger
- **HTTP Method:** `POST`
- **URL Path:** `http://localhost:5076/api/portfolios`
- **JSON Request Body:**
```json
{
  "name": "Tech Growth Portfolio",
  "ownerEmail": "investor@example.com",
  "baseCurrency": "USD"
}
```
*(Take note of the integer `"id"` returned in this response payload to use in the following endpoints)*

### 3. Record a Financial Buy Transaction
- **HTTP Method:** `POST`
- **URL Path:** `http://localhost:5076/api/portfolios/{portfolioId}/transactions`
- **JSON Request Body:**
```json
{
  "symbol": "AAPL",
  "type": "Buy",
  "quantity": 10.000000,
  "unitPrice": 175.5000,
  "fee": 4.95
}
```

### 4. Fetch the Portfolio Open Holdings Grid (Single-SELECT, No N+1)
- **HTTP Method:** `GET`
- **URL Path:** `http://localhost:5076/api/portfolios/{portfolioId}/holdings`

### 5. Fetch the Consolidated Allocation & PnL Financial Summary
- **HTTP Method:** `GET`
- **URL Path:** `http://localhost:5076/api/portfolios/{portfolioId}/summary`
