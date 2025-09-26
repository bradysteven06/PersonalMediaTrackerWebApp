/**
 * Load a single entry by id (edit) or create a new one (add).
 *  - Maps UI type,subType/status to API enums and vice versa
 *  - Keeps your validation, stay-on-page option, and dark mode behavior
 */

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

// ----- Status/Type mapping -----
// Map UI status select -> API status string
function uiStatusToApi(v) {
    switch ((v || "").toLowerCase()) {
        case "watching":        return "Watching";
        case "completed":       return "Completed";
        case "on-hold" :        return "On-Hold";
        case "dropped":         return "Dropped";
        case "plan-to-watch":   return "Planning";
        default:                return "Planning";
    }
}

// Map API status -> UI status select value
function apiStatusToUi(s) {
    switch ((s || "").toLowerCase()) {
        case "watching":        return "watching";
        case "completed":       return "completed";
        case "on-hold" :        return "on-Hold";
        case "dropped":         return "dropped";
        case "planning":        return "plan-to-watch";
        default:                return "plan-to-watch";
    }
}

// Map UI type/subType -> API Type enum string
function uiToApiType(typeValue, subTypeValue) {
    const t = (typeValue || "").toLowerCase();
    const s = (subTypeValue || "").toLowerCase();
    if (s === "manga") return "Manga";
    if (s === "anime") return "Anime";
    if (t === "series") return "Tv";
    return "Movie";
}

// Map API Type -> UI type/subType
function apiTypeToUi(apiType) {
    switch ((apiType || "").toLowerCase()) {
        case "manga": return { type: "series", subType: "manga" };
        case "anime": return { type: "series", subType: "anime" };
        case "tv": return { type: "series", subType: "live-action" };
        case "movie":
        default: return { type: "movie", subType: "live-action" };
    }
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

            // Populate fields
            title && (title.value = entry.title || "");
            const tMap = apiTypeToUi(entry.type);
            type && (type.value = tMap.type);
            subType && (subType.value = tMap.subType);
            status && (status.value = apiStatusToUi(entry.status));
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
            type: uiToApiType(type?.value, subType?.value),
            status: uiStatusToApi(status?.value),
            rating: rating?.value ? Number(rating.value) : null,
            progress: null,
            total: null,
            startedAt: null,
            finishedAt: null,
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
            type: uiToApiType(type?.value, subType?.value),
            status: uiStatusToApi(status?.value),
            rating: rating?.value ? Number(rating.value) : null,
            progress: null,
            total: null,
            startedAt: null,
            finishedAt: null,
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