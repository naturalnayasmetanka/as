import type { User } from "@/entities/user";
import { LogoutButton } from "@/features/auth/logout";

interface ProfileCardProps {
  user: User;
  authType?: "cookie" | "token";
  onLogout?: () => void;
}

export function ProfileCard({ user, authType = "cookie", onLogout }: ProfileCardProps) {
  return (
    <section className="mx-auto flex w-full max-w-xl flex-col gap-6 rounded-[2rem] border border-slate-200 bg-white p-8 shadow-lg">
      <div className="space-y-3">
        <p className="text-sm uppercase tracking-[0.25em] text-slate-500">Профиль</p>
        <h1 className="text-3xl font-semibold text-slate-950">Добро пожаловать</h1>
        <p className="text-sm leading-6 text-slate-600">
          Вы успешно вошли в систему. Ниже указаны данные пользователя и текущий тип авторизации.
        </p>
      </div>

      <div className="grid gap-4 rounded-3xl bg-slate-50 p-5">
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
      </div>

      <div className="flex justify-end">
        <LogoutButton onSuccess={onLogout} />
      </div>
    </section>
  );
}
