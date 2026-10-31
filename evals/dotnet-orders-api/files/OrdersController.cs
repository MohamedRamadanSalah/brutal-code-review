using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orders;
    public OrdersController(OrderService orders) => _orders = orders;

    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        var order = _orders.GetByIdAsync(id).Result;
        if (order == null) return NotFound();
        LogAudit(id);
        return Ok(order);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string term)
        => Ok(await _orders.SearchAsync(term));

    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id)
    {
        await _orders.CloseOrderAsync(id);
        return Ok(new { success = true });
    }

    private async void LogAudit(int id)
    {
        await System.IO.File.AppendAllTextAsync("audit.log", $"viewed {id} at {DateTime.Now}\n");
    }
}
