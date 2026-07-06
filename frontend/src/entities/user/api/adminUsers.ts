import { api } from "@/shared/api/client";

export interface AdminUser {
  id: string;
  email: string;
  roles: string[];
}

export function getAdminUsers() {
  return api.get<AdminUser[]>("/auth/admin/users");
}

export function assignAdminUserRole(accountId: string, role: string) {
  return api.post<void>(`/auth/admin/users/${accountId}/roles/${role}`);
}
