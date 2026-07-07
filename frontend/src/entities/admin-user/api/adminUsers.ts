import { api } from "@/shared/api/client";
import type { AdminUser } from "../model/types";

export function getAdminUsers() {
  return api.get<AdminUser[]>("/auth/admin/users");
}

export function assignUserRole(userId: string, role: string) {
  return api.post<void>(`/auth/admin/users/${userId}/roles/${role}`);
}
