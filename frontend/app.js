import {
  uiTypeToEnum, uiSubTypeToEnum, uiStatusToEnum,
  enumTypeToUI, enumSubTypeToUI, enumStatusToUI
} from "./enums.js";
import { listEntries, deleteEntry } from "./api.js"

// ----- DOM references -----
const entriesContainer = document.getElementById("entriesContainer");
const filterTypeEl = document.getElementById("filterType");
const filterSubTypeEl = document.getElementById("filterSubType");
const filterGenreEl = document.getElementById("filterGenre");
const sortByEl = document.getElementById("sortBy");
const darkToggle = document.getElementById("darkModeToggle");

// ------------------
// Helper utilities
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

// Build query params for the API from UI controls.
function buildQueryFromUI() {
    const typeFilter = filterTypeEl?.value || "";
    const subTypeFilter = filterSubTypeEl?.value || "";
    const genreFilter = filterGenreEl?.value || "";
    const sortBy = (sortByEl?.value || "").toLowerCase();

    // Map UI sort to API fields, API supports: title, created, updated, rating
    const sort = sortBy === "title" ? "title" : sortBy === "rating" ? "rating" : "updated";
    const dir = sortBy === "title" ? "asc" : "desc";

    return {
        type: uiTypeToEnum(typeFilter),
        subType: uiSubTypeToEnum(subTypeFilter),
        tag: genreFilter || undefined,
        sort, dir,
        page: 1, pageSize: 100
    };
}

// Build HTML for one entry returned by the API (MediaEntryDto)
function formatEntryHTML(dto) {
    const title = escapeHTML(dto.title);
    const type = escapeHTML(dto.type);              // EntryType string string
    const subType = escapeHTML(dto.subType ?? "");  // EntrySubType string, may be null
    const status = escapeHTML(dto.status);          // EntryStatus string
    const rating = dto.rating ?? "N/A";
    const notes = escapeHTML(dto.notes || "");

    const tags = Array.isArray(dto.tags) ? dto.tags : [];
    const tagsHTML = tags.length
        ? tags.map((g) => `<span class="genre-badge">${escapeHTML(g)}</span>`).join(" ")
        : "N/A";

    return `
        <div class="entry-row">
            <div class="entry-main">
                <strong>${title}</strong>
                <span class="entry-meta">(${type}${subType ? ` - ${subType}` : ""}, ${status})</span>
            </div>
            <div class="entry-sub">
                Genres: ${tagsHTML} &nbsp;-&nbsp; Rating: ${escapeHTML(rating)}
            </div>
            ${notes ? `<div class="entry-notes"><small>${notes}</small></div>` : ""}
            <div class="entry-actions">
                <button type="button" class="btn" data-action="edit" data-id="${dto.id}">Edit</button>
                <button type="button" class="btn btn-danger" data-action="delete" data-id="${dto.id}">Delete</button>
            </div>    
        </div>
    `;
}

/*
 * Render the entire list according to current filters/sorts.
 * Uses event delegation for action buttons via data-* attributes.
*/
async function renderEntries() {
    if (!entriesContainer) return;
    entriesContainer.innerHTML = `<li class="empty">Loading...</li>`

    try {
        const qs = buildQueryFromUI();
        const result = await listEntries(qs); // {items, total, page, pageSize }
        const items = Array.isArray(result?.items) ? result.items : [];

        if (items.length === 0) {
            entriesContainer.innerHTML = `<li class="empty">No entries match your filters.</li>`;
            return;
        }

        const html = items.map((dto) => `<li>${formatEntryHTML(dto)}</li>`).join("");
        entriesContainer.innerHTML = html;
    } catch (err) {
        entriesContainer.innerHTML = `<li class="empty">Failed to load entries: ${escapeHTML(err.message || String(err))}</li>`;
    }
}

// Event delegation for Edit/Delete buttons rendered inside entriesContainer
entriesContainer?.addEventListener("click", async (e) => {
    const btn = e.target.closest("button[data-action]");
    if (!btn) return;

    const { action, id } = btn.dataset;
    if (!id) return;

    if (action === "edit") {
        window.location.href = `entry.html?mode=edit&id=${encodeURIComponent(id)}`;
    } else if (action === "delete") {
        const confirmed = confirm("Delete this entry? This cannot be undone.");
        if (!confirmed) return;
        try {
            await deleteEntry(id);
            await renderEntries(); // refresh list
        } catch (err) {
            alert(`Delete failed: ${err.message || String(err)}`);
        }
    }
});

// Re-render on filter/sort changes
filterTypeEl?.addEventListener("change", renderEntries);
filterSubTypeEl?.addEventListener("change", renderEntries);
filterGenreEl?.addEventListener("change", renderEntries);
sortByEl?.addEventListener("change", renderEntries);

// Initial render
renderEntries();

// ---------------------
// Dark mode persistence
// ---------------------
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