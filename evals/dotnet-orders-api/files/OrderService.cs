using Microsoft.EntityFrameworkCore;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Open";
    public double Total { get; set; }
    public DateTime ClosedAt { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
}
public class OrderLine { public int Id { get; set; } public int OrderId { get; set; } public double Price { get; set; } public int Qty { get; set; } }

public class OrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(int id) =>
        _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<List<Order>> SearchAsync(string term) =>
        await _db.Orders.FromSqlRaw($"SELECT * FROM Orders WHERE Status LIKE '%{term}%'").ToListAsync();

    public async Task CloseOrderAsync(int id)
    {
        var order = await _db.Orders.FirstAsync(o => o.Id == id);
        var lines = await _db.OrderLines.Where(l => l.OrderId == id).ToListAsync();
        order.Total = 0;
        foreach (var line in lines)
        {
            order.Total += line.Price * line.Qty;
            await _db.SaveChangesAsync();
        }
        order.Status = "Closed";
        order.ClosedAt = DateTime.Now;
        await _db.SaveChangesAsync();
    }
}
