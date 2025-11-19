/* Lightweight fetch-based API helper to avoid adding axios as a dependency.
     Exposes helper methods: get, post, put, del and a generic request.
     Attaches JWT from localStorage and dispatches a global "unauthorized"
     event on 401 responses so the AuthProvider can react.
*/
// NOTE: this module is NOT a React hook. It is a plain API client utility.
// If you want to avoid confusion, consider renaming the file to `api.ts`.

import { ApiErrorObject as ApiError } from "@/types/api/error";

async function request<T = any>(
    path: string,
    options: { method?: string; body?: any; headers?: Record<string, string> } = {}
): Promise<T> {
    const headers: Record<string, string> = {
        "Content-Type": "application/json",
        ...(options.headers || {}),
    };

    try {
        const token = typeof window !== "undefined" ? localStorage.getItem("token") : null;
        if (token) headers["Authorization"] = `Bearer ${token}`;
    } catch (e) {
        // ignore storage errors
    }

    const fetchOptions: RequestInit = {
        method: options.method || "GET",
        headers,
    };
    if (options.body !== undefined) {
        fetchOptions.body = JSON.stringify(options.body);
    }

    const res = await fetch(path, fetchOptions);
    const contentType = res.headers.get("content-type") || "";

    let data: any = null;
    if (contentType.includes("application/json")) {
        data = await res.json();
    } else {
        data = await res.text();
    }

    if (res.status === 401) {
        try {
            window.dispatchEvent(new Event("unauthorized"));
        } catch (e) {
            // ignore
        }
        throw new ApiError("Unauthorized", 401, data);
    }

    if (!res.ok) {
        throw new ApiError(data?.message || res.statusText || "API request failed", res.status, data);
    }

    return data as T;
}

export async function get<T = any>(path: string) {
    return request<T>(path, { method: "GET" });
}

export async function post<T = any>(path: string, body?: any) {
    return request<T>(path, { method: "POST", body });
}

export async function put<T = any>(path: string, body?: any) {
    return request<T>(path, { method: "PUT", body });
}

export async function del<T = any>(path: string) {
    return request<T>(path, { method: "DELETE" });
}

export default { request, get, post, put, del };
