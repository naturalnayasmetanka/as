"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { getSessionUser, refreshAccessToken } from "@/shared/api/client";

interface RoleGateProps {
  roles: string[];
  children: React.ReactNode;
}

type GateState = "loading" | "allowed" | "forbidden";

export function RoleGate({ roles, children }: RoleGateProps) {
  const router = useRouter();
  const [state, setState] = useState<GateState>("loading");
  const rolesKey = roles.map((role) => role.toLowerCase()).join("|");

  useEffect(() => {
    let cancelled = false;

    async function checkAccess() {
      try {
        if (!getSessionUser()) {
          await refreshAccessToken();
        }
      } catch {
        if (!cancelled) router.replace("/login");
        return;
      }

      const user = getSessionUser();
      const requiredRoles = rolesKey.split("|");
      const allowed =
        user?.roles.some((role) =>
          requiredRoles.includes(role.toLowerCase()),
        ) ?? false;

      if (!cancelled) setState(allowed ? "allowed" : "forbidden");
    }

    checkAccess();

    return () => {
      cancelled = true;
    };
  }, [rolesKey, router]);

  if (state === "loading") {
    return <main className="mx-auto w-full max-w-6xl px-4 py-8 text-sm text-slate-500">Загрузка...</main>;
  }

  if (state === "forbidden") {
    return (
      <main className="mx-auto w-full max-w-6xl px-4 py-8">
        <div className="border border-rose-200 bg-rose-50 p-6 text-rose-800">
          <h1 className="text-xl font-semibold">403</h1>
          <p className="mt-2 text-sm">У вашей роли нет доступа к этому разделу.</p>
        </div>
      </main>
    );
  }

  return children;
}
