# KPO KR3 - Асинхронное взаимодействие
Решение контрольной работы №3 (8 баллов) для микросервисов Order и Payment на C# с RabbitMQ, Outbox/Inbox.

## Запуск
1. Убедитесь, что установлен Docker и Docker Compose.
2. В корне проекта выполните:
   ```
   docker-compose up --build
   ```
3. Order Service будет доступен на `http://localhost:5001/swagger`
   Payment Service на `http://localhost:5002/swagger`

## Postman
Импортируйте `PostmanCollection.json` для тестирования API.