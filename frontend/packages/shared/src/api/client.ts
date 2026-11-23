import { emit } from "../events";
import type { ProblemDetails } from "../types";

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly fieldErrors: Record<string, string[]>;
  readonly problem: ProblemDetails | undefined;

  constructor(status: number, code: string, message: string, problem?: ProblemDetails) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fieldErrors = problem?.errors ?? {};
    this.problem = problem;
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  const message = error instanceof Error ? error.message : "Something went wrong.";
  return new ApiError(0, "unknown_error", message);
}

export function describeFieldErrors(error: ApiError): string[] {
  return Object.entries(error.fieldErrors).flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`));
}

export type ApiRequest = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  query?: Record<string, string | undefined>;
  signal?: AbortSignal;
  suppressSessionExpired?: boolean;
};

function withQuery(path: string, query: ApiRequest["query"]): string {
  if (!query) return path;
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value) params.set(key, value);
  }
  const text = params.toString();
  return text ? `${path}?${text}` : path;
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("json")) return undefined;
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return undefined;
  }
}

function defaultCode(status: number): string {
  if (status === 401) return "unauthorized";
  if (status === 403) return "forbidden";
  if (status === 404) return "not_found";
  if (status >= 500) return "server_error";
  return "request_failed";
}

export async function apiFetch<T>(path: string, request: ApiRequest = {}): Promise<T> {
  const method = request.method ?? "GET";
  const headers: Record<string, string> = { Accept: "application/json" };
  if (method !== "GET") headers["X-CSRF"] = "1";
  if (request.body !== undefined) headers["Content-Type"] = "application/json";

  let response: Response;
  try {
    response = await fetch(withQuery(path, request.query), {
      method,
      headers,
      credentials: "same-origin",
      body: request.body === undefined ? undefined : JSON.stringify(request.body),
      signal: request.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw new ApiError(0, "network_error", "Could not reach the server. Check your connection and try again.");
  }

  if (response.ok) {
    if (response.status === 204) return undefined as T;
    return (await response.json()) as T;
  }

  const problem = await readProblem(response);
  const unavailable = response.status >= 500 && !problem;
  const message = unavailable
    ? "The service is temporarily unavailable. Please try again shortly."
    : (problem?.detail ?? problem?.title ?? "The request could not be completed.");
  const error = new ApiError(response.status, problem?.code ?? defaultCode(response.status), message, problem);

  if (response.status === 401 && !request.suppressSessionExpired) {
    emit("november:session-expired");
  }
  throw error;
}
