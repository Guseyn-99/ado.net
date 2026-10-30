# Билет 1. ASP.NET Core Minimal API + PostgreSQL

Реализация Билета 1 («Интернет-магазин: каталог товаров») на ASP.NET Core Minimal API (.NET 8) с использованием ADO.NET (Npgsql) и Dapper. Всё приложение запускается в Docker.

## 🛠 Стек технологий
*   C# (.NET 8), ASP.NET Core Minimal API
*   Npgsql (ADO.NET)
*   Dapper (Micro-ORM)
*   PostgreSQL 16
*   Docker, Docker Compose

## Реализованные эндпоинты
1.  **GET `/products?min=`** — список товаров с фильтром по минимальной цене (классический ADO.NET: `NpgsqlConnection`, `NpgsqlCommand`, параметры, `async`).
2.  **GET `/products/dapper?min=`** — тот же список, но через Dapper.
3.  **GET `/products/dataset`** — отчет через `DataSet`, `DataAdapter` и `DataTable` с фильтрацией `DataTable.Select`.
4.  **POST `/orders`** — оформление заказа в транзакции (с проверкой и `Commit`).

## 📸 Скриншоты работы
*(Вставь сюда свои скрины из Word, когда перенесешь их в папку screenshots)*

1. **Список товаров (ADO.NET):** `![ADO](screenshots/1_products_ado.png)`
2. **Список товаров (Dapper):** `![Dapper](screenshots/2_products_dapper.png)`
3. **Отчет (DataSet):** `![DataSet](screenshots/3_products_dataset.png)`
4. **Оформление заказа (POST):** `![POST](screenshots/4_orders_post.png)`