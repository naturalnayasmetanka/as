"use client";

import type { User } from "@/entities/user";
import { LogoutButton } from "@/features/auth/logout";
import { getSessionUser } from "@/shared/session";

interface ProfileCardProps {
  user: User;
  authType?: "cookie" | "token";
  onLogout?: () => void;
}

export function ProfileCard({ user, authType = "cookie", onLogout }: ProfileCardProps) {
  const roles = getSessionUser()?.roles ?? [];

  return (
    <section className="mx-auto flex w-full max-w-xl flex-col gap-6 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
      <div className="space-y-3">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">Профиль</p>
        <h1 className="text-3xl font-semibold text-slate-950">Добро пожаловать</h1>
        <p className="text-sm leading-6 text-slate-600">
          Вы успешно вошли в систему. Ниже указаны данные пользователя и текущий тип авторизации.
        </p>
      </div>

      <div className="grid gap-4 rounded-xl bg-slate-50 p-5">
        <div className="space-y-1">
          <p className="text-sm text-slate-500">Email</p>
          <p className="text-base font-medium text-slate-900">{user.email}</p>
        </div>
        <div className="space-y-1">
          <p className="text-sm text-slate-500">Тип авторизации</p>
          <p className="text-base font-medium text-slate-900">
            {authType === "token" ? "JWT Access Token" : "Cookie"}
          </p>
        </div>
        <div className="space-y-1">
          <p className="text-sm text-slate-500">Роли</p>
          <p className="text-base font-medium text-slate-900">
            {roles.length > 0 ? roles.join(", ") : "Нет ролей в текущей сессии"}
          </p>
        </div>
      </div>

      <div className="flex justify-end">
        <LogoutButton onSuccess={onLogout} />
      </div>
    </section>
  );
}
