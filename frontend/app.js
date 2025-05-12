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

  mediaList.forEach((entry, index) => {
    const li = document.createElement("li");
    li.innerHTML = `
      <strong>${entry.title}</strong> (${entry.type}, ${entry.status}) - Rating: ${entry.rating || 'N/A'}
      <br/><small>${entry.notes}</small>
      <br/>
      <button onclick="editEntry(${index})">Edit</button>
      <button onclick="deleteEntry(${index})">Delete</button>
    `;
    entriesContainer.appendChild(li);
  });
}

// Edit an entry by its index
function editEntry(index) {
    const entry = mediaList[index];

    document.getElementById("title").value = entry.title;
    document.getElementById("type").value = entry.type;
    document.getElementById("status").value = entry.status;
    document.getElementById("rating").value = entry.rating;
    document.getElementById("notes").value = entry.notes;

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

  // Create a entry object from form inputs
  const entryData = {
    title: document.getElementById("title").value.trim(),
    type: document.getElementById("type").value,
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

// Render list on page load
renderEntries();
