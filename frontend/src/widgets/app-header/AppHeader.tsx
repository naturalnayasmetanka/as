"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getMe, type User } from "@/entities/user";
import { getSessionUser, setSessionUser, type SessionUser } from "@/shared/api/client";

function hasRole(user: SessionUser | User | null, role: string): boolean {
  return user?.roles.some((userRole) => userRole.toLowerCase() === role.toLowerCase()) ?? false;
}

export function AppHeader() {
  const [user, setUser] = useState<SessionUser | User | null>(null);

  useEffect(() => {
    let cancelled = false;
    const existing = getSessionUser();

    if (existing) {
      setUser(existing);
      return;
    }

    getMe()
      .then((data) => {
        if (cancelled) return;
        setUser(data);
        setSessionUser(data);
      })
      .catch(() => {
        if (!cancelled) setUser(null);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <header className="border-b border-slate-200 bg-white">
      <nav className="mx-auto flex h-14 w-full max-w-6xl items-center justify-between px-4">
        <Link href="/" className="text-sm font-semibold text-slate-950">
          AuthService
        </Link>
        <div className="flex items-center gap-2 text-sm">
          <Link className="px-2 py-1 text-slate-700 hover:text-slate-950" href="/profile">
            Профиль
          </Link>
          {hasRole(user, "Employee") && (
            <Link className="px-2 py-1 text-slate-700 hover:text-slate-950" href="/staff">
              Staff
            </Link>
          )}
          {hasRole(user, "Moderator") && (
            <Link className="px-2 py-1 text-slate-700 hover:text-slate-950" href="/moderation">
              Moderation
            </Link>
          )}
          {hasRole(user, "Admin") && (
            <Link className="px-2 py-1 text-slate-700 hover:text-slate-950" href="/admin/users">
              Admin
            </Link>
          )}
        </div>
      </nav>
    </header>
  );
}
