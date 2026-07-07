"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getMe } from "@/entities/user";
import type { User } from "@/entities/user";
import { ProfileCard } from "@/widgets/profile-card";
import { getAuthType } from "@/shared/api/client";

type AuthType = "cookie" | "token";

export default function ProfilePage() {
  const router = useRouter();
  const [user, setUser] = useState<User | null>(null);
  const [authType, setAuthType] = useState<AuthType>("cookie");
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    let cancelled = false;

    getMe()
      .then((data) => {
        if (!cancelled) {
          setUser(data);
          setAuthType(getAuthType() ?? "cookie");
          setChecked(true);
        }
      })
      .catch(() => {
        if (!cancelled) router.replace("/login");
      });

    return () => {
      cancelled = true;
    };
  }, [router]);

  if (!checked || !user) {
    return (
      <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
        <div className="mx-auto max-w-xl rounded-xl border border-slate-200 bg-white p-6 text-sm text-slate-600 shadow-sm">
          Загрузка...
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
      <ProfileCard user={user} authType={authType} />
    </main>
  );
}
