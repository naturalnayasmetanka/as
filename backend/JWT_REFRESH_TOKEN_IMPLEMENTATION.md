# JWT Refresh Token Implementation

## 📋 Обзор

Реализована полноценная схема JWT с refresh-токенами в соответствии с AUTH-5 требованиями. Система включает:

- **Короткий Access Token** (15 минут) - stateless, невозможно отозвать
- **Долгий Refresh Token** (7 дней) - хранится на сервере, отзываемый
- **Ротация refresh-токенов** - каждый refresh гасит старый токен и выдает новый
- **Reuse Detection** - обнаружение и блокировка скомпрометированных токенов
- **HttpOnly Cookies** - защита от XSS

## 🏗️ Архитектура

### Domain Models
- **RefreshSession** (`AuthService.Domain/RefreshSessions/RefreshSession.cs`)
  - Хранит информацию о активной refresh-сессии пользователя
  - `TokenHash` - SHA-256 хеш refresh-токена (не сам токен!)
  - `IsRevoked` - флаг отзыва
  - `ParentSessionId` - для отслеживания цепочки ротаций

### Services

#### IRefreshTokenService
```csharp
public interface IRefreshTokenService
{
	string GenerateToken();           // Генерирует 32 байта в Base64
	string ComputeTokenHash(string token); // SHA-256 хеш
}
```

#### IRefreshSessionRepository
Работает с БД:
- `CreateAsync` - создать новую сессию
- `FindValidByTokenHashAsync` - найти активную сессию по хешу
- `UpdateAsync` - ротация токена
- `RevokeAsync` - отозвать одну сессию
- `RevokeSessionChainAsync` - отозвать всю цепочку (при reuse detection)

### Endpoints

#### POST /auth/jwt/login
**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGc...",
  "expiresAt": "2024-01-01T00:15:00Z"
}
```

**Set-Cookie Header:**
```
refresh_token=<base64-token>; HttpOnly; Secure; SameSite=Strict; Path=/auth/jwt/refresh; Max-Age=604800
```

#### POST /auth/jwt/refresh
**Headers:** (токен в automatic cookie)

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGc...",
  "expiresAt": "2024-01-01T00:15:00Z"
}
```

**Set-Cookie:** новый refresh-токен (ротация)

#### POST /auth/jwt/logout
**Authorization:** Bearer <accessToken>

**Response (200 OK):**
```json
{}
```

**Set-Cookie:** очищен refresh_token

## 🔐 Безопасность

### Хеширование Refresh-токена
```csharp
// В БД хранится ТОЛЬКО хеш:
var hash = SHA256.ComputeHash(token); // SHA-256
// Опционально: HMAC с перцем для дополнительной защиты
```

### Cookie Флаги

| Флаг | Значение | Назначение |
|------|----------|-----------|
| HttpOnly | true | Недоступен для JS (защита от XSS) |
| Secure | true* | Передается только по HTTPS |
| SameSite | Strict | Защита от CSRF |
| Path | /auth/jwt/refresh | Узкое распространение |

*В localhost = false для разработки

### Ротация
Каждый `/refresh` **обязательно** гасит предыдущий токен:
```
Токен 1 → ParentSessionId = null (исходный)
Токен 2 → ParentSessionId = Токен1.Id (ротирован)
Токен 3 → ParentSessionId = Токен2.Id
...
```

### Reuse Detection
Если попытка использовать **отозванный** refresh-токен:
1. Сессия не найдена или `IsRevoked = true`
2. Отзывается **вся цепочка** (все предки и потомки)
3. Пользователь должен переавторизоваться

## 🗄️ База данных

### Таблица `refresh_sessions` (schema: auth)

```sql
CREATE TABLE auth.refresh_sessions (
	id UUID PRIMARY KEY,
	user_id UUID NOT NULL,
	token_hash VARCHAR(64) NOT NULL,         -- SHA-256 hex
	expires_at TIMESTAMP WITH TIME ZONE,
	is_revoked BOOLEAN DEFAULT false,
	created_at TIMESTAMP WITH TIME ZONE,
	rotated_at TIMESTAMP WITH TIME ZONE,     -- NULL для новой, дата для ротированной
	parent_session_id UUID                   -- ссылка на предыдущий токен
);

CREATE INDEX idx_refresh_sessions_user_id ON auth.refresh_sessions(user_id);
CREATE INDEX idx_refresh_sessions_token_hash ON auth.refresh_sessions(token_hash);
CREATE INDEX idx_refresh_sessions_expires_at ON auth.refresh_sessions(expires_at);
```

## ⚙️ Конфигурация

### appsettings.json

```json
{
  "Jwt": {
	"Issuer": "AuthService",
	"Audience": "AuthService.ApiClients",
	"AccessTokenExpireMinutes": 15,
	"SigningKey": "ваш-очень-длинный-ключ-минимум-32-байта"
  },
  "RefreshToken": {
	"ExpireMinutes": 10080,           // 7 * 24 * 60 = неделя
	"AccessTokenExpireMinutes": 15,   // дублирование из Jwt
	"TokenLengthBytes": 32,           // 32 байта = ~43 символа в Base64
	"Pepper": null                    // опциональный HMAC-pepper
  }
}
```

## 📊 Жизненный цикл токена

```
Вход (login)
	├─ Проверка учетных данных
	├─ Создание access-токена (15 мин)
	├─ Создание refresh-токена (7 дней)
	├─ Сохранение hash в БД
	└─ Отправка cookie с refresh-токеном

Обновление access-токена (refresh)
	├─ Получение refresh из cookie
	├─ Вычисление hash и поиск в БД
	├─ Проверка: не отозван? не истек?
	├─ Ротация: гашение старого, создание нового
	├─ Обновление БД (одна транзакция!)
	└─ Отправка нового refresh cookie

Выход (logout)
	├─ Отзыв текущей refresh-сессии в БД
	├─ Очистка refresh cookie
	└─ Access доживает свой TTL (15 мин)

Компрометация (reuse detection)
	├─ Попытка использовать отозванный токен
	├─ Отзыв всей цепочки сессий
	└─ 401 Unauthorized
```

## 🔍 Примеры реализации

### Генерация токена
```csharp
var refreshToken = _refreshTokenService.GenerateToken(); // "abc123=="
var hash = _refreshTokenService.ComputeTokenHash(refreshToken); // "3f2e..."
var session = RefreshSession.Create(userId, hash, expiresAt);
await _repo.CreateAsync(session);
```

### Ротация токена
```csharp
var session = await _repo.FindValidByTokenHashAsync(hash);
var newToken = _refreshTokenService.GenerateToken();
var newHash = _refreshTokenService.ComputeTokenHash(newToken);
session.Rotate(newHash, newExpiresAt);  // ParentSessionId автоматически сохраняется
await _repo.UpdateAsync(session);
```

### Обнаружение кражи
```csharp
var session = await _repo.FindValidByTokenHashAsync(hash);
if (session is null && /* проверим, есть ли отозванная с таким hash */)
{
	await _repo.RevokeSessionChainAsync(sessionId); // Отзываем цепочку!
	return 401;
}
```

## 🚀 Развертывание

### Миграция БД
```bash
dotnet ef database update
```

### Переменные окружения
```bash
Jwt__SigningKey=your-secret-key-min-32-bytes
RefreshToken__ExpireMinutes=10080
RefreshToken__Pepper=optional-hmac-pepper
```

## ✅ Чек-лист требований

- ✅ Access-токен 15 минут
- ✅ Refresh-сессия на сервере (таблица в БД)
- ✅ Refresh в HttpOnly cookie (не в JSON!)
- ✅ SHA-256 хеш (не сам токен в БД)
- ✅ Ротация refresh-токена
- ✅ Reuse detection + отзыв цепочки
- ✅ /auth/jwt/login возвращает access + refresh cookie
- ✅ /auth/jwt/refresh обновляет оба токена
- ✅ /auth/jwt/logout отзывает сессию
- ✅ Только хеш в базе (защита от дампа)
- ✅ Secure + HttpOnly + SameSite
- ✅ Ротация атомарна (одна транзакция)

## 📝 Замечания

1. **Access не отзывается** - это осознанное решение per RFC 9700. Граница защиты - короткий TTL.
2. **ParentSessionId** - для отслеживания цепочки при обнаружении кражи.
3. **Pepper опционален** - рекомендуется для production, хранится в env/secrets.
4. **DeleteExpiredAsync** - можно вызывать периодически (cron job) для очистки БД.
5. **ClockSkew** - validate lifetime с ClockSkew=Zero для точности.

## 🔗 Связанные файлы

```
backend/src/
├── AuthService.Core/
│   ├── Authentication/
│   │   ├── JwtOptions.cs
│   │   ├── JwtTokenService.cs
│   │   ├── RefreshTokenOptions.cs
│   │   ├── RefreshTokenService.cs
│   │   └── Abstractions/
│   │       ├── IJwtTokenService.cs
│   │       └── IRefreshTokenService.cs
│   ├── Database/
│   │   └── Abstractions/
│   │       └── IRefreshSessionRepository.cs
│   └── Features/
│       ├── JwtLogin.cs
│       ├── JwtRefresh.cs
│       └── JwtLogout.cs
├── AuthService.Domain/
│   └── RefreshSessions/
│       └── RefreshSession.cs
├── AuthService.Infrastructure.Postgres/
│   ├── Configurations/
│   │   └── RefreshSessionConfiguration.cs
│   ├── Repositories/
│   │   └── RefreshSessionRepository.cs
│   ├── Migrations/
│   │   └── 20260706141138_AddRefreshSessions.cs
│   └── AuthServiceDbContext.cs
└── AuthService.Web/
	└── appsettings*.json
```
