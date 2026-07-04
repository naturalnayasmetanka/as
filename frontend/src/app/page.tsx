"use client";

import { useEffect, useState } from "react";
import { getMe } from "@/entities/user";
import type { User } from "@/entities/user";
import { LoginForm } from "@/features/auth/login";
import { RegisterForm } from "@/features/auth/register";
import { ProfileCard } from "@/widgets/profile-card";

type Mode = "login" | "register";

export default function HomePage() {
  const [user, setUser] = useState<User | null>(null);
  const [checked, setChecked] = useState(false);
  const [mode, setMode] = useState<Mode>("login");

  useEffect(() => {
    let cancelled = false;

    getMe()
      .then((data) => {
        if (!cancelled) setUser(data);
      })
      .catch(() => {
        // 401 здесь — ожидаемое состояние "гость", не ошибка и не повод редиректить
      })
      .finally(() => {
        if (!cancelled) setChecked(true);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  if (!checked) {
    return <p>Загрузка...</p>;
  }

  if (user) {
    return <ProfileCard user={user} />;
  }

  return (
    <div>
      <div role="tablist" aria-label="Переключение между входом и регистрацией">
        <button
          type="button"
          aria-pressed={mode === "login"}
          onClick={() => setMode("login")}
        >
          Вход
        </button>
        <button
          type="button"
          aria-pressed={mode === "register"}
          onClick={() => setMode("register")}
        >
          Регистрация
        </button>
      </div>

      {mode === "login" ? (
        <LoginForm />
      ) : (
        <RegisterForm onSuccess={() => setMode("login")} />
      )}
    </div>
  );
}
