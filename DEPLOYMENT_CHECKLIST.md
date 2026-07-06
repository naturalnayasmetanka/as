# 🚀 Deployment Checklist - JWT Refresh Token

## ✅ Pre-Deployment

### Code Review
- [ ] Все файлы добавлены в git
- [ ] Нет merge conflicts
- [ ] CI/CD проходит успешно
- [ ] Code style соответствует стандартам

### Security Review
- [x] Refresh-токены хранятся в HttpOnly cookies
- [x] Refresh-токены никогда не в JSON ответе
- [x] Только SHA-256 хеш хранится в БД
- [x] Ротация атомарна (одна транзакция)
- [x] Reuse detection реализовано
- [x] Access не отзывается (TTL граница)
- [x] Secure флаг установлен для cookies
- [x] SameSite = Strict для CSRF защиты

### Configuration Review
- [ ] SigningKey установлен (>= 32 байта)
- [ ] Pepper опционально установлен (если нужен HMAC)
- [ ] AccessTokenExpireMinutes = 15
- [ ] RefreshToken ExpireMinutes = 10080 (7 дней)

## 📋 Migration

### Local Development
```bash
# 1. Обновить БД до последней миграции
cd backend
dotnet ef database update

# 2. Проверить что таблица создана
psql -U postgres -d local_as_db_ -c "\dt auth.refresh_sessions"
```

### Staging/Production
```bash
# 1. Backup БД !!!
pg_dump production_db > backup_$(date +%Y%m%d_%H%M%S).sql

# 2. Применить миграцию
dotnet ef database update --configuration Release

# 3. Проверить миграция
SELECT * FROM auth.refresh_sessions LIMIT 0;

# 4. Проверить индексы
SELECT * FROM pg_indexes 
WHERE schemaname = 'auth' 
AND tablename = 'refresh_sessions';
```

## 🔐 Secrets Management

### Azure Key Vault / AWS Secrets Manager
```json
{
  "Jwt--SigningKey": "your-super-secret-key-min-32-bytes-production",
  "RefreshToken--Pepper": "optional-hmac-pepper-for-extra-security"
}
```

### Environment Variables
```bash
export Jwt__SigningKey="..."
export RefreshToken__Pepper="..."
export Jwt__AccessTokenExpireMinutes="15"
export RefreshToken__ExpireMinutes="10080"
```

### Docker/Kubernetes
```yaml
env:
  - name: Jwt__SigningKey
	valueFrom:
	  secretKeyRef:
		name: auth-secrets
		key: jwt-signing-key
  - name: RefreshToken__Pepper
	valueFrom:
	  secretKeyRef:
		name: auth-secrets
		key: refresh-pepper
```

## 🧪 Testing Before Deploy

### API Testing
```bash
# 1. Login
TOKEN_RESPONSE=$(curl -s -X POST http://localhost:5000/auth/jwt/login \
  -H "Content-Type: application/json" \
  -d '{
	"email":"test@example.com",
	"password":"Password123!"
  }')

ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.accessToken')
echo "Access Token: $ACCESS_TOKEN"

# 2. Refresh
REFRESH_RESPONSE=$(curl -s -X POST http://localhost:5000/auth/jwt/refresh \
  -c cookies.txt \
  -b cookies.txt)

NEW_ACCESS=$(echo $REFRESH_RESPONSE | jq -r '.accessToken')
echo "New Access Token: $NEW_ACCESS"

# 3. Verify different API with new token
curl -X GET http://localhost:5000/auth/me \
  -H "Authorization: Bearer $NEW_ACCESS"

# 4. Logout
curl -X POST http://localhost:5000/auth/jwt/logout \
  -H "Authorization: Bearer $NEW_ACCESS" \
  -b cookies.txt

# 5. Try refresh after logout (должен вернуть 401)
curl -X POST http://localhost:5000/auth/jwt/refresh \
  -b cookies.txt
```

### Integration Tests
```bash
# Запустить unit тесты
dotnet test --configuration Release

# Запустить с покрытием
dotnet test --configuration Release \
  /p:CollectCoverageMetrics=true
```

### Load Testing (k6/JMeter)
```bash
# Проверить производительность refresh на нагрузке
# Expected: < 50ms для каждого refresh
# DB Connection pooling должен работать правильно
```

## 📊 Monitoring Post-Deploy

### Метрики для отслеживания
- [ ] Rate `/auth/jwt/login` успешных входов
- [ ] Rate `/auth/jwt/refresh` успешных обновлений
- [ ] Rate 401 Unauthorized на /refresh (истекшие токены)
- [ ] Rate reuse detection (скомпрометированные токены)
- [ ] Average latency на refresh (< 50ms)
- [ ] DB connection pool usage

### Логирование
```csharp
// Рекомендуется логировать:
_logger.LogInformation("User {UserId} logged in", userId);
_logger.LogInformation("User {UserId} refreshed token", userId);
_logger.LogWarning("Reuse detected for session {SessionId}, revoking chain", sessionId);
_logger.LogWarning("Refresh token expired for user {UserId}", userId);
_logger.LogError("Refresh failed: {Reason}", reason);
```

### Alerts
- [ ] Аномально высокий процент 401 на /refresh
- [ ] Aномально высокий процент reuse detection
- [ ] DB query time для FindValidByTokenHashAsync > 100ms
- [ ] Refresh endpoint latency > 200ms

## 🔄 Rollback Plan

### Если что-то пошло не так
```bash
# 1. Откатить код
git revert <commit-hash>
dotnet build

# 2. Откатить миграцию БД
dotnet ef database update <previous-migration-name>
# или
dotnet ef database update InitialCreate

# 3. Очистить refresh cookies у клиентов
# - Не требуется, они автоматически истекут
# - Фронтенд должен обработать 401 и отправить на /login

# 4. Проверить что все работает
curl -X POST http://localhost:5000/auth/login ...
```

## 📅 Post-Deployment

### День 1
- [ ] Мониторить логи на ошибки
- [ ] Проверить что старые клиенты с long-lived access работают
- [ ] Проверить что браузер отправляет cookies правильно
- [ ] Проверить Cross-Origin requests (withCredentials)

### Неделя 1
- [ ] Собрать метрики производительности
- [ ] Проверить graceful handling 401 на фронтенде
- [ ] Убедиться что никто не кэширует refresh-токены
- [ ] Проверить что истекшие сессии удаляются

### Месяц 1
- [ ] Анализировать reuse detection эвенты (может быть угроза)
- [ ] Проверить что хеши и пепперы работают правильно
- [ ] Убедиться что нет утечек токенов в логах
- [ ] Провести пентест если требуется

## 🏥 Health Checks

### Endpoint Status
```bash
curl http://localhost:5000/health
# Должно быть: { "status": "Healthy" }
```

### Database Connection
```bash
curl http://localhost:5000/health/ready
# Проверяет что БД доступна
```

### Refresh Session Table
```sql
SELECT COUNT(*) as active_sessions FROM auth.refresh_sessions 
WHERE NOT is_revoked AND expires_at > now();

SELECT COUNT(*) as revoked_sessions FROM auth.refresh_sessions 
WHERE is_revoked;

SELECT COUNT(*) as expired_sessions FROM auth.refresh_sessions 
WHERE expires_at < now();
```

## 📱 Client Updates Required

### Frontend Changes Needed
- [ ] HTTP Interceptor для 401 + refresh retry
- [ ] Single-flight логика для параллельных 401
- [ ] Session timeout handler
- [ ] Graceful login redirect

### Mobile Apps
- [ ] Убедиться что cookies работают (URLSession/OkHttp configuration)
- [ ] Implement retry logic
- [ ] Handle refresh failures

## 🔐 Security Hardening (Post-Deploy)

### Optional Enhancements
- [ ] Добавить Device Fingerprinting (для detection anomalies)
- [ ] Rate limiting на /login и /refresh
- [ ] IP whitelist для некритичных операций
- [ ] Require MFA для sensitive операций
- [ ] Add audit logging для всех auth операций

## 📞 Support & Runbook

### Common Issues

**401 после login**
- RefreshSession не создана
- Проверить: AccessToken генерируется? Hash сохраняется?
- Решение: Проверить логи, очистить БД, повторить login

**Cookies не отправляются**
- Frontend не использует `withCredentials: true`
- CORS не позволяет credentials
- Решение: Добавить `AllowCredentials` в CORS policy

**Reuse Detection срабатывает ложно**
- Несколько вкладок используют старый токен
- Race condition в refresh
- Решение: Хотя это нормально для detection, документировать поведение

**Очень много revoked sessions**
- Возможно атака
- Или просто много пользователей
- Решение: Проверить логи, запустить cleanup, alert на security team

## ✨ Final Checklist

- [ ] Код собирается без ошибок и warnings
- [ ] Тесты проходят (100% критические)
- [ ] Миграция подготовлена и протестирована
- [ ] Secrets настроены в target environment
- [ ] CORS настроен для withCredentials
- [ ] Логирование включено
- [ ] Мониторинг/алерты настроены
- [ ] Frontend готов с interceptor
- [ ] Документация обновлена
- [ ] Rollback план готов
- [ ] Team уведомлена

---

**Автор**: AI Assistant  
**Дата**: 2024-01-06  
**Версия**: 1.0
