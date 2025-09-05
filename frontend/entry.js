
// ---------- DOM references ----------
const form                 = document.getElementById("mediaForm");
const title                = document.getElementById("title");
const type                 = document.getElementById("type");
const subType              = document.getElementById("subType");
const status               = document.getElementById("status");
const rating               = document.getElementById("rating");
const notes                = document.getElementById("notes");
const submitBtn            = document.getElementById("submitBtn");
const cancelBtn            = document.getElementById("cancelBtn");
const stayOnPageCheckbox   = document.getElementById("stayOnPage");
const stayCheckboxContainer= document.getElementById("stayCheckboxContainer");
const formTitleEl          = document.getElementById("formTitle");
const genresContainer      = document.getElementById("genreCheckboxes"); // parent div for genre checkboxes

// ---------- URL params (id-based) ----------
const urlParams = new URLSearchParams(window.location.search);
const mode   = (urlParams.get("mode") || "add").toLowerCase(); // "add" or "edit"
const editId = urlParams.get("id");                             // used only when mode === "edit"
const isEditMode = mode === "edit";

// ---------- Load data once ----------
let mediaList = JSON.parse(localStorage.getItem("mediaList") || "[]");

// ---------- Helpers ----------
// UUID helper: prefer crypto.randomUUID(), fallback for older browsers
const uuid = () =>
  (crypto?.randomUUID?.() ||
    "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === "x" ? r : (r & 0x3) | 0x8; // Ensure RFC-4122 variant for 'y'
      return v.toString(16);
    }));

const saveList = () => localStorage.setItem("mediaList", JSON.stringify(mediaList));

// Find entry and index by id (returns { idx, entry })
const findEntryById = (id) => {
  const idx = mediaList.findIndex((e) => String(e.id) === String(id));
  return { idx, entry: idx >= 0 ? mediaList[idx] : null };
};

// Read all checked genres from the UI into an array of strings
const collectSelectedGenres = () => {
  if (!genresContainer) return [];
  return Array.from(
    genresContainer.querySelectorAll('input[type="checkbox"]:checked')
  ).map((cb) => cb.value);
};

// Set checked state for the genre checkboxes based on an array of strings
const setSelectedGenres = (genres) => {
  if (!genresContainer) return;
  const set = new Set(Array.isArray(genres) ? genres : []);
  genresContainer
    .querySelectorAll('input[type="checkbox"]')
    .forEach((cb) => { cb.checked = set.has(cb.value); });
};

// Show a friendly message (no redirect bounce) and halt further script
const showNotFoundAndStop = (msg = "Could not find that entry to edit.") => {
  const notice = document.createElement("div");
  notice.className = "alert";
  notice.style.margin = "1rem 0";
  notice.textContent = msg + " ";
  const back = document.createElement("a");
  back.href = "index.html";
  back.textContent = "Return to the list";
  notice.appendChild(back);

  (form || document.body).prepend(notice);
  throw new Error("Edit aborted: entry not found");
};

// Basic validation for add/edit before save
const validate = () => {
  const t = title?.value?.trim();
  if (!t) {
    alert("Please enter a title.");
    title?.focus();
    return false;
  }
  return true;
};

// ---------- Ensure Cancel never submits a form by accident ----------
if (cancelBtn) {
  cancelBtn.setAttribute("type", "button");
  cancelBtn.addEventListener("click", (e) => {
    e.preventDefault();
    window.location.href = "index.html";
  });
}

// ---------- EDIT MODE ----------
if (isEditMode) {
  // Page chrome
  if (formTitleEl)  formTitleEl.textContent = "Edit Entry";
  if (submitBtn)    submitBtn.textContent   = "Save Changes";
  if (stayCheckboxContainer) stayCheckboxContainer.style.display = "none";

  // Require an id in the URL
  if (!editId) showNotFoundAndStop("Missing entry id.");

  // Find the entry by id
  const { idx, entry } = findEntryById(editId);
  if (idx === -1 || !entry) showNotFoundAndStop();

  // Populate form fields from the entry
  if (title)   title.value   = entry.title   || "";
  if (type)    type.value    = entry.type    || "";
  if (subType) subType.value = entry.subType || "";
  if (status)  status.value  = entry.status  || "";
  if (rating)  rating.value  = entry.rating  ?? "";
  if (notes)   notes.value   = entry.notes   || "";
  setSelectedGenres(entry.genres);

  // Submit handler - update by id
  form?.addEventListener("submit", (e) => {
    e.preventDefault();
    if (!validate()) return;

    const updated = {
      ...entry, // keeps id + any other fields
      title:   title?.value?.trim() || "",
      type:    type?.value || "",
      subType: subType?.value || "",
      status:  status?.value || "",
      rating:  rating?.value, // keep as string; change to Number(...) if you want numeric
      notes:   notes?.value?.trim() || "",
      genres:  collectSelectedGenres()
    };

    mediaList[idx] = updated;
    saveList();

    // On successful save, go back to index
    window.location.href = "index.html";
  });

} else {
  // ---------- ADD MODE ----------
  // Page chrome
  if (formTitleEl)  formTitleEl.textContent = "Add Entry";
  if (submitBtn)    submitBtn.textContent   = "Add Entry";
  if (stayCheckboxContainer) stayCheckboxContainer.style.display = "";

  // Save handler: create a fresh entry with id
  form?.addEventListener("submit", (e) => {
    e.preventDefault();
    if (!validate()) return;

    const newEntry = {
      id:      uuid(), // <-- stable identifier
      title:   title?.value?.trim() || "",
      type:    type?.value || "",
      subType: subType?.value || "",
      status:  status?.value || "",
      rating:  rating?.value,
      notes:   notes?.value?.trim() || "",
      genres:  collectSelectedGenres()
    };

    mediaList.push(newEntry);
    saveList();

    // Optional 'add another' workflow
    if (stayOnPageCheckbox && stayOnPageCheckbox.checked) {
      form.reset();
      setSelectedGenres([]); // clear all checked genres
      title?.focus();
    } else {
      window.location.href = "index.html";
    }
  });
}

// --------------------------------------------------------------
// Dark mode support
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
