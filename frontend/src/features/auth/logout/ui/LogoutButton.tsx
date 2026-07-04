"use client";

import { useRouter } from "next/navigation";
import { logout } from "../api/logout";

export function LogoutButton() {
  const router = useRouter();

  async function handleLogout() {
    await logout();
    router.replace("/login");
  }

  return <button onClick={handleLogout}>Выйти</button>;
}
