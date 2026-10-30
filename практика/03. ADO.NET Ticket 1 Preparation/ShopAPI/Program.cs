using System.Data;
using Dapper;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Строка подключения (в Docker она берется из переменной окружения)
string cs = Environment.GetEnvironmentVariable("CONN_STRING") 
    ?? "Host=localhost;Port=5432;Database=shopdb;Username=postgres;Password=postgres;Pooling=true;Maximum Pool Size=20";

// 1. Список товаров (ADO.NET + параметры + async)
app.MapGet("/products", async (decimal min) =>
{
    var list = new List<object>();
    await using var conn = new NpgsqlConnection(cs);
    await conn.OpenAsync();
    
    await using var cmd = new NpgsqlCommand("SELECT id, name, price FROM products WHERE price >= @min ORDER BY id", conn);
    cmd.Parameters.AddWithValue("@min", min);
    
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        list.Add(new { Id = reader.GetInt32(0), Name = reader.GetString(1), Price = reader.GetDecimal(2) });

    return Results.Ok(list);
});

// 2. Тот же список через Dapper
app.MapGet("/products/dapper", async (decimal min) =>
{
    await using var conn = new NpgsqlConnection(cs);
    var products = await conn.QueryAsync("SELECT id, name, price FROM products WHERE price >= @min ORDER BY id", new { min });
    return Results.Ok(products);
});

// 3. Отчёт через DataSet / DataAdapter / DataTable
app.MapGet("/products/dataset", () =>
{
    using var conn = new NpgsqlConnection(cs);
    conn.Open();
    
    using var adapter = new NpgsqlDataAdapter("SELECT * FROM products", conn);
    var ds = new DataSet();
    adapter.Fill(ds, "products");

    var table = ds.Tables["products"]!;
    // Фильтруем через DataTable.Select (например, ищем товары дороже 2000)
    var filtered = table.Select("price > 2000");

    var result = filtered.Select(r => new
    {
        Id = r["id"],
        Name = r["name"],
        Price = r["price"]
    });

    return Results.Ok(result);
});

// 4. Оформление заказа в транзакции
app.MapPost("/orders", async (OrderReq req) =>
{
    await using var conn = new NpgsqlConnection(cs);
    await conn.OpenAsync();
    await using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Вставляем заказ
        await using var cmd = new NpgsqlCommand("INSERT INTO orders (product_id, qty) VALUES (@pid, @qty)", conn, tx);
        cmd.Parameters.AddWithValue("@pid", req.ProductId);
        cmd.Parameters.AddWithValue("@qty", req.Qty);
        await cmd.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        return Results.Ok(new { Message = "Заказ оформлен" });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.Run();

record OrderReq(int ProductId, int Qty);