using Microsoft.EntityFrameworkCore;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal MinOrderTotal { get; set; }
    public DateTime ExpiresAt { get; set; }   // stored in UTC
    public int MaxUses { get; set; }
    public int TimesUsed { get; set; }
}
public class CouponRedemption { public int Id { get; set; } public int CouponId { get; set; } public int UserId { get; set; } public int OrderId { get; set; } }

public record RedeemRequest(string Code, int OrderId, decimal DiscountAmount);

public class CouponService
{
    private readonly AppDbContext _db;
    public CouponService(AppDbContext db) => _db = db;

    public async Task<decimal> RedeemAsync(int userId, RedeemRequest req)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == req.Code)
                     ?? throw new InvalidOperationException("Invalid coupon");
        if (coupon.ExpiresAt < DateTime.Now) throw new InvalidOperationException("Expired");
        if (coupon.TimesUsed >= coupon.MaxUses) throw new InvalidOperationException("Used up");

        var alreadyUsed = await _db.CouponRedemptions
            .AnyAsync(r => r.CouponId == coupon.Id && r.UserId == userId);
        if (alreadyUsed) throw new InvalidOperationException("Already redeemed");

        var order = await _db.Orders.FirstAsync(o => o.Id == req.OrderId);
        order.Total -= req.DiscountAmount;

        coupon.TimesUsed++;
        _db.CouponRedemptions.Add(new CouponRedemption { CouponId = coupon.Id, UserId = userId, OrderId = order.Id });
        await _db.SaveChangesAsync();
        return order.Total;
    }
}
