using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;

namespace PortfolioApi;

public class Asset
{
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string AssetType { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public DateTime PriceUpdatedAtUtc { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
}

public class Portfolio
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string OwnerEmail { get; set; } = null!;
    public string BaseCurrency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
}

public class Transaction
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int AssetId { get; set; }
    public string Type { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Fee { get; set; }
    public DateTime ExecutedAtUtc { get; set; }

    public Portfolio Portfolio { get; set; } = null!;
    public Asset Asset { get; set; } = null!;
}

public class Holding
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int AssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal RealizedPnl { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public Portfolio Portfolio { get; set; } = null!;
    public Asset Asset { get; set; } = null!;
}

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Holding> Holdings => Set<Holding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Symbol).IsUnique();
            entity.Property(e => e.Symbol).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.AssetType).IsRequired();
            entity.Property(e => e.CurrentPrice).HasColumnType("decimal(18,4)");
            entity.Property(e => e.PriceUpdatedAtUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Name, e.OwnerEmail }).IsUnique();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.OwnerEmail).IsRequired();
            entity.Property(e => e.BaseCurrency).HasDefaultValue("USD").HasMaxLength(10);
            entity.Property(e => e.CreatedAtUtc).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,6)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Fee).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.ExecutedAtUtc).HasColumnType("datetime2");

            entity.HasOne(t => t.Portfolio).WithMany(p => p.Transactions).HasForeignKey(t => t.PortfolioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Asset).WithMany(a => a.Transactions).HasForeignKey(t => t.AssetId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Holding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PortfolioId, e.AssetId }).IsUnique();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,6)");
            entity.Property(e => e.AverageCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.RealizedPnl).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();

            entity.HasOne(h => h.Portfolio).WithMany(p => p.Holdings).HasForeignKey(h => h.PortfolioId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(h => h.Asset).WithMany(a => a.Holdings).HasForeignKey(h => h.AssetId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
