/**
 * Add/Edit page wired to API with enum-safe mapping.
 *  - Maps UI selects -> Enum strings for create/update
 *  - Maps Enum strings -> UI selects for edit load
 */

import {
  uiTypeToEnum, uiSubTypeToEnum, uiStatusToEnum,
  enumTypeToUI, enumSubTypeToUI, enumStatusToUI
} from "./enums.js";
import { getEntry, createEntry, updateEntry } from "./api.js";

// ----- DOM references -----
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
const genresContainer = document.getElementById("genreCheckboxes"); // parent div for genre checkboxes
const darkToggle = document.getElementById("darkModeToggle");

// ----- URL params (id-based) -----
const urlParams = new URLSearchParams(window.location.search);
const mode = (urlParams.get("mode") || "add").toLowerCase(); // "add" or "edit"
const editId = urlParams.get("id"); // used only when mode === "edit"
const isEditMode = mode === "edit";

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
    const t = title?.value?.trim();
    if (!t) {
        alert("Please enter a title.");
        title?.focus();
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

// ----- EDIT MODE -----
if (isEditMode) {
    formTitleEl && (formTitleEl.textContent = "Edit Entry");
    submitBtn && (submitBtn.textContent = "Save Changes");
    if (stayCheckboxContainer) stayCheckboxContainer.style.display = "none";

    // Require an id in the URL
    if (!editId) showNotFoundAndStop("Missing entry id.");

    // Load from API and populate form
    (async () => {
        try {
            const entry = await getEntry(editId);
            if (!entry) showNotFoundAndStop();

            // Populate fields from DTO
            title && (title.value = entry.title || "");
            type && (type.value = enumTypeToUI(entry.type));
            subType && (subType.value = enumSubTypeToUI(entry.subType));
            status && (status.value = enumStatusToUI(entry.status));
            rating && (rating.value = entry.rating ?? "");
            notes && (notes.value = entry.notes || "");
            setSelectedGenres(entry.tags);
        } catch {
            showNotFoundAndStop("Failed to load entry.");
        }
    })();

    // Submit handler - update by id
    form?.addEventListener("submit", async (e) => {
        e.preventDefault();
        if (!validate()) return;

        const payload = {
            title: title?.value?.trim() || "",
            type: uiTypeToEnum(type?.value),
            subType: uiSubTypeToEnum(subType?.value),
            status: uiStatusToEnum(status?.value),
            rating: rating?.value ? Number(rating.value) : null,
            notes: notes?.value?.trim() || "",
            tags: collectSelectedGenres()
        };

        try {
            await updateEntry(editId, payload);
            window.location.href = "index.html";
        } catch (err) {
            alert(`Save failed: ${err.message || String(err)}`);
        }
    });
} else {
    //----- ADD MODE -----
    formTitleEl && (formTitleEl.textContent = "Add Entry");
    submitBtn && (submitBtn.textContent = "Add Entry");
    if (stayCheckboxContainer) stayCheckboxContainer.style.display = "";

    form?.addEventListener("submit", async (e) => {
        e.preventDefault();
        if (!validate()) return;

        const payload = {
            title: title?.value?.trim() || "",
            type: uiTypeToEnum(type?.value),
            subType: uiSubTypeToEnum(subType?.value),
            status: uiStatusToEnum(status?.value),
            rating: rating?.value ? Number(rating.value) : null,
            notes: notes?.value?.trim() || "",
            tags: collectSelectedGenres()
        };

        try {
            await createEntry(payload);
            if (stayOnPageCheckbox && stayOnPageCheckbox.checked) {
                form.reset();
                setSelectedGenres([]);
                title?.focus();
            } else {
                window.location.href = "index.html";  
            }            
        } catch (err) {
            alert(`Create failed: ${err.message || String(err)}`);
        }
    });
}

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