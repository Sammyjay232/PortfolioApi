using Microsoft.EntityFrameworkCore;
using PortfolioApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Database Context
builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Global Error Handlers
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Register Core Application Services (ADDED THESE LINES)
builder.Services.AddScoped<PortfolioService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<PortfolioQueryService>();

// 4. Configure Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// 5. Middleware Pipeline Configuration
app.UseExceptionHandler();
app.UseAuthorization();
app.MapControllers();

app.Run();
