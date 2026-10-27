# PortfolioApi

An investment portfolio tracking Web API built with modern .NET and Entity Framework Core. This application acts as a financial tracking ledger, allowing users to create portfolios, register assets, record transaction histories (buys/sells), and track real-time analytics like dollar-weighted average costs, asset allocation, and profit/loss (PnL).

## 🚀 Key Features

*   **Portfolio Management:** Support for multiple user-managed portfolios with local base currency designations.
*   **Transaction Processing Engine:** Complete transaction ledger ensuring valid execution parameters (e.g., short-selling prevention, automated fee integrations).
*   **Real-Time Analytics:** Dynamically calculated holding metrics including total market values, cost basis tracking, and realized/unrealized PnL summaries.
*   **Asset Allocation:** On-the-fly portfolio diversification breakdowns grouped by distinct asset types (e.g., Stocks, Cryptocurrencies, ETFs).
*   **Robust Architecture:** Features a native central exception pipeline mapping errors into standard RFC problem details and modern thread-safe database concurrency constraints.

## 🛠️ Built With

*   **.NET 10** (C#)
*   **Entity Framework Core 10** (Database ORM)
*   **SQL Server / LocalDB** (Relational Database)
*   **OpenAPI/Swagger** (Interactive API Testing Tools)

## 📦 Database Schema Structure

The application maps out the following interconnected models in the relational database:
*   **Portfolios:** Core entity managing owner info and preferred currency settings.
*   **Assets:** Global tracking database mapping symbol identifiers, asset types, and current valuations.
*   **Transactions:** Historical ledger record tracking asset trade snapshots (Buy/Sell actions).
*   **Holdings:** Running balances capturing active holdings quantities, cost basis averages, and cumulative trade metrics.

## 🏁 Getting Started

### Prerequisites

*   [.NET 10 SDK](https://microsoft.com) or later.
*   [Visual Studio 2022](https://microsoft.com) (with ASP.NET and web development workloads) or JetBrains Rider / VS Code.
*   SQL Server LocalDB instance (`(localdb)\mssqllocaldb`).

### Installation & Local Setup

1.  **Clone the Repository**
    ```bash
    git clone https://github.com
    cd PortfolioApi
    ```

2.  **Verify Configuration**
    Ensure your local connection string in `appsettings.json` points to a valid LocalDB instance:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PortfolioDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
    ```

3.  **Apply Database Migrations**
    Open your terminal or Package Manager Console inside the project directory and run the initialization commands:
    ```bash
    dotnet ef database update
    ```

4.  **Run the Application**
    Start the local web server:
    ```bash
    dotnet run
    ```
    The console output will display the listening local addresses (e.g., `http://localhost:5076`). You can append `/swagger` or use native OpenAPI tools to explore the active endpoints.
