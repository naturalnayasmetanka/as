import { api } from "@/shared/api/client";

export function register(email: string, password: string) {
  return api.post<void>("/register", { email, password });
}
