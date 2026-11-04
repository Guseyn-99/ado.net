-- Таблицы для выполнения отдельных упражнений.
-- Запускать в БД npgsql_tasks. Использовать только учебную БД.
CREATE TABLE IF NOT EXISTS employees (
    id SERIAL PRIMARY KEY,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    email TEXT,
    salary NUMERIC(12,2) NOT NULL
);
INSERT INTO employees (first_name, last_name, email, salary)
SELECT 'Имя ' || n, 'Фамилия ' || n, 'employee' || n || '@example.org', 50000+n*100
FROM generate_series(1, 15) AS n
WHERE NOT EXISTS (SELECT 1 FROM employees);

CREATE TABLE IF NOT EXISTS orders (
    id INT PRIMARY KEY,
    customer_id INT NOT NULL,
    amount NUMERIC(12,2) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
INSERT INTO orders (id, customer_id, amount)
VALUES (99999, 1, 5000.00)
ON CONFLICT (id) DO NOTHING;

CREATE TABLE IF NOT EXISTS accounts (
    id INT PRIMARY KEY,
    balance NUMERIC(12,2) NOT NULL
);
INSERT INTO accounts (id, balance)
VALUES (1, 10000.00), (2, 10000.00)
ON CONFLICT (id) DO NOTHING;

CREATE TABLE IF NOT EXISTS documents (
    id SERIAL PRIMARY KEY,
    file_name TEXT NOT NULL,
    content BYTEA NOT NULL
);
INSERT INTO documents (file_name, content)
SELECT 'example.txt', convert_to('Пример файла', 'UTF8')
WHERE NOT EXISTS (SELECT 1 FROM documents);

CREATE TABLE IF NOT EXISTS batch_items (
    id INT PRIMARY KEY,
    name TEXT NOT NULL
);
