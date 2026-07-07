"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { register } from "../api/register";
import { ApiError } from "@/shared/api/client";

interface RegisterFormProps {
  onSuccess?: () => void;
}

export function RegisterForm({ onSuccess }: RegisterFormProps = {}) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await register(email, password);
      if (onSuccess) {
        onSuccess();
      } else {
        router.push("/login");
      }
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Не удалось зарегистрироваться",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-5 rounded-xl border border-slate-200 bg-white p-6"
    >
      <div>
        <h1 className="text-2xl font-semibold text-slate-950">Регистрация</h1>
        <p className="mt-2 text-sm text-slate-600">
          Зарегистрируйтесь для входа по email и паролю.
        </p>
      </div>

      <label className="block space-y-2 text-sm text-slate-700">
        <span>Email</span>
        <input
          className="w-full rounded-lg border border-slate-300 bg-white px-4 py-3 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
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
          className="w-full rounded-lg border border-slate-300 bg-white px-4 py-3 text-slate-900 outline-none transition focus:border-slate-500 focus:ring-2 focus:ring-slate-200"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="new-password"
          required
        />
      </label>

      {error && (
        <p role="alert" className="rounded-lg bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {error}
        </p>
      )}

      <button
        type="submit"
        disabled={loading}
        className="inline-flex h-12 w-full items-center justify-center rounded-lg bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {loading ? "Регистрация..." : "Зарегистрироваться"}
      </button>
    </form>
  );
}
