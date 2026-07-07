"use client";

import { useEffect } from "react";
import Link from "next/link";
import { useSessionStore } from "@/shared/session";
import { refreshAccessToken } from "@/shared/api/client";

const roleLinks = [
  { href: "/staff", label: "Сотрудники", roles: ["Employee", "Moderator", "Admin"] },
  { href: "/moderation", label: "Модерация", roles: ["Moderator", "Admin"] },
  { href: "/admin", label: "Админка", roles: ["Admin"] },
];

export function AppHeader() {
  const user = useSessionStore((state) => state.user);
  const hasAnyRole = useSessionStore((state) => state.hasAnyRole);
  const visibleLinks = user ? roleLinks.filter((link) => hasAnyRole(link.roles)) : [];

  useEffect(() => {
    if (user) {
      return;
    }

    refreshAccessToken().catch(() => {
      // Anonymous and cookie-only sessions do not have frontend-visible roles.
    });
  }, [user]);

  return (
    <header className="sticky top-0 z-10 border-b border-slate-200 bg-white">
      <nav className="mx-auto flex min-h-16 w-full max-w-6xl flex-col gap-3 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:py-0">
        <Link href="/" className="text-base font-semibold text-slate-950">
          Auth
        </Link>

        <div className="flex flex-wrap items-center gap-1.5">
          {visibleLinks.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className="inline-flex h-9 items-center rounded-md px-3 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-slate-950"
            >
              {link.label}
            </Link>
          ))}
          <Link
            href="/profile"
            className="inline-flex h-9 items-center rounded-md px-3 text-sm font-medium text-slate-700 transition hover:bg-slate-100 hover:text-slate-950"
          >
            Профиль
          </Link>
        </div>
      </nav>
    </header>
  );
}
