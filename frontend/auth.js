// Minimal client for Auth endpoints and token persistence

const API_BASE = window.APP_CONFIG?.apiBaseUrl ?? "https://localhost:7106";

// Persist or clear the JWT in localStorage in a single place
export function setAuthToken(token) {
    if (token) localStorage.setItem("authToken", token);
    else localStorage.removeItem("authToken");
}

// Create account and sign in
export async function register(email, password) {
    const r = await fetch(`${API_BASE}/api/auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password })
    });
    if (!r.ok) throw new Error(await r.text());
    const data = await r.json();
    setAuthToken(data.accessToken);     // store the JWT
    return data;
}

// Sign in
export async function login(email, password) {
    const r = await fetch(`${API_BASE}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password })
    });
    if (!r.ok) throw new Error(await r.text());
    const data = await r.json();
    setAuthToken(data.accessToken);
    return data;
}

// Sign out (client side only, token is stateless)
export function logout() {
    setAuthToken(null);
}

// Get current user (requires token)
export async function fetchMe() {
    const token = localStorage.getItem("authToken");
    if (!token) throw new Error("Not authenticated.");
    const r = await fetch(`${API_BASE}/api/auth/me`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    return r.json();
}