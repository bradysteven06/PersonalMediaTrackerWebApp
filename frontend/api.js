// Small, focused wrapper around fetch with basic error handling.

const API_BASE = window.APP_CONFIG?.apiBaseUrl ?? "https://localhost:7143"; // <-- set in index.html

// Generic fetch helper that throws on !ok for simpler calling code
async function http(method, path, body) {
    const res = await fetch(`${API_BASE}${path}`, {
        method,
        headers: { "Content-Type": "application/json" },
        body: body ? JSON.stringify(body) : undefined,
    });

    // Fast path: 2xx/3xx status codes
    if (res.ok) {
        if (res.status === 204) return null;
        const ct = res.headers.get("content-type") || "";
        if (ct.toLowerCase().includes("application/json")) {
        return res.json(); // safe: first and only read on success
        }
        return res.text(); // non-JSON success (unlikely for your API, but safe)
    }

    // Error path: read ONCE, then parse if possible
    let raw = "";
    try {
        raw = await res.text(); // first and only read
    } catch {
        // ignore—some environments may throw if body missing
    }

    // Try to extract a helpful detail from ProblemDetails JSON or similar
    let detail = "";
    if (raw) {
        try {
            const p = JSON.parse(raw);
            detail = p?.detail || p?.title || p?.message || raw;
        } catch {
            detail = raw; // not JSON; include the text body (e.g., HTML dev error page)
        }
    }

    throw new Error(`HTTP ${res.status} ${res.statusText}${detail ? `: ${detail}` : ""}`);
}

// --- Public API used by the UI ---

// List entries with optional filters/sorting/paging
export async function listEntries({ q, type, subType, tag, sort = "updated", dir = "desc", page = 1, pageSize = 50 } = {}) {
    // Build query string. Controller expects: q, type status (optional), tag, sort, dir, page, pageSize
    const qs = new URLSearchParams();
    if (q) qs.set("q", q);
    if (type) qs.set("type", type);
    if (subType) qs.set("subType", subType);
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