import type { User } from "@/entities/user";
import { LogoutButton } from "@/features/auth/logout";

export function ProfileCard({ user }: { user: User }) {
  return (
    <div>
      <h1>Профиль</h1>
      <p>Email: {user.email}</p>
      <LogoutButton />
    </div>
  );
}
