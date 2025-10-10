/**
 * Add/Edit page wired to API with enum-safe mapping.
 *  - Maps UI selects -> Enum strings for create/update
 *  - Maps Enum strings -> UI selects for edit load
 */

import {
  uiTypeToEnum, uiSubTypeToEnum, uiStatusToEnum,
  enumTypeToUI, enumSubTypeToUI, enumStatusToUI
} from "./enums.js";
import { api } from "./api.js";

// ----- Constants -----
// Centralize the base path so you never forget it in API calls.
const BASE = "/api/mediaentries";

// ----- DOM references -----
const form = document.getElementById("mediaForm");
const titleInput = document.getElementById("title");
const typeSelect = document.getElementById("type");
const subTypeSelect = document.getElementById("subType");
const statusSelect = document.getElementById("status");
const ratingInput = document.getElementById("rating");
const notesInput = document.getElementById("notes");
const submitBtn = document.getElementById("submitBtn");
const cancelBtn = document.getElementById("cancelBtn");
const stayOnPageToggle = document.getElementById("stayOnPageToggle");
const stayCheckboxContainer = document.getElementById("stayCheckboxContainer");
const formTitleEl = document.getElementById("formTitle");
const genresContainer = document.getElementById("genreContainer"); // parent div for genre checkboxes
const darkToggle = document.getElementById("darkModeToggle");

// ----- URL params (id-based) -----
const urlParams = new URLSearchParams(window.location.search);
const mode = (urlParams.get("mode") || "add").toLowerCase(); // "add" or "edit"
const editId = urlParams.get("id"); // used only when mode === "edit"
const isEditMode = mode === "edit";

// Converts "", null, undefined -> null. Any number is rounded to nearest 0.5.
function parseOptionalRating(v) {
    if (v === null || v === undefined) return null;
    const s = String(v).trim();
    if (!s) return null;
    const n = Number(s);
    if (Number.isNaN(n)) return null;
    // normalize to 0.5 increments
    const halfSteps = Math.round(n * 2);
    return halfSteps / 2;
}

// ----- Genre helpers -----
// Read all checked genres from the UI into an array of strings
const collectSelectedGenres = () => {
    if (!genresContainer) return [];
    return Array.from(
        genresContainer.querySelectorAll('input[type="checkbox"]:checked')
    ).map((cb) => cb.value);
};

// Set checkbox state based on array of strings
const setSelectedGenres = (genres) => {
    if (!genresContainer) return;
    const set = new Set(Array.isArray(genres) ? genres : []);
    genresContainer.querySelectorAll('input[type="checkbox"]').forEach((cb) => {
        cb.checked = set.has(cb.value);
    });
}

// ----- UX helpers -----
// Show a friendly message and halt further script
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

// Simple required title validation
const validate = () => {
    const t = titleInput?.value?.trim();
    if (!t) {
        alert("Please enter a title.");
        titleInput?.focus();
        return false;
    }
    return true;
}

// ----- Ensure Cancel never submits a form by accident -----
if (cancelBtn) {
    cancelBtn.setAttribute("type", "button");
    cancelBtn.addEventListener("click", (e) => {
        e.preventDefault();
        window.location.href = "index.html";
    });
}

// Build DTO from form, UI -> payload
function buildDtoFromForm() {
    return {
        title: titleInput?.value?.trim() || "",
        type: uiTypeToEnum(typeSelect?.value),
        subType: uiSubTypeToEnum(subTypeSelect?.value) || null,
        status: uiStatusToEnum(statusSelect?.value),
        rating: parseOptionalRating(ratingInput?.value),
        notes: notesInput?.value?.trim() || "",
        tags: collectSelectedGenres()
  };
}

// Apply DTO to form, payload -> UI mapping
function applyDtoToForm(dto) {
    titleInput && (titleInput.value = dto.title ?? "");
    typeSelect && (typeSelect.value = enumTypeToUI(dto.type));
    subTypeSelect && (subTypeSelect.value = enumSubTypeToUI(dto.subType));
    statusSelect && (statusSelect.value = enumStatusToUI(dto.status));
    ratingInput && (ratingInput.value = dto.rating ?? "");
    notesInput && (notesInput.value = dto.notes ?? "");
    setSelectedGenres(dto.tags);
}

// Mode initializer (non-submit tasks)
// - Sets headings/butons, shows/hides "stay on page", loads DTO for edit and populates form
async function initMode() { 
    if (isEditMode) {
        formTitleEl && (formTitleEl.textContent = "Edit Entry"); 
        submitBtn && (submitBtn.textContent = "Save Changes");   
        if (stayCheckboxContainer) stayCheckboxContainer.style.display = "none"; 

        if (!editId) showNotFoundAndStop("Missing entry id.");   

        try {
        const entry = await api.get(`${BASE}/${editId}`);      
        if (!entry) showNotFoundAndStop();
        applyDtoToForm(entry);                                 
        } catch {
        showNotFoundAndStop("Failed to load entry.");
        }
    } else {
        formTitleEl && (formTitleEl.textContent = "Add Entry");  
        submitBtn && (submitBtn.textContent = "Add Entry");      
        if (stayCheckboxContainer) stayCheckboxContainer.style.display = ""; 
    }
}

// Submit function
// - Prevents double submit, decides PUT vs POST by isEditMode, honors "stay on page" toggle
async function submitEntry(e) { 
    e?.preventDefault?.();
    if (!validate()) return;

    const prevText = submitBtn.textContent; 
    submitBtn.disabled = true;              
    submitBtn.textContent = isEditMode ? "Saving..." : "Adding..."; 

    try {
        const payload = buildDtoFromForm();   

        if (isEditMode && editId) {
        await api.put(`${BASE}/${editId}`, payload); 
        window.location.href = "index.html";
        } else {
        await api.post(`${BASE}`, payload);         
        if (stayOnPageToggle && stayOnPageToggle.checked) {
            form.reset();
            setSelectedGenres([]);
            titleInput?.focus();
        } else {
            window.location.href = "index.html";
        }
        }
    } catch (err) {
        alert(`Save failed: ${err?.message || String(err)}`);
    } finally {
        submitBtn.disabled = false;          
        submitBtn.textContent = prevText;    
    }
}

// 
initMode() 
    .then(() => {
        form?.addEventListener("submit", submitEntry); 
    })
    .catch(err => {
        console.error(err); // errors are surfaced to the user by initMode
    });


// ------------------
// Dark mode support
// ------------------
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