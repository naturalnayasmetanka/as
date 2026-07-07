"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { logoutWithCookie, logoutWithToken } from "../api/logout";
import { ApiError, clearAccessToken, getAuthType } from "@/shared/api/client";

interface LogoutButtonProps {
  onSuccess?: () => void;
}

export function LogoutButton({ onSuccess }: LogoutButtonProps = {}) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleLogout() {
    setLoading(true);
    setError(null);

    try {
      if (getAuthType() === "token") {
        await logoutWithToken();
      } else {
        await logoutWithCookie();
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Не удалось выполнить выход на сервере");
    } finally {
      clearAccessToken();
      setLoading(false);
      if (onSuccess) {
        onSuccess();
      } else {
        router.replace("/login");
      }
    }
  }

  return (
    <div className="flex flex-col items-end gap-2">
      {error && <p className="text-sm text-rose-600">{error}</p>}
      <button
        type="button"
        onClick={handleLogout}
        disabled={loading}
        className="inline-flex h-11 items-center justify-center rounded-lg bg-rose-600 px-4 text-sm font-semibold text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {loading ? "Выход..." : "Выйти"}
      </button>
    </div>
  );
}
