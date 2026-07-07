"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, refreshAccessToken } from "@/shared/api/client";
import { useSessionStore } from "@/shared/session";
import { ForbiddenScreen } from "@/shared/ui/ForbiddenScreen";

interface RoleGateProps {
  allowedRoles: string[];
  children: React.ReactNode;
}

type GateState = "loading" | "allowed" | "forbidden";

export function RoleGate({ allowedRoles, children }: RoleGateProps) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const hasAnyRole = useSessionStore((state) => state.hasAnyRole);
  const [state, setState] = useState<GateState>("loading");

  useEffect(() => {
    let cancelled = false;

    async function checkAccess() {
      setState("loading");

      try {
        if (!user) {
          await refreshAccessToken();
        }

        if (cancelled) {
          return;
        }

        setState(hasAnyRole(allowedRoles) ? "allowed" : "forbidden");
      } catch (error) {
        if (cancelled) {
          return;
        }

        if (error instanceof ApiError && error.status === 403) {
          setState("forbidden");
          return;
        }

        router.replace("/login");
      }
    }

    checkAccess();

    return () => {
      cancelled = true;
    };
  }, [allowedRoles, hasAnyRole, router, user]);

  if (state === "loading") {
    return (
      <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
        <div className="mx-auto max-w-xl rounded-xl border border-slate-200 bg-white p-6 text-sm text-slate-600 shadow-sm">
          Загрузка...
        </div>
      </main>
    );
  }

  if (state === "forbidden") {
    return <ForbiddenScreen />;
  }

  return <>{children}</>;
}
