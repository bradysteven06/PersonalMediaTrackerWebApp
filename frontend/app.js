
// ---------- DOM references ----------
const entriesContainer = document.getElementById("entriesContainer");
const filterTypeEl     = document.getElementById("filterType");
const filterSubTypeEl  = document.getElementById("filterSubType");
const filterGenreEl    = document.getElementById("filterGenre");
const sortByEl         = document.getElementById("sortBy");
const darkToggle       = document.getElementById("darkModeToggle");

// ---------- Load data ----------
let mediaList = JSON.parse(localStorage.getItem("mediaList") || "[]");

// --------------------------------------------------------------
// Storage helpers
// --------------------------------------------------------------

/**
 * Persist current mediaList to localStorage.
 * Keeping writes centralized makes the code easier to reason about.
 */
function saveToLocalStorage() {
  localStorage.setItem("mediaList", JSON.stringify(mediaList));
}

/**
 * Assign a stable `id` to every entry that doesn't have one yet.
 * - This is an in-place, idempotent migration (safe to call every load).
 */
function ensureEntryIds(list) {
  const uuid = () =>
    (crypto?.randomUUID?.() ||
      "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
        const r = (Math.random() * 16) | 0;
        const v = c === "x" ? r : (r & 0x3) | 0x8; // RFC-4122 variant nibble for 'y'
        return v.toString(16);
      }));

  let mutated = false;
  for (const entry of list) {
    if (!entry.id) {
      entry.id = uuid();
      mutated = true;
    }
  }
  if (mutated) saveToLocalStorage();
}

// Ensure ids exist on page load (covers old data and seeded data)
ensureEntryIds(mediaList);

// --------------------------------------------------------------
// Rendering helpers
// --------------------------------------------------------------

/**
 * Basic HTML escape to guard against XSS when rendering user-entered fields.
 */
function escapeHTML(s) {
  return String(s ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

/**
 * Returns a new array filtered and sorted according to the UI controls.
 */
function getFilteredEntries() {
  const typeFilter    = filterTypeEl?.value || "";
  const subTypeFilter = filterSubTypeEl?.value || "";
  const genreFilter   = filterGenreEl?.value || "";
  const sortBy        = sortByEl?.value || "";

  // Filter pass
  let filtered = mediaList.filter((entry) => {
    const matchesType    = !typeFilter || entry.type === typeFilter;
    const matchesSubType = !subTypeFilter || entry.subType === subTypeFilter;
    const matchesGenre   = !genreFilter || (Array.isArray(entry.genres) && entry.genres.includes(genreFilter));
    return matchesType && matchesSubType && matchesGenre;
  });

  // Sort pass (defensive against missing fields)
  if (sortBy === "title") {
    filtered.sort((a, b) => (a.title || "").localeCompare(b.title || ""));
  } else if (sortBy === "rating") {
    filtered.sort((a, b) => (Number(b.rating) || 0) - (Number(a.rating) || 0));
  } else if (sortBy === "status") {
    filtered.sort((a, b) => (a.status || "").localeCompare(b.status || ""));
  }
  return filtered;
}

/**
 * Builds a single entry's HTML (string) for insertion into the list.
 * NOTE: values are escaped to prevent HTML injection.
 */
function formatEntryHTML(entry) {
  const title  = escapeHTML(entry.title);
  const type   = escapeHTML(entry.type);
  const sub    = escapeHTML(entry.subType);
  const status = escapeHTML(entry.status);
  const rating = entry.rating ?? "N/A";
  const notes  = escapeHTML(entry.notes || "");

  const genresHTML = Array.isArray(entry.genres) && entry.genres.length
    ? entry.genres.map((g) => `<span class="genre-badge">${escapeHTML(g)}</span>`).join(" ")
    : "N/A";

  return `
    <div class="entry-row">
      <div class="entry-main">
        <strong>${title}</strong>
        <span class="entry-meta">(${type} - ${sub}, ${status})</span>
      </div>
      <div class="entry-sub">
        Genres: ${genresHTML} &nbsp;—&nbsp; Rating: ${escapeHTML(rating)}
      </div>
      ${notes ? `<div class="entry-notes"><small>${notes}</small></div>` : ""}
      <div class="entry-actions">
        <button type="button" class="btn" data-action="edit" data-id="${entry.id}">Edit</button>
        <button type="button" class="btn btn-danger" data-action="delete" data-id="${entry.id}">Delete</button>
      </div>
    </div>
  `;
}

/**
 * Render the entire list according to current filters/sorts.
 * Uses event delegation for action buttons via data-* attributes.
 */
function renderEntries() {
  if (!entriesContainer) return;

  const filtered = getFilteredEntries();
  if (filtered.length === 0) {
    entriesContainer.innerHTML = `<li class="empty">No entries match your filters.</li>`;
    return;
  }

  // Build once as a single HTML string for performance
  const html = filtered.map((entry) => `<li>${formatEntryHTML(entry)}</li>`).join("");
  entriesContainer.innerHTML = html;
}

// --------------------------------------------------------------
// Actions (Edit/Delete) — id-based
// --------------------------------------------------------------

/**
 * Navigate to the entry page in edit mode for a specific id.
 * Clear any accidental stale session data here if you add sessionStorage later.
 */
function openEditById(id) {
  window.location.href = `entry.html?mode=edit&id=${encodeURIComponent(id)}`;
}

/**
 * Delete a record by stable id. Finds its index in mediaList and splices.
 */
function deleteEntryById(id) {
  const idx = mediaList.findIndex((e) => String(e.id) === String(id));
  if (idx === -1) return;

  const name = mediaList[idx]?.title ? `"${mediaList[idx].title}"` : "this entry";
  const confirmed = confirm(`Delete ${name}? This cannot be undone.`);
  if (!confirmed) return;

  mediaList.splice(idx, 1);
  saveToLocalStorage();
  renderEntries();
}

// Event delegation for Edit/Delete buttons rendered inside entriesContainer
if (entriesContainer) {
  entriesContainer.addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-action]");
    if (!btn) return;

    const { action, id } = btn.dataset;
    if (!id) return;

    if (action === "edit") {
      openEditById(id);
    } else if (action === "delete") {
      deleteEntryById(id);
    }
  });
}

// --------------------------------------------------------------
// Filters and sorting — re-render on change
// --------------------------------------------------------------
filterTypeEl     && filterTypeEl.addEventListener("change", renderEntries);
filterSubTypeEl  && filterSubTypeEl.addEventListener("change", renderEntries);
filterGenreEl    && filterGenreEl.addEventListener("change", renderEntries);
sortByEl         && sortByEl.addEventListener("change", renderEntries);

// --------------------------------------------------------------
// Seed data (optional): only when list is empty
// --------------------------------------------------------------
if (mediaList.length === 0) {
  mediaList = [
    {
      title: "Spirited Away",
      type: "movie",
      subType: "anime",
      genres: ["fantasy", "adventure"],
      status: "completed",
      rating: 10,
      notes: "Gorgeous visuals and emotional story."
    },
    {
      title: "Breaking Bad",
      type: "series",
      subType: "live-action",
      genres: ["drama", "crime"],
      status: "completed",
      rating: 9,
      notes: "Amazing character development."
    },
    {
      title: "Attack on Titan",
      type: "series",
      subType: "anime",
      genres: ["action", "drama"],
      status: "in-progress",
      rating: 8,
      notes: "Intense and plot-heavy."
    }
  ];
  ensureEntryIds(mediaList);  // assign ids to the seeded records
  saveToLocalStorage();
}

// Initial render
renderEntries();

// --------------------------------------------------------------
// Dark mode persistence
// --------------------------------------------------------------
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
