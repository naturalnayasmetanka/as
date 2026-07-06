const API_BASE = "http://localhost:7218";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export type AuthType = "cookie" | "token" | null;

export interface SessionUser {
  id: string;
  email: string;
  roles: string[];
}

type ApiErrorMessage = {
  message?: string;
};

type ApiEnvelope<T> = {
  result: T;
  error?: {
    messages?: ApiErrorMessage[];
  } | null;
  isError?: boolean;
};

let accessToken: string | null = null;
let authType: AuthType = null;
let sessionUser: SessionUser | null = null;
let refreshPromise: Promise<void> | null = null;

const RETRY_BLOCKLIST = new Set<string>([
  "/auth/jwt/refresh",
  "/auth/jwt/login",
  "/auth/login",
  "/auth/logout",
  "/register",
]);

function decodeBase64Url(value: string): string {
  const base64 = value.replace(/-/g, "+").replace(/_/g, "/");
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
  return atob(padded);
}

function readStringArrayClaim(payload: Record<string, unknown>, name: string): string[] {
  const value = payload[name];
  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === "string");
  }

  return typeof value === "string" ? [value] : [];
}

function decodeSessionUser(token: string): SessionUser | null {
  try {
    const [, payloadPart] = token.split(".");
    if (!payloadPart) return null;

    const payload = JSON.parse(decodeBase64Url(payloadPart)) as Record<string, unknown>;
    const id = typeof payload.sub === "string" ? payload.sub : "";
    const email =
      typeof payload.email === "string"
        ? payload.email
        : typeof payload.name === "string"
          ? payload.name
          : "";
    const roles = readStringArrayClaim(payload, "role");

    if (!id || !email) return null;

    return { id, email, roles };
  } catch {
    return null;
  }
}

function getHeaders(options: RequestInit = {}): Record<string, string> {
  return {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
    ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
  };
}

function createAuthorizedRequest(url: string, options: RequestInit = {}): Request {
  return new Request(url, {
    ...options,
    credentials: "include",
    headers: getHeaders(options),
  });
}

async function createApiError(response: Response): Promise<ApiError> {
  let message = response.statusText;

  try {
    const body = await response.json();
    if (body && typeof body === "object") {
      if ("message" in body && typeof body.message === "string") {
        message = body.message;
      }

      if ("error" in body) {
        const envelope = body as ApiEnvelope<unknown>;
        const firstMessage = envelope.error?.messages?.[0]?.message;
        if (firstMessage) {
          message = firstMessage;
        }
      }
    }
  } catch {
    // ignore non-JSON error payloads
  }

  return new ApiError(response.status, message);
}

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  if (!text) {
    return undefined as T;
  }

  const body = JSON.parse(text);

  if (body && typeof body === "object" && "result" in body) {
    const envelope = body as ApiEnvelope<T>;

    if (envelope.isError) {
      throw new ApiError(
        response.status,
        envelope.error?.messages?.[0]?.message ?? response.statusText,
      );
    }

    return envelope.result;
  }

  return body as T;
}

export async function refreshAccessToken(): Promise<void> {
  if (refreshPromise) {
    return refreshPromise;
  }

  refreshPromise = (async () => {
    const response = await fetch(`${API_BASE}/auth/jwt/refresh`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (!response.ok) {
      clearAccessToken();
      throw await createApiError(response);
    }

    const result = await parseResponse<{ accessToken: string; expiresAt: string }>(response);
    if (!result?.accessToken) {
      clearAccessToken();
      throw new ApiError(response.status, "Refresh failed");
    }

    setAccessToken(result.accessToken);
  })();

  try {
    await refreshPromise;
  } finally {
    refreshPromise = null;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const url = `${API_BASE}${path}`;
  const initialRequest = createAuthorizedRequest(url, options);
  const retryRequest = initialRequest.clone();

  let response = await fetch(initialRequest);

  if (response.status === 401 && !RETRY_BLOCKLIST.has(path)) {
    try {
      await refreshAccessToken();

      response = await fetch(
        new Request(retryRequest, {
          credentials: "include",
          headers: getHeaders(options),
        }),
      );
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }
      throw new ApiError(401, "Unauthorized");
    }
  }

  if (!response.ok) {
    throw await createApiError(response);
  }

  return parseResponse<T>(response);
}

export function setAccessToken(token: string) {
  accessToken = token;
  authType = token ? "token" : authType;
  sessionUser = token ? decodeSessionUser(token) : null;
}

export function setSessionUser(user: SessionUser | null) {
  sessionUser = user;
}

export function setAuthType(type: AuthType) {
  authType = type;
}

export function clearAccessToken() {
  accessToken = null;
  authType = null;
  sessionUser = null;
}

export function getAuthType(): AuthType {
  return authType;
}

export function getSessionUser(): SessionUser | null {
  return sessionUser;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "POST",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};
