"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { loginWithCookie, loginWithToken, type LoginAuthMode } from "../api/login";
import { ApiError, clearAccessToken, setAccessToken, setAuthType } from "@/shared/api/client";

interface LoginFormProps {
  onSuccess?: () => void;
}

export function LoginForm({ onSuccess }: LoginFormProps = {}) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authMode, setAuthMode] = useState<LoginAuthMode>("cookie");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      if (authMode === "token") {
        const result = await loginWithToken(email, password);
        setAccessToken(result.accessToken);
      } else {
        clearAccessToken();
        await loginWithCookie(email, password);
        setAuthType("cookie");
      }

      if (onSuccess) {
        onSuccess();
      } else {
        router.push("/profile");
      }
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Неверный email или пароль",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-6 rounded-3xl border border-slate-200 bg-slate-50 p-6 shadow-sm"
    >
      <div>
        <h1 className="text-2xl font-semibold text-slate-900">Вход</h1>
        <p className="mt-2 text-sm text-slate-600">
          Выберите способ авторизации и введите email с паролем.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-2 rounded-2xl bg-white p-1 ring-1 ring-slate-200">
        <button
          type="button"
          onClick={() => setAuthMode("cookie")}
          className={`h-10 rounded-xl text-sm font-semibold transition ${
            authMode === "cookie"
              ? "bg-slate-950 text-white"
              : "text-slate-600 hover:bg-slate-100"
          }`}
        >
          Cookie
        </button>
        <button
          type="button"
          onClick={() => setAuthMode("token")}
          className={`h-10 rounded-xl text-sm font-semibold transition ${
            authMode === "token"
              ? "bg-slate-950 text-white"
              : "text-slate-600 hover:bg-slate-100"
          }`}
        >
          JWT Token
        </button>
      </div>

      <label className="block space-y-2 text-sm text-slate-700">
        <span>Email</span>
        <input
          className="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
          required
        />
      </label>

      <label className="block space-y-2 text-sm text-slate-700">
        <span>Пароль</span>
        <input
          className="w-full rounded-2xl border border-slate-300 bg-white px-4 py-3 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
        />
      </label>

      {error && (
        <p role="alert" className="rounded-2xl bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {error}
        </p>
      )}

      <button
        type="submit"
        disabled={loading}
        className="inline-flex h-12 w-full items-center justify-center rounded-2xl bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {loading ? "Вход..." : "Войти"}
      </button>
    </form>
  );
}
