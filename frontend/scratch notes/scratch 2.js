// --------------------------------------------------------------
// Media Tracker - Add/Edit Page (entry.html)
// Look up entries by stable id (not array index) + graceful errors
// --------------------------------------------------------------

// Form element references
const form = document.getElementById("mediaForm");
const title = document.getElementById("title");
const type = document.getElementById("type");
const subType = document.getElementById("subType");
const status = document.getElementById("status");
const rating = document.getElementById("rating");
const notes = document.getElementById("notes");
const submitBtn = document.getElementById("submitBtn");
const cancelBtn = document.getElementById("cancelBtn");
const stayOnPageCheckbox = document.getElementById("stayOnPage");
const stayCheckboxContainer = document.getElementById("stayCheckboxContainer");
const formTitleEl = document.getElementById("formTitle");

// Read all entries (we’ll ensure ids on the list page; safe here too if needed)
let mediaList = JSON.parse(localStorage.getItem("mediaList")) || [];

// Parse query params: mode=add|edit, id=<uuid>
const urlParams = new URLSearchParams(window.location.search);
const mode = (urlParams.get("mode") || "add").toLowerCase();
const editId = urlParams.get("id");

// Convenience: find entry + index by id
function findEntryById(id) {
  const idx = mediaList.findIndex(e => String(e.id) === String(id));
  return { idx, entry: idx >= 0 ? mediaList[idx] : null };
}

/**
 * Show a friendly message if the id is missing/invalid instead of redirecting.
 * This avoids the "flash to form, then bounce" effect.
 */
function showNotFoundAndStop() {
  const notice = document.createElement("div");
  notice.className = "alert";
  notice.style.margin = "1rem 0";
  notice.textContent = "Could not find that entry to edit. ";
  const back = document.createElement("a");
  back.href = "index.html";
  back.textContent = "Return to the list";
  notice.appendChild(back);
  (form || document.body).prepend(notice);
  throw new Error("Edit aborted: entry not found");
}

const isEditMode = mode === "edit";

if (isEditMode) {
  // Require a valid id
  if (!editId) showNotFoundAndStop();

  const { idx, entry } = findEntryById(editId);
  if (idx === -1 || !entry) showNotFoundAndStop();

  // Update page chrome for edit mode
  if (formTitleEl) formTitleEl.textContent = "Edit Entry";
  if (submitBtn) submitBtn.textContent = "Save Changes";
  if (stayCheckboxContainer) stayCheckboxContainer.style.display = "none";

  // Populate form from the entry
  title.value = entry.title || "";
  type.value = entry.type || "";
  subType.value = entry.subType || "";
  status.value = entry.status || "";
  rating.value = entry.rating ?? "";
  notes.value = entry.notes || "";

  // Restore genres to checkboxes
  const genreCheckboxes = document.querySelectorAll('#genreCheckboxes input[type="checkbox"]');
  const genres = Array.isArray(entry.genres) ? entry.genres : [];
  genreCheckboxes.forEach(cb => {
    cb.checked = genres.includes(cb.value);
  });

  // Submit handler - update by id
  form.addEventListener("submit", e => {
    e.preventDefault();

    const selectedGenres = Array
      .from(document.querySelectorAll('#genreCheckboxes input[type="checkbox"]:checked'))
      .map(cb => cb.value);

    const updated = {
      ...entry, // keep id + any other fields
      title: title.value.trim(),
      type: type.value,
      subType: subType.value,
      status: status.value,
      rating: rating.value, // keep as string or Number(rating.value) if desired
      notes: notes.value.trim(),
      genres: selectedGenres
    };

    mediaList[idx] = updated;
    localStorage.setItem("mediaList", JSON.stringify(mediaList));

    // On successful save, go back to index
    window.location.href = "index.html";
  });

} else {
  // ADD mode - show "Stay on page" option and create a new entry with id
  if (formTitleEl) formTitleEl.textContent = "Add Entry";
  if (submitBtn) submitBtn.textContent = "Add Entry";
  if (stayCheckboxContainer) stayCheckboxContainer.style.display = "";

  form.addEventListener("submit", e => {
    e.preventDefault();

    const selectedGenres = Array
      .from(document.querySelectorAll('#genreCheckboxes input[type="checkbox"]:checked'))
      .map(cb => cb.value);

    const uuid = () =>
      (crypto?.randomUUID?.() ||
        "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, c => {
          const r = (Math.random() * 16) | 0;
          const v = c === "x" ? r : (r & 0x3) | 0x8;
          return v.toString(16);
        }));

    const newEntry = {
      id: uuid(),
      title: title.value.trim(),
      type: type.value,
      subType: subType.value,
      status: status.value,
      rating: rating.value,
      notes: notes.value.trim(),
      genres: selectedGenres
    };

    mediaList.push(newEntry);
    localStorage.setItem("mediaList", JSON.stringify(mediaList));

    if (stayOnPageCheckbox && stayOnPageCheckbox.checked) {
      // Stay on page to add more
      form.reset();
      // Also clear checked genres
      document
        .querySelectorAll('#genreCheckboxes input[type="checkbox"]')
        .forEach(cb => (cb.checked = false));
      title.focus();
    } else {
      // Navigate back to list
      window.location.href = "index.html";
    }
  });
}

// Cancel button just redirects back to main list
cancelBtn.addEventListener("click", () => {
  window.location.href = "index.html";
});

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
