// Home page script: loads, filters, sorts, and deletes entries.

import { api } from "./api.js"
import {
  uiTypeToEnum, uiSubTypeToEnum, uiStatusToEnum,
  enumTypeToUI, enumSubTypeToUI, enumStatusToUI,
  enumStringToLabel
} from "./enums.js";

const BASE = "/api/mediaentries";

// Builds a query string from an object (skips null/empty)
function toQuery(params) {
    const q = new URLSearchParams();
    Object.entries(params || {}).forEach(([k, v]) => {
        if (v === undefined || v === null || v === "") return;
        q.set(k, String(v));
    });
    const s = q.toString();
    return s ? `?${s}` : "";
}

// List entries with filters and paging
async function listEntries(query) {
    return api.get(`${BASE}${toQuery(query)}`);
}

// delete a single entry by id
async function deleteEntry(id) {
    return api.del(`${BASE}/${encodeURIComponent(id)}`);
}

// ----- DOM references -----
const entriesContainer = document.getElementById("entriesContainer");
const filterType = document.getElementById("filterType");
const filterSubType = document.getElementById("filterSubType");
const filterGenre = document.getElementById("filterGenre");
const sortBy = document.getElementById("sortBy");
const darkToggle = document.getElementById("darkModeToggle");

// ------------------
// Utilities
// ------------------

// Basic HTML escape to guard against XSS when rendering user-entered fields.

function escapeHTML(s) {
    return String(s ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

// Build "tag badges" row from string[]
function renderTags(tags) {
    if (!tags || !tags.length) return `<span class="tag-badge muted"> No tags</span>`;
    return tags.map(t => `<span class="tag-badge">${escapeHTML(t)}</span>`).join(" ");
}

// Build HTML for one list item (MediaEntryDto)
function formatEntryHTML(dto) {
    // Defensive: escape user-provided fields to avoid XSS
    const safeTitle = escapeHTML(dto.title);
    const safeNotes = dto.notes ? escapeHTML(dto.notes) : "";

    const rating = dto.rating ?? "N/A";
    const statusLabel = enumStringToLabel(dto.status);     
    const typeLabel = enumStringToLabel(dto.type);
    const subTypeLabel = enumStringToLabel(dto.subType);
    const tagsHTML = renderTags(dto.tags);

    // Buttons: edit/delete
    // NOTE: edit uses entry.html?mode=edit&id=<id>
    return `
        <div class="entry-row">
            <div class="entry-main">
                <strong>${safeTitle}</strong>
                <span class="entry-meta">(${escapeHTML(typeLabel)}${subTypeLabel ? ` - ${escapeHTML(subTypeLabel)}` : ""}, ${escapeHTML(statusLabel)})</span>
            </div>
            <div class="entry-sub">
                Genres: ${tagsHTML} &nbsp;-&nbsp; Rating: ${escapeHTML(rating)}
            </div>
            ${dto.notes ? `<div class="entry-notes"><small>${safeNotes}</small></div>` : ""}
            <div class="entry-actions">
                <button type="button" class="btn" data-action="edit" data-id="${dto.id}">Edit</button>
                <button type="button" class="btn btn-danger" data-action="delete" data-id="${dto.id}">Delete</button>
            </div>    
        </div>
  `;
}

        


// ------------------
// Data loading
// ------------------

// Load entries using current filter controls
async function loadEntries() {
    // Map UI -> enum strings as the API expects them
    const q = {
        type: filterType.value ? uiTypeToEnum(filterType.value) : "",
        subType: filterSubType.value ? uiSubTypeToEnum(filterSubType.value) : "",
        tag: filterGenre.value || "",
        sort: normalizeSort(sortBy.value),
        dir: sortDirFor(sortBy.value),
        page: 1,
        pageSize: 50
    };

    // Call the API
    const result = await listEntries(q);

    // Render
    entriesContainer.innerHTML = (result.items || [])
        .map(formatEntryHTML)
        .join("") || `<div class="muted">No entries yet - try adding one!</div>`;
}

// Normalize the sort field used by the server
function normalizeSort(v) {
    const s = (v || "").toLowerCase();
    if (s === "title") return "title";
    if (s === "rating") return "rating";
    if (s === "status") return "status";
    return "updated";           // server default
}

// For this UI "Title" sorts ascending, "Rating" and "Updated" sort descending by default
function sortDirFor(v) {
    const s = (v || "").toLowerCase();
    if (s === "title" || s === "status") return "asc";
    return "desc";
}

// ------------------
// Event wiring
// ------------------

[filterType, filterSubType, filterGenre, sortBy].forEach(el => {
    el?.addEventListener("change", () => {
        loadEntries().catch(err => {
        console.error(err);
        entriesContainer.innerHTML =
            `<div class="error">Failed to load: ${escapeHTML(err?.message || err)}</div>`;
        });
    });
});

entriesContainer.addEventListener("click", async (e) => {
    // Delegate button clicks for Edit/Delete using data-action
    const btn = e.target.closest("[data-action]");
    if (!btn) return;

    const action = btn.getAttribute("data-action");
    const id = btn.getAttribute("data-id");
    if (!id) return;

    if (action === "edit") {
        // Navigate to edit page preserving the id
        window.location.href = `entry.html?mode=edit&id=${encodeURIComponent(id)}`;
        return;
    }

    if (action === "delete") {
        if (!confirm("Delete this entry?")) return;
        try {
        await deleteEntry(id);
        // Remove the rendered row
        const row = btn.closest(".entry-row");
        row?.remove();
        // Reload if list is now empty 
        if (!entriesContainer.children.length) {
            await loadEntries();
        }
        } catch (err) {
        alert(`Delete failed: ${err?.message || String(err)}`);
        }
    }
});


const prefersDark = localStorage.getItem("darkMode") === "true";

// Apply saved mode
if (prefersDark) {
    document.body.classList.add("dark-mode");
    if (darkToggle) darkToggle.checked = true;
}

// Listen for toggle changes
if (darkToggle) {
    darkToggle.addEventListener("change", () => {
        const enabled = darkToggle.checked;
        document.body.classList.toggle("dark-mode", enabled);
        localStorage.setItem("darkMode", enabled);
    });
}

// ---------------------
// Boot
// ---------------------

loadEntries().catch(err => {
    console.error(err);
    entriesContainer.innerHTML = `<div class="error">Failed to load: ${escapeHTML(err?.message || err)}</div>`;
})