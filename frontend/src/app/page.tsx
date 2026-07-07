"use client";

import { useEffect, useState } from "react";
import { getMe } from "@/entities/user";
import type { User } from "@/entities/user";
import { LoginForm } from "@/features/auth/login";
import { RegisterForm } from "@/features/auth/register";
import { ProfileCard } from "@/widgets/profile-card";
import { getAuthType, clearAccessToken } from "@/shared/api/client";

type Mode = "login" | "register";

type AuthType = "cookie" | "token" | null;

export default function HomePage() {
  const [user, setUser] = useState<User | null>(null);
  const [checked, setChecked] = useState(false);
  const [mode, setMode] = useState<Mode>("login");
  const [authType, setAuthType] = useState<AuthType>(null);

  const fetchCurrentUser = async () => {
    const data = await getMe();
    setUser(data);
    setAuthType(getAuthType() ?? "cookie");
  };

  useEffect(() => {
    let cancelled = false;

    getMe()
      .then((data) => {
        if (cancelled) return;
        setUser(data);
        setAuthType(getAuthType() ?? "cookie");
      })
      .catch(() => {
        if (cancelled) return;
        setUser(null);
        setAuthType(null);
      })
      .finally(() => {
        if (!cancelled) setChecked(true);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const handleLoginSuccess = async () => {
    await fetchCurrentUser();
  };

  const handleLogout = () => {
    setUser(null);
    setAuthType(null);
    clearAccessToken();
  };

  if (!checked) {
    return (
      <main className="flex min-h-[calc(100vh-4rem)] items-center justify-center px-4 py-10">
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <p className="text-sm text-slate-500">Загрузка...</p>
        </div>
      </main>
    );
  }

  if (user) {
    return (
      <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
        <ProfileCard user={user} authType={authType ?? "cookie"} onLogout={handleLogout} />
      </main>
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
      <div className="mx-auto grid w-full max-w-5xl gap-8 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm md:grid-cols-[0.9fr_1.1fr] md:p-8">
        <section className="flex flex-col justify-between gap-8 rounded-xl bg-slate-950 p-6 text-white">
          <div className="space-y-3">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-300">Добро пожаловать</p>
            <h1 className="text-3xl font-semibold tracking-tight md:text-4xl">
              Войдите или зарегистрируйтесь, чтобы продолжить.
            </h1>
            <p className="max-w-xl text-sm leading-6 text-slate-300">
              После успешной авторизации вы останетесь в системе при перезагрузке страницы. Токен автоматически обновляется при истечении срока действия.
            </p>
          </div>

          <div className="grid grid-cols-2 gap-2 rounded-xl bg-white/10 p-1">
            <button
              type="button"
              onClick={() => setMode("login")}
              className={`rounded-lg px-4 py-2.5 text-sm font-semibold transition ${
                mode === "login"
                  ? "bg-white text-slate-950"
                  : "text-slate-200 hover:bg-white/10"
              }`}
            >
              Вход
            </button>
            <button
              type="button"
              onClick={() => setMode("register")}
              className={`rounded-lg px-4 py-2.5 text-sm font-semibold transition ${
                mode === "register"
                  ? "bg-white text-slate-950"
                  : "text-slate-200 hover:bg-white/10"
              }`}
            >
              Регистрация
            </button>
          </div>
        </section>

        <section className="lg:w-1/2">
          {mode === "login" ? (
            <LoginForm onSuccess={handleLoginSuccess} />
          ) : (
            <RegisterForm onSuccess={() => setMode("login")} />
          )}
        </section>
      </div>
    </main>
  );
}
