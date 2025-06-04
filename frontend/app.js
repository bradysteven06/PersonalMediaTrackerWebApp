// Get references to form and display container
const form = document.getElementById("mediaForm");
const entriesContainer = document.getElementById("entriesContainer");
const cancelEditBtn = document.getElementById("cancelEditBtn");

// Track edit mode and index
let isEditMode = false;
let editIndex = null;

// Load existing entries from localStorage, or initialize empty array
let mediaList = JSON.parse(localStorage.getItem("mediaList")) || [];

// Save current media list to localStorage
function saveToLocalStorage() {
    localStorage.setItem("mediaList", JSON.stringify(mediaList));
}

// Render all entries to the page
function renderEntries() {
    entriesContainer.innerHTML = ""; // Clear existing entries

    const filteredEntries = getFilteredEntries();

    filteredEntries.forEach((entry, index) => {
        const li = document.createElement("li");
        li.innerHTML = `
            <strong>${entry.title}</strong> (${entry.type} - ${entry.subType}, ${entry.status})<br/>
            Genres: ${entry.genres?.join(", ") || "N/A"} - Rating: ${entry.rating || 'N/A'}
            <br/><small>${entry.notes}</small>
            <br/>
            <button onclick="editEntry(${index})">Edit</button>
            <button onclick="deleteEntry(${index})">Delete</button>
        `;
        entriesContainer.appendChild(li);
    });
}

// Gets the filter options 
function getFilteredEntries() {
    const typeFilter = document.getElementById("filterType").value;
    const subTypeFilter = document.getElementById("filterSubType").value;
    const genreFilter = document.getElementById("filterGenre").value;
    const sortBy = document.getElementById("sortBy").value;
  
    // Apply filters
    let filtered = mediaList.filter(entry => {
        const matchesType = typeFilter === "" || entry.type === typeFilter;
        const matchesSubType = subTypeFilter === "" || entry.subType === subTypeFilter;
        const matchesGenre = genreFilter === "" || (entry.genres && entry.genres.includes(genreFilter));
        return matchesType && matchesSubType && matchesGenre;
    });

    // Apply sort
    if (sortBy === "title") {
        filtered.sort((a, b) => a.title.localeCompare(b.title));
    } else if (sortBy === "rating") {
        filtered.sort((a, b) => (b.rating || 0) - (a.rating || 0));
    } else if (sortBy === "status") {
        filtered.sort((a, b) => a.status.localeCompare(b.status));
    }

    return filtered;
  }

// Edit an entry by its index
function editEntry(index) {
    const entry = mediaList[index];

    document.getElementById("title").value = entry.title;
    document.getElementById("type").value = entry.type;
    document.getElementById("subType").value = entry.subType;
    document.getElementById("status").value = entry.status;
    document.getElementById("rating").value = entry.rating;
    document.getElementById("notes").value = entry.notes;

    const genreCheckboxes = document.querySelectorAll('#genreCheckboxes input[type="checkbox"]');
    genreCheckboxes.forEach(checkbox => {
        checkbox.checked = entry.genres?.includes(checkbox.value);
    });

    isEditMode = true;
    editIndex = index;

    // Change button text
    form.querySelector("button[type='submit']").textContent = "Save Changes";

    cancelEditBtn.style.display = "inline-block";
}

function cancelEditMode() {
    isEditMode = false;
    editIndex = null;
    form.reset(); // Clear form fields
    form.querySelector("button[type='submit']").textContent = "Add Entry";
    cancelEditBtn.style.display = "none";
}

// Remove an entry by its index
function deleteEntry(index) {
  mediaList.splice(index, 1);        // Remove from array
  saveToLocalStorage();              // Save updated list
  renderEntries();                   // Re-render UI
}

// Handle form submission
form.addEventListener("submit", (e) => {
    e.preventDefault(); // Prevent page reload

    const selectedGenres = Array.from(document.querySelectorAll('#genreCheckboxes input[type="checkbox"]:checked'))
    .map(checkbox => checkbox.value);

    // Create a entry object from form inputs
    const entryData = {
        title: document.getElementById("title").value.trim(),
        type: document.getElementById("type").value,
        subType: document.getElementById("subType").value,
        genres: selectedGenres,
        status: document.getElementById("status").value,
        rating: document.getElementById("rating").value,
        notes: document.getElementById("notes").value.trim()
    };

    if (isEditMode) {
        // Update existing entry
        mediaList[editIndex] = entryData;
        isEditMode = false;
        editIndex = null;
        form.querySelector("button[type='submit']").textContent = "Add Entry";
        cancelEditBtn.style.display = "none";
    } else {
        // Add new entry
        mediaList.push(entryData);
    }

    // Add to media list and update storage
    saveToLocalStorage();
    renderEntries();
    form.reset(); // Clear form
});

cancelEditBtn.addEventListener("click", cancelEditMode);

// inject sample data for testing
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
    saveToLocalStorage();
}

// Render list on page load
renderEntries();

document.getElementById("filterType").addEventListener("change", renderEntries);
document.getElementById("filterSubType").addEventListener("change", renderEntries);
document.getElementById("filterGenre").addEventListener("change", renderEntries);
document.getElementById("sortBy").addEventListener("change", renderEntries);
