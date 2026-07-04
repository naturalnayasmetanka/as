import { api } from "@/shared/api/client";

export function login(email: string, password: string) {
  return api.post<void>("/auth/login", { email, password });
}
