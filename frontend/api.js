// Small, focused wrapper around fetch with basic error handling.

const API_BASE = window.APP_CONFIG?.apiBaseUrl ?? "https://localhost:7143"; // <-- set in index.html

// Generic fetch helper that throws on !ok for simpler calling code
async function http(method, path, body) {
    const res = await fetch(`${API_BASE}${path}`, {
        method,
        headers: { "Content-Type": "application/json" },
        body: body ? JSON.stringify(body) : undefined,
    });
    if (!res.ok) {
        // Try to surface ProblemDetails (or plain text) clearly
        let detail = "";
        try { const p = await res.json(); detail = p?.detail || p?.title || JSON.stringify(p); }
        catch { detail = await res.text(); }
        throw new Error(`HTTP ${res.status} ${res.statusText}${detail ? `: ${detail}` : ""}`);
    }
    // 204 responses don't have a body
    if (res.status === 204) return null;
    return res.json();
}

// --- Public API used by the UI ---

// List entries with optional filters/sorting/paging
export async function listEntries({ q, type, tag, sort = "updated", dir = "desc", page = 1, pageSize = 50 } = {}) {
    // Build query string. Controller expects: q, type status (optional), tag, sort, dir, page, pageSize
    const qs = new URLSearchParams();
    if (q) qs.set("q", q);
    if (type) qs.set("type", type);
    if (tag) qs.set("tag", tag);
    qs.set("sort", sort);
    qs.set("dir", dir);
    qs.set("page", String(page));
    qs.set("pageSize", String(pageSize));
    return http("GET", `/api/mediaentries?${qs.toString()}`);
}

export async function getEntry(id) {
    return http("GET", `/api/mediaentries/${encodeURIComponent(id)}`);
}

export async function createEntry(payload) {
    // payload must match CreateMediaEntryDto
    return http("POST", `/api/mediaentries`, payload);
}

export async function updateEntry(id, payload) {
    // payload should be UpdateMediaEntryDto, includ id to satisfy controller's consistency check
    return http("PUT", `/api/mediaentries/${encodeURIComponent(id)}`, { id, ...payload });
}

export async function deleteEntry(id) {
    return http("DELETE", `/api/mediaentries/${encodeURIComponent(id)}`);
}