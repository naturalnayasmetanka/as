import { api } from "@/shared/api/client";

export interface JwtLoginResponse {
  accessToken: string;
  expiresAt: string;
}

export interface CookieLoginResponse {
  id: string;
  email: string;
}

export type LoginAuthMode = "cookie" | "token";

export function loginWithToken(email: string, password: string) {
  return api.post<JwtLoginResponse>("/auth/jwt/login", {
    email,
    password,
  });
}

export function loginWithCookie(email: string, password: string) {
  return api.post<CookieLoginResponse>("/auth/login", {
    email,
    password,
  });
}
