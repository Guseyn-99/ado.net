using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

string connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Задайте переменную окружения PG_CONNECTION_STRING.");

Console.WriteLine("=== Запуск Задачи 10: xmin ===");

var repo = new OrderRepository(connectionString);

try
{
    var order = await repo.GetByIdAsync(99999, CancellationToken.None);
    if (order == null)
    {
        Console.WriteLine("Заказ не найден!");
        return;
    }

    Console.WriteLine($"Заказ: {order.Id}, Сумма: {order.Amount}, xmin: {order.Version}");

    order.Amount += 100.00m;

    await repo.UpdateAsync(order, CancellationToken.None);
    Console.WriteLine("[УСПЕХ]: Заказ обновлен.");
}
catch (DBConcurrencyException ex)
{
    Console.WriteLine($"[КОЛЛИЗИЯ]: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"[ОШИБКА]: {ex.Message}");
}

Console.WriteLine("\nНажмите любую клавишу для выхода...");


public class OrderDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public uint Version { get; set; }
}

public class OrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);

        const string sql = "SELECT id, amount, xmin FROM orders WHERE id = @id";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
        if (await reader.ReadAsync(token).ConfigureAwait(false))
        {
            return new OrderDto
            {
                Id = reader.GetInt32(0),
                Amount = reader.GetDecimal(1),
                Version = reader.GetFieldValue<uint>(2)
            };
        }
        return null;
    }

    public async Task UpdateAsync(OrderDto order, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(token).ConfigureAwait(false);

        const string sql = "UPDATE orders SET amount = @amount WHERE id = @id AND xmin = @version";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("amount", order.Amount);
        command.Parameters.AddWithValue("id", order.Id);
        command.Parameters.Add("version", NpgsqlTypes.NpgsqlDbType.Xid).Value = order.Version;

        int rowsAffected = await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);

        if (rowsAffected == 0)
        {
            throw new DBConcurrencyException("Данные были изменены другим пользователем.");
        }
    }
}

public class DBConcurrencyException : Exception
{
    public DBConcurrencyException(string message) : base(message) { }
}
