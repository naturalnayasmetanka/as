import { api } from "@/shared/api/client";

export function logoutWithToken() {
  return api.post<void>("/auth/jwt/logout");
}

export function logoutWithCookie() {
  return api.post<void>("/auth/logout");
}
