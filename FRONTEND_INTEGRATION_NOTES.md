// ПРИМЕР ДЛЯ ФРОНТЕНДА (Angular/TypeScript)
// Это не код для backend, а пояснение как фронтенд должен работать с API

/**
 * 1. LOGIN
 * 
 * POST /auth/jwt/login
 * {
 *   "email": "user@example.com",
 *   "password": "Password123!"
 * }
 * 
 * Response (200 OK):
 * {
 *   "accessToken": "eyJhbGciOiJIUzI1NiIs...",
 *   "expiresAt": "2024-01-01T00:15:00Z"
 * }
 * 
 * Set-Cookie (automatic):
 * refresh_token=<base64>; HttpOnly; Secure; SameSite=Strict; Path=/auth/jwt/refresh
 * 
 * Фронтенд должен:
 * - Сохранить accessToken в памяти (переменная)
 * - HttpOnly cookie установится автоматически (браузер)
 * - Установить таймер на обновление перед истечением (через 14 минут например)
 */

/**
 * 2. API REQUESTS WITH ACCESS TOKEN
 * 
 * Все API запросы:
 * Authorization: Bearer <accessToken>
 * 
 * Пример:
 * GET /api/protected
 * Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
 */

/**
 * 3. REFRESH (SILENT RENEWAL)
 * 
 * Когда accessToken истекает (401 Unauthorized):
 * 
 * POST /auth/jwt/refresh
 * (refresh_token cookie отправится автоматически)
 * 
 * Response (200 OK):
 * {
 *   "accessToken": "eyJhbGciOiJIUzI1NiIs... (НОВЫЙ)",
 *   "expiresAt": "2024-01-01T00:15:00Z"
 * }
 * 
 * Set-Cookie (automatic):
 * refresh_token=<base64 НОВЫЙ>; ...
 * 
 * Фронтенд должен:
 * - Перехватить 401
 * - Вызвать /refresh (один раз, single-flight!)
 * - Обновить accessToken в памяти
 * - Повторить исходный запрос
 * - Если сам /refresh вернул 401, отправить на /login
 */

/**
 * 4. LOGOUT
 * 
 * POST /auth/jwt/logout
 * Authorization: Bearer <accessToken>
 * 
 * Response (200 OK):
 * {}
 * 
 * Set-Cookie (automatic):
 * refresh_token=; Expires=Thu, 01 Jan 1970 00:00:00 GMT; ...
 * 
 * Фронтенд должен:
 * - Очистить accessToken из памяти
 * - Перенаправить на /login
 * - HttpOnly cookie будет очищена автоматически
 */

/**
 * ПРИМЕР INTERCEPTOR (Angular)
 */
/*
@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshSubject$ = new Subject<string>();

  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
	// Добавить access token если есть
	const accessToken = this.authService.getAccessToken();
	if (accessToken) {
	  req = req.clone({
		setHeaders: {
		  Authorization: `Bearer ${accessToken}`
		}
	  });
	}

	return next.handle(req).pipe(
	  catchError((error: HttpErrorResponse) => {
		if (error.status === 401 && !this.isRefreshing) {
		  // Access истек, нужно обновить
		  this.isRefreshing = true;
		  this.refreshSubject$ = new Subject<string>();

		  return this.authService.refresh().pipe(
			switchMap((response) => {
			  this.isRefreshing = false;
			  this.authService.setAccessToken(response.accessToken);
			  this.refreshSubject$.next(response.accessToken);

			  // Повторить исходный запрос с новым токеном
			  return next.handle(
				req.clone({
				  setHeaders: {
					Authorization: `Bearer ${response.accessToken}`
				  }
				})
			  );
			}),
			catchError((refreshError) => {
			  // Ошибка refresh - нужно переавторизоваться
			  this.isRefreshing = false;
			  this.authService.logout();
			  return throwError(() => refreshError);
			})
		  );
		} else if (error.status === 401 && this.isRefreshing) {
		  // Параллельный 401, дождаться обновления
		  return this.refreshSubject$.pipe(
			switchMap((newToken) => {
			  return next.handle(
				req.clone({
				  setHeaders: {
					Authorization: `Bearer ${newToken}`
				  }
				})
			  );
			})
		  );
		}

		return throwError(() => error);
	  })
	);
  }
}
*/

/**
 * БЕЗОПАСНОСТЬ ФРОНТЕНДА:
 * 
 * ✅ Access token в памяти (переменная, localStorage НЕ!)
 * ✅ Refresh в HttpOnly cookie (фронтенд не видит)
 * ✅ CORS with credentials для отправки cookie
 * ✅ Перехватчик для автоматического обновления (single-flight)
 * ✅ При ошибке refresh → logout → /login
 * ✅ Таймер на предупредительное обновление (за 1 минуту до истечения)
 * 
 * ❌ localStorage для access - XSS украдет!
 * ❌ localStorage для refresh - XSS украдет!
 * ❌ session storage для refresh - потеряется при перезагрузке
 * ❌ Без перехватчика - 401 ошибки видны пользователю
 */

/**
 * HTTP CLIENT CONFIG (Angular)
 */
/*
httpClient.get(url, {
  withCredentials: true  // КРИТИЧНО! Для отправки cookie
})
*/

export {};
