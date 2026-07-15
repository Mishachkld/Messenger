# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This is a learning project in its early planning stage. `docker-compose.yml`, `README.md`, and `docs/MVP.md` are currently empty placeholders, and `backend/api`, `backend/realtime`, and `frontend` are empty directories with no code yet. The only substantive content is `docs/Architecture.md` (written in Russian), which is the authoritative design doc — read it in full before implementing anything, since it defines contracts (WS protocol, DB schema, endpoints) that later code must conform to.

## Architecture (from docs/Architecture.md)

This is a messenger app built in two stages: everything on ASP.NET Core + React first, then the realtime service is rewritten in Go without touching the rest of the system.

**Key design decision**: the realtime layer is a separate service (`backend/realtime`) from day one, speaking a custom JSON protocol over plain WebSocket (WSS) — not SignalR. SignalR bakes in its own client protocol (handshake, hub calls, MessagePack/JSON framing), which would force reimplementing SignalR in Go or rewriting the frontend during the stage-2 migration. A plain WS + JSON contract means swapping .NET → Go only changes one service.

### Services

- **`backend/api`** (ASP.NET Core, permanent) — all business logic and persistence: auth (JWT access+refresh, shared signing key so realtime can validate tokens locally), users/contacts, chats (direct/group), messages. Messages are received via REST (`POST /chats/{id}/messages`), saved to PostgreSQL, then published to Redis Pub/Sub. History is paginated via cursor (`GET /chats/{id}/messages?before=...`). Stack: ASP.NET Core (Minimal API or Controllers), EF Core, PostgreSQL.
- **`backend/realtime`** (ASP.NET Core initially, later Go) — deliberately dumb and stateless so it's easy to rewrite: holds WebSocket connections (`wss://.../ws?token=<jwt>`), validates JWT locally against the shared secret, subscribes to Redis Pub/Sub and fans out events to the right user sockets. Presence (`SETEX presence:{userId}`) is stored in Redis; typing indicators are fanned out but never persisted. **It never writes to the database** except presence keys in Redis. Stage 1: `System.Net.WebSockets` (not SignalR). Stage 2: Go with `nhooyr.io/websocket` or `gorilla/websocket` + `go-redis`, same protocol/channel contract.
- **`frontend`** (React + TypeScript + Vite) — TanStack Query for REST data, Zustand for WS state. A single WS client module handles reconnect with exponential backoff; after reconnect it backfills missed messages via REST (`GET /messages?after=<lastSeenId>`).

### Message send flow

Sending goes over REST, receiving goes over WS — a deliberate asymmetry so WS stays a one-directional delivery channel with no business logic:

1. Client `POST /chats/{id}/messages` (REST, not WS — simpler retries, idempotency via `clientMessageId`).
2. API validates, persists to PostgreSQL, returns `201` with server `id`/`createdAt`.
3. API publishes to Redis channel `chat-events`.
4. Realtime service receives the event, finds open connections for chat participants, fans out.
5. The sender matches incoming events by `clientMessageId` to avoid duplicating its own message.

### WS protocol (must survive the .NET → Go rewrite)

All frames: `{ "type": string, "payload": object }`.

Server → client: `message.new`, `message.read`, `typing`, `presence`.
Client → server: `typing`, `ping`.

Read receipts go through REST (`POST /chats/{id}/read`), then the same Redis relay as messages.

### Data model

PostgreSQL: `users(id, username, password_hash, display_name, created_at)`, `chats(id, type: direct|group, title, created_at)`, `chat_members(chat_id, user_id, joined_at, last_read_message_id)`, `messages(id, chat_id, sender_id, text, client_message_id, created_at)` with index `(chat_id, id)`.

Redis: Pub/Sub channel `chat-events` (API → Realtime transport); `presence:{userId}` with TTL, refreshed by client pings.

### Explicitly out of scope

E2E encryption, file attachments, multi-device sync, horizontal scaling of the realtime service (Redis Pub/Sub already supports it, just not implemented), push notifications.

## Build stages (planned order)

1. Skeleton: docker-compose with Postgres/Redis, API with auth + JWT, frontend with login.
2. Chats and history: chat CRUD, send/read messages via REST only (no realtime — polling).
3. Realtime on .NET: WS service, Redis Pub/Sub, `message.new` delivery.
4. Polish: typing, presence, read receipts, reconnect with backfill.
5. Rewrite realtime in Go: same protocol/channel, can run both services side by side on different ports and switch the frontend via one env var.

## Commands

No build, lint, or test tooling exists yet — `docker-compose.yml` is empty and no project files (`.csproj`, `package.json`, `go.mod`) have been created. Once services are scaffolded, update this section with real commands (dotnet build/test, npm scripts, docker compose up, etc.).
