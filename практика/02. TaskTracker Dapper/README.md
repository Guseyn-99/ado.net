# TaskTracker: Практика Dapper + PostgreSQL

Учебный проект — мини-трекер задач (проекты → задачи → теги), демонстрирующий возможности микро-ORM **Dapper** в связке с **PostgreSQL**.

## 🛠 Стек технологий
*   C# (.NET 8)
*   Dapper (Micro-ORM)
*   Npgsql (ADO.NET провайдер для PostgreSQL)
*   PostgreSQL

## Реализованный функционал
В приложении реализованы все ключевые сценарии из гайда:
*   **Создание и чтение** (INSERT ... RETURNING, QueryFirstOrDefault).
*   **Связи один-ко-многим** (получение проекта вместе с его задачами через `splitOn`).
*   **Массовая вставка** (быстрая вставка задач через `unnest`).
*   **Работа с JSONB** (настройки проектов и метаданные задач).
*   **Работа с массивами** (теги проектов, поиск по тегам через `ANY`).
*   **Пагинация** (через `QueryMultiple`).
*   **Обновление с проверкой** (UPDATE с `RETURNING`).
*   **UPSERT** (INSERT ... ON CONFLICT DO UPDATE).

## 📸 Скриншоты работы

**Главное меню программы:**

<img width="410" height="282" alt="1_screenshot " src="https://github.com/user-attachments/assets/97f81d85-7c2b-4b7e-b9d8-60c1cfb91d8a" />

**Результат работы (вывод проекта):**

<img width="410" height="291" alt="2_screenshot " src="https://github.com/user-attachments/assets/51f1329c-3105-4b21-9056-592a4be7a49b" />
