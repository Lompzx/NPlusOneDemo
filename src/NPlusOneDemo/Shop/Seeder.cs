namespace NPlusOneDemo.Shop;

public static class Seeder
{
    public const int CustomerCount = 200;
    public const int ProductCount = 50;

    private static readonly string[] PaymentMethods = ["Card", "Pix", "Voucher"];

    public static async Task SeedAsync(ShopContext db, int orderCount)
    {
        // Fixed seed: every run produces the same data, so the numbers are comparable.
        var random = new Random(42);

        var customers = Enumerable.Range(1, CustomerCount)
            .Select(i => new Customer { Name = $"Customer {i}" })
            .ToList();
        var products = Enumerable.Range(1, ProductCount)
            .Select(i => new Product { Name = $"Product {i}", Price = random.Next(500, 50_000) / 100m })
            .ToList();
        db.AddRange(customers);
        db.AddRange(products);

        for (var i = 0; i < orderCount; i++)
        {
            var order = new Order
            {
                CreatedAt = new DateTime(2026, 1, 1).AddMinutes(i * 37),
                Customer = customers[random.Next(customers.Count)],
            };

            for (var j = random.Next(1, 21); j > 0; j--)
            {
                var product = products[random.Next(products.Count)];
                order.Items.Add(new OrderItem { Product = product, Quantity = random.Next(1, 6), UnitPrice = product.Price });
            }

            for (var j = random.Next(1, 6); j > 0; j--)
                order.Payments.Add(new Payment
                {
                    Method = PaymentMethods[random.Next(PaymentMethods.Length)],
                    Amount = random.Next(1_000, 100_000) / 100m,
                });

            db.Orders.Add(order);
        }

        await db.SaveChangesAsync();
    }
}
