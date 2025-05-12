// Get references to form and display container
const form = document.getElementById("mediaForm");
const entriesContainer = document.getElementById("entriesContainer");

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
      <button onclick="deleteEntry(${index})">Delete</button>
    `;
    entriesContainer.appendChild(li);
  });
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

  // Create a new entry object from form inputs
  const newEntry = {
    title: document.getElementById("title").value.trim(),
    type: document.getElementById("type").value,
    status: document.getElementById("status").value,
    rating: document.getElementById("rating").value,
    notes: document.getElementById("notes").value.trim()
  };

  // Add to media list and update storage
  mediaList.push(newEntry);
  saveToLocalStorage();
  renderEntries();
  form.reset(); // Clear form
});

// Render list on page load
renderEntries();
