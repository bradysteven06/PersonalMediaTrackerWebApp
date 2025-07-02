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
            Genres: ${entry.genres?.map(genre => `<span class="genre-badge">${genre}</span>`).join(" ") || "N/A"} - Rating: ${entry.rating || 'N/A'}
            <br/><small>${entry.notes}</small>
            <br/>
            <a href="entry.html?edit=${index}">
                <button>Edit</button>
            </a>
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

// Remove an entry by its index
function deleteEntry(index) {
    // confirmation for deleting an entry
    const confirmed = confirm(`Are you sure you want to delete "${mediaList[index].title}"?`);
    if (!confirmed) return; 

    mediaList.splice(index, 1);        // Remove from array
    saveToLocalStorage();              // Save updated list
    renderEntries();                   // Re-render UI
}

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
