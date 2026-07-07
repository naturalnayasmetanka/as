export type Role = "User" | "Employee" | "Moderator" | "Admin" | string;

export interface SessionUser {
  roles: Role[];
}
