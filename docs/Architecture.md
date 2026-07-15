# Архитектура мессенджера

Обучающий проект. Этап 1 — всё на ASP.NET Core + React. Этап 2 — real-time сервис переписывается на Go без изменений в остальной системе.

## Ключевое решение

Real-time слой с самого начала выделен в **отдельный сервис** (`backend/realtime`) с собственным протоколом поверх чистого WebSocket (WSS), а не SignalR. Причина: SignalR — это не просто транспорт, а свой клиентский протокол (handshake, hub-вызовы, MessagePack/JSON-фреймы). Если завязаться на него, при переходе на Go придётся либо реализовывать протокол SignalR вручную, либо переписывать и клиент. С собственным JSON-протоколом поверх WS замена .NET → Go меняет только один сервис, фронтенд не трогается.

## Схема

```
                ┌──────────────┐
                │  React SPA   │
                └──┬───────┬───┘
          REST/HTTPS       WSS (свой протокол)
                ┌──▼───┐ ┌─▼──────────┐
                │ API  │ │  Realtime  │  ← этап 2: переписывается на Go
                │ .NET │ │ .NET → Go  │
                └─┬──┬─┘ └─┬──────┬───┘
                  │  │     │      │
             ┌────▼┐ └──►──┴─┐    │ (валидация JWT — локально, по общему ключу)
             │ PG  │ │ Redis │◄───┘
             └─────┘ │pub/sub│
                     └───────┘
```

## Сервисы

### `backend/api` — ASP.NET Core (остаётся навсегда)

Вся бизнес-логика и хранение:

- **Auth**: регистрация/логин, выдача JWT (access + refresh). JWT подписывается общим ключом — realtime-сервис валидирует токен сам, без похода в API.
- **Users**: профили, поиск, контакты.
- **Chats**: личные и групповые, участники.
- **Messages**: приём сообщения (REST `POST /chats/{id}/messages`), сохранение в PostgreSQL, затем публикация события в Redis Pub/Sub.
- **История**: `GET /chats/{id}/messages?before=...` — пагинация курсором.

Стек: ASP.NET Core Minimal API или Controllers, **Dapper + рукописный SQL** (не EF Core — цель проекта в том числе выучить SQL), PostgreSQL, миграции — чистые `.sql`-файлы (DbUp или просто скрипт-раннер).

Структура — feature folders (вертикальные слайсы), а не слои `Controllers/Services/Repositories`: вся фича лежит в одной папке, внутренние файлы (endpoints / service / repository / models) — по необходимости, без пустых прослоек.

```
backend/api/
├── Program.cs            # DI, middleware, маппинг эндпоинтов
├── Features/
│   ├── Auth/             # AuthEndpoints, AuthService (JWT, хеши), модели
│   ├── Users/
│   ├── Chats/
│   └── Messages/         # можно слить с Chats, если репозитории срастутся
└── Common/
    ├── Database/         # фабрика подключений, раннер миграций
    ├── Migrations/       # 001_users.sql, 002_chats.sql...
    ├── Auth/             # JWT-мидлварь, CurrentUser-хелпер
    └── Events/           # публикация в Redis (с этапа 4)
```

### `backend/realtime` — сначала .NET, потом Go

Максимально тупой и stateless — именно поэтому его легко переписать:

- Держит WebSocket-соединения (`wss://.../ws?token=<jwt>` или токен первым фреймом).
- Валидирует JWT локально (общий ключ/секрет с API).
- Подписан на Redis Pub/Sub; получив событие, рассылает его в сокеты нужных пользователей.
- Presence (онлайн/оффлайн) и typing-индикаторы: хранит в Redis (`SETEX presence:{userId}`), typing вообще не персистится — только fan-out.
- **Не пишет в базу.** Единственная запись — presence в Redis.

Этап 1: ASP.NET Core + `System.Net.WebSockets` (не SignalR).
Этап 2: Go + `nhooyr.io/websocket` или `gorilla/websocket` + `go-redis`. Контракт (протокол ниже + канал Redis) не меняется.

### `frontend` — React

- React + TypeScript + Vite.
- Состояние: TanStack Query для REST-данных, лёгкий стор (Zustand) для WS-состояния.
- Один WS-клиент-модуль с reconnect + экспоненциальным backoff; после reconnect — дозагрузка пропущенных сообщений через REST (`GET /messages?after=<lastSeenId>`).

## Поток отправки сообщения

1. Клиент шлёт `POST /chats/{id}/messages` (REST, не WS — проще ретраи, идемпотентность через `clientMessageId`).
2. API валидирует, сохраняет в PostgreSQL, возвращает `201` с серверным `id` и `createdAt`.
3. API публикует событие в Redis: канал `chat-events`, payload — JSON события.
4. Realtime-сервис получает событие, находит открытые соединения участников чата и рассылает.
5. Клиент-отправитель матчит входящее событие по `clientMessageId` (не дублирует своё же сообщение).

Отправка через REST, а получение через WS — осознанное упрощение: WS остаётся однонаправленным каналом доставки (плюс typing/presence от клиента), и realtime-сервису не нужна бизнес-логика.

## WS-протокол (контракт, который переживёт переход на Go)

Все фреймы — JSON `{ "type": string, "payload": object }`.

Сервер → клиент:

```json
{ "type": "message.new",   "payload": { "chatId": 1, "id": 42, "senderId": 7, "text": "...", "clientMessageId": "uuid", "createdAt": "..." } }
{ "type": "message.read",  "payload": { "chatId": 1, "userId": 7, "lastReadMessageId": 42 } }
{ "type": "typing",        "payload": { "chatId": 1, "userId": 7 } }
{ "type": "presence",      "payload": { "userId": 7, "status": "online" } }
```

Клиент → сервер (минимум):

```json
{ "type": "typing",  "payload": { "chatId": 1 } }
{ "type": "ping" }
```

Read-receipts клиент отправляет через REST (`POST /chats/{id}/read`), дальше та же схема через Redis.

## Данные

PostgreSQL (упрощённо):

- `users(id, username, password_hash, display_name, created_at)`
- `chats(id, type: direct|group, title, created_at)`
- `chat_members(chat_id, user_id, joined_at, last_read_message_id)`
- `messages(id, chat_id, sender_id, text, client_message_id, created_at)` — индекс `(chat_id, id)`

Redis:

- Pub/Sub канал `chat-events` — транспорт API → Realtime.
- `presence:{userId}` с TTL — онлайн-статус (обновляется ping-ами).

## docker-compose

Сервисы: `postgres`, `redis`, `api`, `realtime`, `frontend` (dev — Vite, прод — nginx). На этапе 2 в compose меняется только образ `realtime`.

## Этапы

1. **Скелет**: compose с Postgres/Redis, API с auth + JWT, фронт с логином.
2. **Чаты и история**: CRUD чатов, отправка/чтение сообщений через REST (без real-time — polling).
3. **Realtime на .NET**: WS-сервис, Redis Pub/Sub, доставка `message.new`.
4. **Полировка**: typing, presence, read-receipts, reconnect с дозагрузкой.
5. **Переписывание realtime на Go**: тот же протокол и канал Redis; можно гонять оба сервиса параллельно на разных портах и переключить фронт одной переменной окружения.

## Что осознанно за скоупом

E2E-шифрование, вложения/файлы, доставка на несколько устройств с синхронизацией, горизонтальное масштабирование realtime (хотя Redis Pub/Sub его уже допускает — все инстансы получают все события), push-уведомления.
