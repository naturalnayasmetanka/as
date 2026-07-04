import { api } from "@/shared/api/client";

export function logout() {
  return api.post<void>("/auth/logout");
}
