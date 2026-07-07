"use client";

import { create } from "zustand";
import type { SessionUser } from "./types";

type JwtPayload = Record<string, unknown>;

interface SessionState {
  user: SessionUser | null;
  setAccessToken: (token: string | null) => void;
  clearSession: () => void;
  hasAnyRole: (roles: string[]) => boolean;
}

const ROLE_CLAIMS = [
  "role",
  "roles",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
];

function decodeBase64Url(value: string): string {
  const base64 = value.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));

  return new TextDecoder().decode(bytes);
}

function normalizeRoles(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.flatMap(normalizeRoles);
  }

  if (typeof value !== "string") {
    return [];
  }

  const trimmed = value.trim();
  if (!trimmed) {
    return [];
  }

  if (trimmed.startsWith("[") && trimmed.endsWith("]")) {
    try {
      return normalizeRoles(JSON.parse(trimmed));
    } catch {
      return [trimmed];
    }
  }

  return trimmed
    .split(",")
    .map((role) => role.trim())
    .filter(Boolean);
}

function decodeSessionUser(token: string): SessionUser {
  const [, payload] = token.split(".");

  if (!payload) {
    return { roles: [] };
  }

  try {
    const parsed = JSON.parse(decodeBase64Url(payload)) as JwtPayload;
    const roles = ROLE_CLAIMS.flatMap((claim) => normalizeRoles(parsed[claim]));

    return { roles: Array.from(new Set(roles)) };
  } catch {
    return { roles: [] };
  }
}

export const useSessionStore = create<SessionState>((set, get) => ({
  user: null,
  setAccessToken: (token) => set({ user: token ? decodeSessionUser(token) : null }),
  clearSession: () => set({ user: null }),
  hasAnyRole: (roles) => {
    const userRoles = (get().user?.roles ?? []).map((role) => role.toLowerCase());

    return roles.some((role) => userRoles.includes(role.toLowerCase()));
  },
}));

export function getSessionUser() {
  return useSessionStore.getState().user;
}

export function setSessionAccessToken(token: string | null) {
  useSessionStore.getState().setAccessToken(token);
}

export function clearSession() {
  useSessionStore.getState().clearSession();
}
