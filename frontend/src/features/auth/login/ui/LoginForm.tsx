"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { login } from "../api/login";
import { ApiError } from "@/shared/api/client";

interface LoginFormProps {
  onSuccess?: () => void;
}

export function LoginForm({ onSuccess }: LoginFormProps = {}) {
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
      await login(email, password);
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
    <form onSubmit={handleSubmit}>
      <h1>Вход</h1>
      <label>
        Email
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
          required
        />
      </label>
      <label>
        Пароль
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
        />
      </label>
      {error && <p role="alert">{error}</p>}
      <button type="submit" disabled={loading}>
        {loading ? "..." : "Войти"}
      </button>
    </form>
  );
}
