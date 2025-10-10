// Small, focused wrapper around fetch with basic error handling.

const API_BASE = window.APP_CONFIG?.apiBaseUrl ?? "https://localhost:7106"; // <-- set in index.html

// Read the current token (kept here so only one file touches localStorage)
function getAuthToken() {
    return localStorage.getItem("authToken");
}

// Generic fetch helper that throws on !ok for simpler calling code
export async function http(method, path, body) {
    const headers = { "Content-Type": "application/json" };

    // Attach the Bearer token if there is one
    const token = getAuthToken();
    if (token) headers.Authorization = `Bearer ${token}`;


    const res = await fetch(`${API_BASE}${path}`, {
        method,
        headers,
        body: body != null ? JSON.stringify(body) : undefined,
    });

    // If not authorized, send user to login page and include a return url
    if (res.status === 401) {
        // Avoid infinite redirect if we're already on the login page
        if (!/\/login\.html$/i.test(window.location.pathname)) {
            // Use full href (preserves query/hash)
            const here = window.location.href;
            window.location.href = `login.html?return=${encodeURIComponent(here)}`;
        }
        return;
    }

    // Fast path: 2xx
    if (res.ok) {
        if (res.status === 204) return null;
        const ct = (res.headers.get("content-type") || "").toLowerCase();
        return ct.includes("application/json") ? res.json() : res.text();
    }

    // Non-2xx error (other than 401 handled above)
    let msg = "";
    try { msg = await res.text(); } catch {}
    throw new Error(`HTTP ${res.status} ${res.statusText}${msg ? `: ${msg}` : "" }`);
}

// Convenience helpers
export const api = {
    get: (path) => http("GET", path),
    post: (path, body) => http("POST", path, body),
    put: (path, body) => http("PUT", path, body),
    del: (path) => http("DELETE", path),
};