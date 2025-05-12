const form = document.getElementById("mediaForm");
const entriesContainer = document.getElementById("entriesContainer");

let mediaList = JSON.parse(localStorage.getItem("mediaList")) || [];

function saveToLocalStorage() {
  localStorage.setItem("mediaList", JSON.stringify(mediaList));
}

function renderEntries() {
  entriesContainer.innerHTML = "";
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

function deleteEntry(index) {
  mediaList.splice(index, 1);
  saveToLocalStorage();
  renderEntries();
}

form.addEventListener("submit", (e) => {
  e.preventDefault();
  const newEntry = {
    title: document.getElementById("title").value.trim(),
    type: document.getElementById("type").value,
    status: document.getElementById("status").value,
    rating: document.getElementById("rating").value,
    notes: document.getElementById("notes").value.trim()
  };
  mediaList.push(newEntry);
  saveToLocalStorage();
  renderEntries();
  form.reset();
});

renderEntries();
