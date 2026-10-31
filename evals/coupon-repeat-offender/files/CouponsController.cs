using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController, Authorize]
[Route("api/coupons")]
public class CouponsController : ControllerBase
{
    private readonly CouponService _coupons;
    public CouponsController(CouponService coupons) => _coupons = coupons;

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem(RedeemRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var newTotal = await _coupons.RedeemAsync(userId, req);
        return Ok(new { total = newTotal });
    }
}
