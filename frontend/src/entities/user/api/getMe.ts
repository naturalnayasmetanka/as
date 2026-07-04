import { api } from "@/shared/api/client";
import type { User } from "../model/types";

export function getMe() {
  return api.get<User>("/auth/me");
}
