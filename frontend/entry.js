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

const urlParams = new URLSearchParams(window.location.search);
const editIndex = urlParams.get("edit");
let isEditMode = editIndex !== null;
let mediaList = JSON.parse(localStorage.getItem("mediaList")) || [];

// If editing, load existing entry and fill the form
if (isEditMode) {
    const entry = mediaList[editIndex];
    if (!entry) window.location.href = "index.html";

    document.getElementById("formTitle").textContent = "Edit Entry";
    submitBtn.textContent = "Save Changes";
    stayCheckboxContainer.style.display = "none";

    title.value = entry.title;
    type.value = entry.type;
    subType.value = entry.subType;
    status.value = entry.status;
    rating.value = entry.rating;
    notes.value = entry.notes;

    const genreCheckboxes = document.querySelectorAll('#genreCheckboxes input[type="checkbox"]');
    genreCheckboxes.forEach(cb => {
    cb.checked = entry.genres.includes(cb.value);
    });
}

// Handle form submit (add or update entry)
form.addEventListener("submit", e => {
    e.preventDefault();
  
    const selectedGenres = Array.from(document.querySelectorAll('#genreCheckboxes input[type="checkbox"]:checked'))
        .map(cb => cb.value);
  
    const entryData = {
        title: title.value.trim(),
        type: type.value,
        subType: subType.value,
        status: status.value,
        rating: rating.value,
        notes: notes.value.trim(),
        genres: selectedGenres
    };
  
    if (isEditMode) {
        mediaList[editIndex] = entryData;
    } else {
        mediaList.push(entryData);
    }
  
    localStorage.setItem("mediaList", JSON.stringify(mediaList));
  
    // Redirect or reset
    if (isEditMode || !stayOnPageCheckbox.checked) {
        window.location.href = "index.html";
    } else {
        form.reset(); // Clear form if staying on page
    }
});

// Cancel button just redirects back to main list
cancelBtn.addEventListener("click", () => {
    window.location.href = "index.html";
});
  
// Dark mode support
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
