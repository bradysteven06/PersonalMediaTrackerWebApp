// --------------------------------------------------------------
// Media Tracker - List Page (index.html)
// Switch to stable ids + safe edit/delete by id
// --------------------------------------------------------------

// Get references to form and display container
const entriesContainer = document.getElementById("entriesContainer");

// Load existing entries from localStorage, or initialize empty array
let mediaList = JSON.parse(localStorage.getItem("mediaList")) || [];

/**
 * Ensure every entry has a stable `id`. This runs once per load and also
 * migrates older entries that were saved without ids.
 */
function ensureEntryIds(list) {
  const uuid = () =>
    (crypto?.randomUUID?.() ||
      // Fallback UUID v4-ish if crypto.randomUUID is unavailable
      "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, c => {
        const r = (Math.random() * 16) | 0;
        const v = c === "x" ? r : (r & 0x3) | 0x8;
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

// Save current media list to localStorage
function saveToLocalStorage() {
  localStorage.setItem("mediaList", JSON.stringify(mediaList));
}

/**
 * Filtering + sorting logic (unchanged structure; returns filtered array).
 * NOTE: We render from the filtered list, but we NEVER use the filtered index
 * as an identifier. We always use entry.id for links/actions.
 */
function getFilteredEntries() {
  const typeFilter = document.getElementById("filterType").value;
  const subTypeFilter = document.getElementById("filterSubType").value;
  const genreFilter = document.getElementById("filterGenre").value;
  const sortBy = document.getElementById("sortBy").value;

  // Apply filters
  let filtered = mediaList.filter(entry => {
    const matchesType = typeFilter === "" || entry.type === typeFilter;
    const matchesSubType = subTypeFilter === "" || entry.subType === subTypeFilter;
    const matchesGenre =
      genreFilter === "" || (entry.genres && entry.genres.includes(genreFilter));
    return matchesType && matchesSubType && matchesGenre;
  });

  // Apply sort
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
 * Render all entries to the page, building the Edit link with a stable id.
 * We also pass the id to deleteEntryById instead of array indexes.
 */
function renderEntries() {
  entriesContainer.innerHTML = ""; // Clear existing entries

  const filteredEntries = getFilteredEntries();

  filteredEntries.forEach(entry => {
    const li = document.createElement("li");
    li.innerHTML = `
      <strong>${entry.title}</strong> (${entry.type} - ${entry.subType}, ${entry.status})<br/>
      Genres: ${entry.genres?.map(g => `<span class="genre-badge">${g}</span>`).join(" ") || "N/A"} - Rating: ${entry.rating ?? 'N/A'}
      <br/><small>${entry.notes || ""}</small>
      <br/>
      <a href="entry.html?mode=edit&id=${encodeURIComponent(entry.id)}">
        <button type="button">Edit</button>
      </a>
      <button type="button" onclick="deleteEntryById('${entry.id}')">Delete</button>
    `;
    entriesContainer.appendChild(li);
  });
}

/**
 * Delete by stable id (find index in the full mediaList, not filtered array).
 */
function deleteEntryById(id) {
  const idx = mediaList.findIndex(e => String(e.id) === String(id));
  if (idx === -1) return;

  const confirmed = confirm(`Are you sure you want to delete "${mediaList[idx].title}"?`);
  if (!confirmed) return;

  mediaList.splice(idx, 1);
  saveToLocalStorage();
  renderEntries();
}

// --------------------------------------------------------------
// Initial sample data injection (only when list is empty)
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
    },
    {
      title: "The Mandalorian",
      type: "series",
      subType: "live-action",
      genres: ["action", "sci-fi"],
      status: "completed",
      rating: 8,
      notes: "Star Wars done right."
    },
    {
      title: "One Piece: Strong World",
      type: "movie",
      subType: "anime",
      genres: ["romance", "fantasy"],
      status: "completed",
      rating: 9,
      notes: "Beautiful and emotional."
    }
  ];
  // Assign ids to the seeded entries
  ensureEntryIds(mediaList);
  saveToLocalStorage();
}

// Always ensure ids exist for older saved data
ensureEntryIds(mediaList);

// Render list on page load
renderEntries();

// Re-render on filter/sort changes
document.getElementById("filterType").addEventListener("change", renderEntries);
document.getElementById("filterSubType").addEventListener("change", renderEntries);
document.getElementById("filterGenre").addEventListener("change", renderEntries);
document.getElementById("sortBy").addEventListener("change", renderEntries);

// --------------------------------------------------------------
// Dark mode support (unchanged behavior)
// --------------------------------------------------------------
const darkToggle = document.getElementById("darkModeToggle");
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
