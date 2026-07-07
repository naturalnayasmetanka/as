"use client";

import { useEffect, useState } from "react";
import { assignUserRole, getAdminUsers, type AdminUser } from "@/entities/admin-user";
import { RoleGate } from "@/shared/session/ui";
import { ApiError } from "@/shared/api/client";
import { ForbiddenScreen } from "@/shared/ui/ForbiddenScreen";

const AVAILABLE_ROLES = ["Employee", "Moderator", "Admin"];

function normalizeUsers(users: AdminUser[]): AdminUser[] {
  return users.map((user) => ({
    ...user,
    roles: Array.isArray(user.roles) ? user.roles : [],
  }));
}

export default function AdminUsersPage() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [forbidden, setForbidden] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<string | null>(null);

  async function loadUsers() {
    setLoading(true);
    setError(null);
    setForbidden(false);

    try {
      const data = await getAdminUsers();
      setUsers(normalizeUsers(data));
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setForbidden(true);
      } else {
        setError(err instanceof ApiError ? err.message : "Не удалось загрузить пользователей");
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadUsers();
  }, []);

  async function handleAssignRole(userId: string, role: string) {
    const actionId = `${userId}:${role}`;
    setPendingAction(actionId);
    setError(null);
    setNotice(null);

    try {
      await assignUserRole(userId, role);
      await loadUsers();
      setNotice("Роль изменена. Пользователю нужно перелогиниться или дождаться refresh access-токена.");
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setForbidden(true);
      } else {
        setError(err instanceof ApiError ? err.message : "Не удалось назначить роль");
      }
    } finally {
      setPendingAction(null);
    }
  }

  return (
    <RoleGate allowedRoles={["Admin"]}>
      {forbidden ? (
        <ForbiddenScreen />
      ) : (
        <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
          <section className="mx-auto max-w-6xl">
            <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <p className="text-sm font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Админка
                </p>
                <h1 className="mt-2 text-3xl font-semibold text-slate-950">
                  Пользователи
                </h1>
              </div>
              <button
                type="button"
                onClick={loadUsers}
                disabled={loading}
                className="inline-flex h-10 items-center justify-center rounded-lg border border-slate-300 bg-white px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Обновить
              </button>
            </div>

            {notice && (
              <p className="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                {notice}
              </p>
            )}

            {error && (
              <p role="alert" className="mb-4 rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                {error}
              </p>
            )}

            <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
              {loading ? (
                <div className="p-6 text-sm text-slate-600">Загрузка...</div>
              ) : users.length === 0 ? (
                <div className="p-6 text-sm text-slate-600">Пользователей пока нет.</div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full min-w-[760px] border-collapse text-left text-sm">
                    <thead className="bg-slate-50 text-slate-600">
                      <tr>
                        <th className="border-b border-slate-200 px-4 py-3 font-semibold">Email</th>
                        <th className="border-b border-slate-200 px-4 py-3 font-semibold">ID</th>
                        <th className="border-b border-slate-200 px-4 py-3 font-semibold">Роли</th>
                        <th className="border-b border-slate-200 px-4 py-3 font-semibold">Назначить</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((user) => (
                        <tr key={user.id} className="border-b border-slate-100 last:border-0">
                          <td className="px-4 py-4 font-medium text-slate-950">{user.email}</td>
                          <td className="px-4 py-4 font-mono text-xs text-slate-500">{user.id}</td>
                          <td className="px-4 py-4 text-slate-700">
                            {user.roles.length > 0 ? user.roles.join(", ") : "Нет ролей"}
                          </td>
                          <td className="px-4 py-4">
                            <div className="flex flex-wrap gap-2">
                              {AVAILABLE_ROLES.map((role) => {
                                const actionId = `${user.id}:${role}`;
                                const isPending = pendingAction === actionId;
                                const alreadyAssigned = user.roles
                                  .map((userRole) => userRole.toLowerCase())
                                  .includes(role.toLowerCase());

                                return (
                                  <button
                                    key={role}
                                    type="button"
                                    onClick={() => handleAssignRole(user.id, role)}
                                    disabled={Boolean(pendingAction) || alreadyAssigned}
                                    className="inline-flex h-9 min-w-24 items-center justify-center rounded-md bg-slate-950 px-3 text-xs font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-300"
                                  >
                                    {isPending ? "..." : role}
                                  </button>
                                );
                              })}
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </section>
        </main>
      )}
    </RoleGate>
  );
}
