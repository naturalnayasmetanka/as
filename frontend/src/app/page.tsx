"use client";

import { useEffect, useState } from "react";
import { getMe } from "@/entities/user";
import type { User } from "@/entities/user";
import { LoginForm } from "@/features/auth/login";
import { RegisterForm } from "@/features/auth/register";
import { ProfileCard } from "@/widgets/profile-card";
import { getAuthType, clearAccessToken, setSessionUser } from "@/shared/api/client";

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
    setSessionUser(data);
    setAuthType(getAuthType() ?? "cookie");
  };

  useEffect(() => {
    let cancelled = false;

    getMe()
      .then((data) => {
        if (cancelled) return;
        setUser(data);
        setSessionUser(data);
        setAuthType(getAuthType() ?? "cookie");
      })
      .catch(() => {
        if (cancelled) return;
        setUser(null);
        setSessionUser(null);
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
    setSessionUser(null);
    setAuthType(null);
    clearAccessToken();
  };

  if (!checked) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4">
        <div className="rounded-3xl bg-white p-8 shadow-lg">
          <p className="text-sm text-slate-500">Загрузка...</p>
        </div>
      </div>
    );
  }

  if (user) {
    return <ProfileCard user={user} authType={authType ?? "cookie"} onLogout={handleLogout} />;
  }

  return (
    <main className="min-h-screen bg-slate-100 px-4 py-12">
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-10 rounded-[2rem] border border-slate-200 bg-white p-8 shadow-lg lg:flex-row lg:items-start lg:p-10">
        <section className="space-y-6 lg:w-1/2">
          <div className="space-y-3">
            <p className="text-sm uppercase tracking-[0.3em] text-slate-500">Добро пожаловать</p>
            <h1 className="text-4xl font-semibold tracking-tight text-slate-950">
              Войдите или зарегистрируйтесь, чтобы продолжить.
            </h1>
            <p className="max-w-xl text-base leading-7 text-slate-600">
              После успешной авторизации вы останетесь в системе при перезагрузке страницы. Токен автоматически обновляется при истечении срока действия.
            </p>
          </div>

          <div className="flex gap-3 rounded-3xl bg-slate-50 p-5">
            <button
              type="button"
              onClick={() => setMode("login")}
              className={`flex-1 rounded-2xl px-5 py-3 text-sm font-semibold transition ${
                mode === "login"
                  ? "bg-slate-950 text-white"
                  : "bg-white text-slate-700 ring-1 ring-slate-200"
              }`}
            >
              Вход
            </button>
            <button
              type="button"
              onClick={() => setMode("register")}
              className={`flex-1 rounded-2xl px-5 py-3 text-sm font-semibold transition ${
                mode === "register"
                  ? "bg-slate-950 text-white"
                  : "bg-white text-slate-700 ring-1 ring-slate-200"
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
