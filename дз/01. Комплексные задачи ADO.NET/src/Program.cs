using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace NpgsqlTasks
{
    public class Employee { public int Id { get; set; } public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; public decimal Salary { get; set; } }
    public class Part { public int Id { get; set; } public string Name { get; set; } = ""; public string Article { get; set; } = ""; public decimal Price { get; set; } public int Stock { get; set; } }

    class Program
    {
        static readonly string connString = Environment.GetEnvironmentVariable("ADO_NET_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Задайте переменную окружения ADO_NET_CONNECTION_STRING перед запуском.");Port=5432;Database=npgsql_tasks;Username=postgres;Password=PASSWORD;Maximum Pool Size=5;Minimum Pool Size=1";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Задача 1 (Пагинация)");
            await Task1_Pagination();

            Console.WriteLine("\nЗадача 2 (Удаление с проверкой)");
            Task2_Delete(3); // Удаляем запчасть с id=3

            Console.WriteLine("\nЗадача 3 (Статистика)");
            Task3_Stats();

            Console.WriteLine("\nЗадача 4 (Заказ в транзакции)");
            Task4_PlaceOrder(1, 2); // Купить 2 шт. запчасти с id=1

            Console.WriteLine("\nЗадача 5 (Массовое поступление)");
            Task5_Restock();

            Console.WriteLine("\nЗадача 6 (Сырое чтение Reader)");
            Task6_RawRead();

            Console.WriteLine("\nЗадача 7 (DataTable)");
            Task7_DataTable();

            Console.WriteLine("\nЗадача 8 (Асинхронное чтение)");
            var asyncParts = await GetAllPartsAsync();
            Console.WriteLine($"Асинхронно прочитано запчастей: {asyncParts.Count}");

            Console.WriteLine("\nЗадача 9 (Обработка ошибок)");
            Task9_TryAddPart();

            Console.WriteLine("\nЗадача 10 (Пул соединений)");
            await Task10_PoolTest();

            Console.WriteLine("\nНажми Enter для выхода...");
            Console.ReadLine();
        }



        // --- ЗАДАЧА 1 ---
        static async Task Task1_Pagination()
        {
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT id, first_name, last_name, salary FROM employees ORDER BY id LIMIT 5 OFFSET 5", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                Console.WriteLine($"{reader.GetInt32(0)}: {reader.GetString(1)} {reader.GetString(2)}");
        }

        // --- ЗАДАЧА 2 ---
        static void Task2_Delete(int id)
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM parts WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            bool success = cmd.ExecuteNonQuery() > 0;
            Console.WriteLine(success ? $"Запчасть {id} успешно удалена." : $"Запчасть {id} не найдена.");
        }

        // --- ЗАДАЧА 3 ---
        static void Task3_Stats()
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*), COALESCE(SUM(price*stock),0), COALESCE(AVG(price),0) FROM parts", conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Console.WriteLine($"Всего позиций: {reader.GetInt32(0)}");
                Console.WriteLine($"Общая стоимость: {reader.GetDecimal(1)}");
                Console.WriteLine($"Средняя цена: {reader.GetDecimal(2)}");
            }
        }

        // --- ЗАДАЧА 4 ---
        static void Task4_PlaceOrder(int partId, int quantity)
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                decimal price = 0; int stock = 0; bool found = false;
                using (var cmd = new NpgsqlCommand("SELECT price, stock FROM parts WHERE id = @id FOR UPDATE", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", partId);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read()) { found = true; price = reader.GetDecimal(0); stock = reader.GetInt32(1); }
                }

                if (!found || quantity <= 0)
                {
                    tx.Rollback();
                    Console.WriteLine("Запчасть не найдена или количество некорректно. Транзакция отменена.");
                    return;
                }

                if (stock < quantity)
                {
                    tx.Rollback();
                    Console.WriteLine("Недостаточно товара! Транзакция откатана (Rollback).");
                    return;
                }

                using (var cmd = new NpgsqlCommand("INSERT INTO orders (part_id, quantity, total) VALUES (@id, @qty, @total)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", partId);
                    cmd.Parameters.AddWithValue("@qty", quantity);
                    cmd.Parameters.AddWithValue("@total", price * quantity);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand("UPDATE parts SET stock = stock - @qty WHERE id = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", partId);
                    cmd.Parameters.AddWithValue("@qty", quantity);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                Console.WriteLine("Заказ оформлен! Транзакция закоммичена (Commit).");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Console.WriteLine($"Ошибка: {ex.Message}. Откат (Rollback).");
            }
        }





        // --- ЗАДАЧА 5 ---
        static void Task5_Restock()
        {
            var items = new List<(int, int)> { (1, 10), (999, 5) }; // 999 не существует
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var item in items)
                {
                    using var cmd = new NpgsqlCommand("UPDATE parts SET stock = stock + @qty WHERE id = @id", conn, tx);
                    cmd.Parameters.AddWithValue("@id", item.Item1);
                    cmd.Parameters.AddWithValue("@qty", item.Item2);
                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        tx.Rollback();
                        Console.WriteLine($"Запчасть {item.Item1} не найдена. Вся транзакция откачена!");
                        return;
                    }
                }
                tx.Commit();
                Console.WriteLine("Все остатки успешно обновлены.");
            }
            catch { tx.Rollback(); }
        }

        // --- ЗАДАЧА 6 ---
        static void Task6_RawRead()
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM parts LIMIT 2", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dict = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                Console.WriteLine(string.Join(" | ", dict.Select(kv => $"{kv.Key}: {kv.Value}")));
            }
        }

        // --- ЗАДАЧА 7 ---
        static void Task7_DataTable()
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            var adapter = new NpgsqlDataAdapter("SELECT * FROM parts LIMIT 3", conn);
            var dt = new DataTable();
            adapter.Fill(dt);
            foreach (DataRow row in dt.Rows)
                Console.WriteLine($"ID: {row["id"]}, Название: {row["name"]}, Артикул: {row["article"]}");
        }


        // --- ЗАДАЧА 8: Асинхронная версия ---
        static async Task<List<Part>> GetAllPartsAsync()
        {
            var parts = new List<Part>();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync(); // Асинхронное открытие

            await using var cmd = new NpgsqlCommand("SELECT id, name, article, price, stock FROM parts ORDER BY id", conn);
            await using var reader = await cmd.ExecuteReaderAsync(); // Асинхронное чтение

            while (await reader.ReadAsync())
            {
                parts.Add(new Part { Id = reader.GetInt32(0), Name = reader.GetString(1), Article = reader.GetString(2), Price = reader.GetDecimal(3), Stock = reader.GetInt32(4) });
            }
            return parts;
        }

        // --- ЗАДАЧА 10: Тест пула соединений ---
        static async Task Task10_PoolTest()
        {
            Console.WriteLine("Запускаем 20 одновременных запросов...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Благодаря пулу, физически откроется не более 5 соединений
                    await using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    await using var cmd = new NpgsqlCommand("SELECT pg_sleep(0.2)", conn); // Искусственная задержка
                    await cmd.ExecuteNonQueryAsync();
                }));
            }

            await Task.WhenAll(tasks);
            sw.Stop();
            Console.WriteLine($"20 запросов выполнено за {sw.ElapsedMilliseconds} мс. (Лимит пула — 5 соединений)");
        }

        // --- ЗАДАЧА 9 ---
        static void Task9_TryAddPart()
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            try
            {
                // Пытаемся вставить артикул, который уже есть (BRK-001)
                using var cmd = new NpgsqlCommand("INSERT INTO parts (name, article, price, stock) VALUES ('Дубль', 'BRK-001', 100, 1)", conn);
                cmd.ExecuteNonQuery();
                Console.WriteLine("Запись добавлена (ошибки нет).");
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                Console.WriteLine("ОШИБКА (SqlState 23505): Запчасть с таким артикулом уже существует!");
            }
        }
    }
}