import { clearSession, setSessionAccessToken } from "@/shared/session";

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:7218";
const PROJECTS_API_BASE = process.env.NEXT_PUBLIC_PROJECTS_API_URL ?? "http://localhost:7220";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export type AuthType = "cookie" | "token" | null;

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
let refreshPromise: Promise<void> | null = null;

const RETRY_BLOCKLIST = new Set<string>([
  "/auth/jwt/refresh",
  "/auth/jwt/login",
  "/auth/login",
  "/auth/logout",
  "/register",
]);

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

    accessToken = result.accessToken;
    setSessionAccessToken(result.accessToken);
    authType = "token";
  })();

  try {
    await refreshPromise;
  } finally {
    refreshPromise = null;
  }
}

async function request<T>(path: string, options: RequestInit = {}, baseUrl = API_BASE): Promise<T> {
  const url = `${baseUrl}${path}`;
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
  setSessionAccessToken(token);
  authType = token ? "token" : authType;
}

export function setAuthType(type: AuthType) {
  authType = type;
}

export function clearAccessToken() {
  accessToken = null;
  clearSession();
  authType = null;
}

export function getAuthType(): AuthType {
  return authType;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "POST",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "PUT",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};

export const projectsApi = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }, PROJECTS_API_BASE),
  post: <T>(path: string, body?: unknown) =>
    request<T>(
      path,
      {
        method: "POST",
        body: body !== undefined ? JSON.stringify(body) : undefined,
      },
      PROJECTS_API_BASE,
    ),
  put: <T>(path: string, body?: unknown) =>
    request<T>(
      path,
      {
        method: "PUT",
        body: body !== undefined ? JSON.stringify(body) : undefined,
      },
      PROJECTS_API_BASE,
    ),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }, PROJECTS_API_BASE),
};
