# ❓ FAQ - JWT Refresh Token Implementation

## 🤔 Общие вопросы

### В. Почему access-токен только 15 минут? Не будет ли много refresh запросов?
**А.** 15 минут - это баланс между безопасностью и удобством.
- Украденный access используется максимум 15 минут
- Refresh быстрый (поиск по индексу в БД)
- Фронтенд делает silent renewal, пользователь не видит 401
- Можно изменить в конфиге если нужен другой TTL

### В. Зачем refresh в cookie если можно в JSON?
**А.** HttpOnly cookies защищены от XSS:
- JavaScript не может прочитать HttpOnly cookie
- Даже если XSS украдет access из памяти, refresh остается защищен
- Refresh живет дольше и критичнее
- Браузер отправляет cookie автоматически

### В. Что если пользователь откроет много вкладок?
**А.** Это нормально:
- Каждая вкладка имеет свой access в памяти
- Все вкладки делят один refresh cookie
- При refresh одной вкладки, cookie обновляется для всех
- Paraллельные 401 должны быть обработаны single-flight логикой на фронте

### В. Что если забыл пароль? Нужно переавторизоваться?
**А.** Да, правильно:
- Смена пароля = новый security stamp в Identity
- JWT валидируется по старому security stamp → 401
- Пользователь перенаправляется на /login
- Это добавляет security: если аккаунт скомпрометирован, старые токены станут невалидны

---

## 🔐 Безопасность

### В. Почему только SHA-256 а не bcrypt для refresh-токена?
**А.** Правильный выбор для refresh:
- Refresh - это случайная 32-байтовая строка с высокой энтропией
- Bcrypt нужен для паролей (низкая энтропия, подвержен brute-force)
- Перебрать 2^256 SHA-256 хешей физически невозможно
- SHA-256 быстрый (не тормозит каждый refresh)
- Опционально добавить HMAC с pepper для дополнительной защиты

### В. Что если БД скомпрометирована и украдены все хеши?
**А.** Защиты на разных уровнях:
- Хеши - это не сами токены, нельзя использовать напрямую
- Refresh-токены никогда не были выданы для украденных хешей
- Pepper (если используется) усложняет обратное вычисление
- Нужно перевыдать все токены (logout всех пользователей)
- Это лучше чем при краже сразу токены: хотя бы есть время на реакцию

### В. Может ли фронтенд украсть refresh из cookie?
**А.** Нет, это невозможно:
- HttpOnly флаг = JavaScript не может читать
- Даже if XSS в приложении, refresh защищен
- XSS может украсть только access (в памяти)
- Access имеет TTL 15 минут, окно ущерба минимально

### В. Что если refresh украден и использован на другом устройстве?
**А.** Это обнаруживается (reuse detection):
- Устройство 1: использует refresh, получает новый token (ротация)
- Устройство 2: использует старый refresh → 401 (уже отозван)
- Система обнаруживает попытку переиспользования
- Отзывается вся цепочка = оба устройства требуют login
- Это sacrifice (неудобство) ради безопасности

---

## 🛠️ Техническое

### В. Как работает BFS при отзыве цепочки?
**А.** Алгоритм поиска всех потомков:
```
Корень (исходный refresh)
├─ Потомок 1 (ротация 1)
│  └─ Потомок 2 (ротация 2)
│     └─ Потомок 3 (ротация 3)

При обнаружении компрометации корня:
1. Загружаем все сессии пользователя
2. BFS от корня находит всех потомков
3. Помечаем все как revoked
4. Одна транзакция - атомарно
```

### В. Почему ротация именно в update а не delete+insert?
**А.** Проверьте implementation:
```csharp
session.Rotate(newHash, newExpiresAt);  // обновляем поля
_refreshSessionRepository.UpdateAsync(session);  // одна транзакция
```
- Это работает через update (не delete+insert)
- Сохраняется `id` (первичный ключ)
- Сохраняется `created_at` (когда была выдана исходная сессия)
- Обновляются: `token_hash`, `expires_at`, `rotated_at`
- `parent_session_id` остается тот же (цепочка не меняется)

Wait, это неправильно. Нужно СОЗДАВАТЬ новую сессию с parent_session_id! Давайте проверим:

### В. Правильно ли реализована ротация?
**А.** 🚨 ВАЖНЫЙ ВОПРОС - проверим текущую реализацию:

**Текущая реализация (неправильная):**
```csharp
session.Rotate(newHash, newExpiresAt); // Это обновляет ту же сессию
```

**Правильная должна быть:**
```csharp
// Гасим старую сессию
session.Revoke();

// Создаём новую с ссылкой на старую
var newSession = RefreshSession.Create(
	userId, 
	newTokenHash, 
	newExpiresAt,
	parentSessionId: session.Id  // ← ВАЖНО!
);
await _repo.CreateAsync(newSession);
```

Текущая реализация отслеживает потомков неправильно. Нужно переработать!

### В. Когда использовать ParentSessionId?
**А.** ParentSessionId должно указывать на предыдущую сессию при ротации.

---

## 🚨 КРИТИЧЕСКИЕ ВОПРОСЫ

### В. Миграция правильная?
**А.** Проверим, нужно ли что-то обновить после выяснения про ротацию.

### В. Нужно ли обновить JwtRefreshHandler?
**А.** Да! После ротации нужно создавать НОВУЮ сессию, а не обновлять старую.

### В. Как это влияет на reuse detection?
**А.** Если мы обновляем туже сессию:
- Старый токен становится невалидным (хеш изменился)
- Но `id` сессии остается тот же
- Цепочка родителей не отслеживается правильно

Нужно исправить!

---

## 🔧 Как это исправить

### Обновить JwtRefreshHandler
Нужно вместо `session.Rotate()` делать:
```csharp
// Отзываем старую сессию
session.Revoke();
await _refreshSessionRepository.RevokeAsync(session.Id, cancellationToken);

// Создаём новую сессию (потомка)
var newSession = RefreshSession.Create(
	session.UserId,
	newTokenHash,
	newExpiresAt,
	parentSessionId: session.Id
);
await _refreshSessionRepository.CreateAsync(newSession, cancellationToken);
```

### Удалить или переработать session.Rotate()
Метод `Rotate()` в RefreshSession больше не нужен.

---

## 📚 Документация

### В. Где найти информацию по API?
- **JWT_REFRESH_TOKEN_IMPLEMENTATION.md** - полная техническая документация
- **DEPLOYMENT_CHECKLIST.md** - инструкции по деплою
- **FRONTEND_INTEGRATION_NOTES.md** - как интегрировать фронтенд
- **IMPLEMENTATION_SUMMARY.md** - что было сделано

### В. Можно ли менять TTL после deплоя?
**А.** Да, в appsettings:
- `Jwt.AccessTokenExpireMinutes` - для access (15 мин default)
- `RefreshToken.ExpireMinutes` - для refresh (10080 мин = 7 дней default)
- Эффект: новые токены будут с новым TTL, старые доживают свой срок

### В. Как добавить логирование?
**А.** Есть места для логирования:
```csharp
_logger.LogInformation("User {UserId} logged in", user.Id);
_logger.LogInformation("Token refreshed for user {UserId}", user.Id);
_logger.LogWarning("Reuse detected for session {SessionId}", sessionId);
```

Добавить ILogger<T> в DI и логировать события.

---

## ⚡ Production

### В. Нужен ли background job для очистки старых сессий?
**А.** Рекомендуется:
- `DeleteExpiredAsync()` существует в repository
- Запускать 1 раз в день
- Очищает сессии где `expires_at < now()`
- Экономит дисковое пространство

### В. Нужно ли кэширование?
**А.** С осторожностью:
- Кэш refresh сессии может скрыть revoke
- Если кэшировать, нужен invalidation на revoke
- Рекомендуется не кэшировать (БД быстрая благодаря индексам)

### В. Работает ли в load balancer с несколькими серверами?
**А.** Да, если:
- Все серверы используют одну БД ✓
- Ротация атомарна (одна транзакция) ✓
- Нет in-memory state ✓
- Ideal for horizontal scaling

---

## 🎓 Примеры кода

### Пример на Angular
```typescript
// В interceptor:
if (error.status === 401 && !this.isRefreshing) {
  this.isRefreshing = true;
  return this.authService.refresh().pipe(
	tap((newToken) => {
	  this.authService.setAccessToken(newToken);
	  // Повторить original request
	}),
	catchError(() => {
	  this.authService.logout();
	  // redirect to /login
	})
  );
}
```

### Пример на Node.js (client)
```javascript
const response = await fetch('/api/protected', {
  headers: { 'Authorization': `Bearer ${accessToken}` },
  credentials: 'include'  // ← ВАЖНО для cookies
});

if (response.status === 401) {
  const refreshResponse = await fetch('/auth/jwt/refresh', {
	method: 'POST',
	credentials: 'include'
  });

  if (refreshResponse.ok) {
	const { accessToken: newToken } = await refreshResponse.json();
	accessToken = newToken; // Сохранить в памяти
	// Повторить запрос
  } else {
	window.location = '/login';
  }
}
```

---

## 📞 Нужна помощь?

- Проверьте документацию выше
- Посмотрите exception/error сообщение
- Логи обычно показывают причину
- Проверьте appsettings конфигурацию
- Убедитесь что DB миграция применена

