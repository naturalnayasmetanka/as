"use client";

import { useEffect, useState } from "react";
import { ApiError } from "@/shared/api/client";
import { RoleGate } from "@/shared/auth";
import { assignAdminUserRole, getAdminUsers, type AdminUser } from "@/entities/user";

const assignableRoles = ["Employee", "Moderator", "Admin"];

export default function AdminUsersPage() {
  return (
    <RoleGate roles={["Admin"]}>
      <AdminUsersContent />
    </RoleGate>
  );
}

function AdminUsersContent() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);
  const [pendingKey, setPendingKey] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function loadUsers() {
    setLoading(true);
    setError(null);
    setForbidden(false);

    try {
      const data = await getAdminUsers();
      setUsers(data);
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

  async function handleAssign(accountId: string, role: string) {
    const key = `${accountId}:${role}`;
    setPendingKey(key);
    setError(null);
    setNotice(null);

    try {
      await assignAdminUserRole(accountId, role);
      await loadUsers();
      setNotice("Роль назначена. Пользователю нужно перелогиниться или дождаться refresh access-токена.");
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        setForbidden(true);
      } else {
        setError(err instanceof ApiError ? err.message : "Не удалось назначить роль");
      }
    } finally {
      setPendingKey(null);
    }
  }

  return (
    <main className="mx-auto w-full max-w-6xl px-4 py-8">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-slate-950">Пользователи</h1>
          <p className="mt-1 text-sm text-slate-600">Роли назначаются через Identity и применяются в новых JWT.</p>
        </div>
        <button
          type="button"
          onClick={loadUsers}
          disabled={loading}
          className="h-10 border border-slate-300 bg-white px-3 text-sm font-medium text-slate-800 hover:bg-slate-50 disabled:opacity-60"
        >
          Обновить
        </button>
      </div>

      {notice && (
        <p className="mt-5 border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
          {notice}
        </p>
      )}

      {error && (
        <p role="alert" className="mt-5 border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
          {error}
        </p>
      )}

      {forbidden && (
        <div className="mt-5 border border-rose-200 bg-rose-50 p-6 text-rose-800">
          <h2 className="text-xl font-semibold">403</h2>
          <p className="mt-2 text-sm">Сервер отказал в доступе к admin API.</p>
        </div>
      )}

      {loading && <p className="mt-6 text-sm text-slate-500">Загрузка...</p>}

      {!loading && !forbidden && !error && users.length === 0 && (
        <p className="mt-6 border border-slate-200 bg-white p-6 text-sm text-slate-500">Пользователей пока нет.</p>
      )}

      {!loading && !forbidden && users.length > 0 && (
        <div className="mt-6 overflow-x-auto border border-slate-200 bg-white">
          <table className="w-full min-w-[760px] border-collapse text-left text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="border-b border-slate-200 px-4 py-3 font-medium">Email</th>
                <th className="border-b border-slate-200 px-4 py-3 font-medium">Роли</th>
                <th className="border-b border-slate-200 px-4 py-3 font-medium">Назначить</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id} className="border-b border-slate-100 last:border-b-0">
                  <td className="px-4 py-3 font-medium text-slate-900">{user.email}</td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      {user.roles.length > 0 ? (
                        user.roles.map((role) => (
                          <span key={role} className="border border-slate-200 bg-slate-50 px-2 py-1 text-xs text-slate-700">
                            {role}
                          </span>
                        ))
                      ) : (
                        <span className="text-slate-500">Нет ролей</span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      {assignableRoles.map((role) => {
                        const hasRole = user.roles.some((userRole) => userRole.toLowerCase() === role.toLowerCase());
                        const key = `${user.id}:${role}`;

                        return (
                          <button
                            key={role}
                            type="button"
                            onClick={() => handleAssign(user.id, role)}
                            disabled={hasRole || pendingKey !== null}
                            className="h-9 border border-slate-300 bg-white px-3 text-xs font-medium text-slate-800 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            {pendingKey === key ? "..." : hasRole ? "Есть" : role}
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
    </main>
  );
}
