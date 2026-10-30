CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    price NUMERIC(10,2) NOT NULL
);
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    product_id INT REFERENCES products(id),
    qty INT NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);
INSERT INTO products (name, price) VALUES
    ('Ноутбук', 75000.00),
    ('Мышь', 1500.00),
    ('Клавиатура', 3500.00);