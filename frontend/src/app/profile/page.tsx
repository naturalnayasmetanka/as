"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getMe } from "@/entities/user";
import type { User } from "@/entities/user";
import { ProfileCard } from "@/widgets/profile-card";
import { getAuthType, setSessionUser } from "@/shared/api/client";

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
          setSessionUser(data);
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
    return <p>Загрузка...</p>;
  }

  return <ProfileCard user={user} authType={authType} />;
}
