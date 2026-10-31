using Microsoft.EntityFrameworkCore;
public class Order { public int Id { get; set; } public int UserId { get; set; } public decimal Total { get; set; } }
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<Order> Orders => Set<Order>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        b.Entity<Coupon>().Property(c => c.Amount).HasPrecision(18, 2);
        b.Entity<Order>().Property(o => o.Total).HasPrecision(18, 2);
    }
}
