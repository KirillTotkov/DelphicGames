let currentNominationId = null;
let selectedCameraIds = [];

document.addEventListener("DOMContentLoaded", async () => {
  await loadNominations();

  // Delete nomination
  document.getElementById("table-body").addEventListener("click", (event) => {
    if (event.target.id === "deleteBtn") {
      const id = event.target.dataset.id;
      deleteNomination(id);
    }
  });

  // Filter cameras by URL or name
  document.getElementById("filterUrl").addEventListener("input", filterCameras);
  document
    .getElementById("filterName")
    .addEventListener("input", filterCameras);

  // Handle checkbox changes
  document
    .querySelector("#addGroupModal table tbody")
    .addEventListener("change", (e) => {
      if (e.target && e.target.type === "checkbox") {
        const cameraId = parseInt(e.target.value);
        if (e.target.checked) {
          if (!selectedCameraIds.includes(cameraId)) {
            selectedCameraIds.push(cameraId);
          }
        } else {
          selectedCameraIds = selectedCameraIds.filter((id) => id !== cameraId);
        }
      }
    });
});

// Open modal for adding a new nomination
document
  .querySelector('[data-bs-target="#addGroupModal"]')
  .addEventListener("click", () => {
    // Clear inputs and selections
    document.getElementById("groupName").value = "";
    selectedCameraIds = [];
    // Set save button action for adding
    document.getElementById("notification-save").onclick = addNomination;
    // Clear filters
    document.getElementById("filterUrl").value = "";
    document.getElementById("filterName").value = "";
    // Clear camera table
    const tableBody = document.querySelector("#addGroupModal table tbody");
    tableBody.innerHTML = "";

    loadCameras();
  });

// Function to add a new nomination
async function addNomination() {
  const nominationName = document.getElementById("groupName").value.trim();
  if (!nominationName) {
    alert("Пожалуйста, введите название номинации.");
    return;
  }

  const selectedCameras = selectedCameraIds;

  const nominationData = {
    name: nominationName,
    cameraIds: selectedCameras,
  };

  try {
    const response = await fetch("/api/nominations", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(nominationData),
    });

    if (response.ok) {
      $("#addGroupModal").modal("hide");
      await loadNominations();
    } else {
      const errorData = await response.json();
      alert(
        `Ошибка при создании номинации: ${
          errorData.Error || response.statusText
        }`
      );
    }
  } catch (error) {
    console.error("Error creating nomination:", error);
  }
}

// Handle edit button click
document.getElementById("table-body").addEventListener("click", (event) => {
  if (event.target.id === "editBtn") {
    const nominationId = event.target.dataset.id;
    openEditModal(nominationId);
  }
});

// Open modal for editing a nomination
async function openEditModal(nominationId) {
  currentNominationId = nominationId;

  // clear filters
  document.getElementById("filterUrl").value = "";
  document.getElementById("filterName").value = "";

  try {
    const response = await fetch(`/api/nominations/${nominationId}`);
    const nomination = await response.json();

    document.getElementById("groupName").value = nomination.name;
    selectedCameraIds = nomination.cameras.map((camera) => camera.id);

    await loadCameras({ nominationId });

    // Set save button action for updating
    document.getElementById("notification-save").onclick = () =>
      updateNomination(nominationId);

    $("#addGroupModal").modal("show");
  } catch (error) {
    console.error("Error loading nomination:", error);
  }
}

// Function to update a nomination
async function updateNomination(nominationId) {
  const nominationName = document.getElementById("groupName").value.trim();
  if (!nominationName) {
    alert("Пожалуйста, введите название номинации.");
    return;
  }

  const selectedCameras = selectedCameraIds;

  const nominationData = {
    name: nominationName,
    cameraIds: selectedCameras,
  };

  try {
    const response = await fetch(`/api/nominations/${nominationId}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(nominationData),
    });

    if (response.ok) {
      $("#addGroupModal").modal("hide");
      await loadNominations();
    } else {
      const errorData = await response.json();
      alert(
        `Ошибка при обновлении номинации: ${
          errorData.Error || response.statusText
        }`
      );
    }
  } catch (error) {
    console.error("Error updating nomination:", error);
  }
}

async function loadNominations() {
  try {
    const response = await fetch("/api/nominations/with-cameras");
    const nominations = await response.json();
    populateNominationsTable(nominations);
  } catch (error) {
    console.error("Error loading nominations:", error);
  }
}

function populateNominationsTable(nominations) {
  const tableBody = document.querySelector("#nominations-list #table-body");
  tableBody.innerHTML = "";

  nominations.forEach((nomination) => {
    const tr = document.createElement("tr");

    const nameTd = document.createElement("td");
    nameTd.textContent = nomination.name;
    tr.appendChild(nameTd);

    const contentTd = document.createElement("td");
    const cameraUl = document.createElement("ul");
    nomination.cameras.forEach((camera) => {
      const cameraLi = document.createElement("li");
      cameraLi.textContent = camera.url;
      cameraLi.dataset.id = camera.id;
      cameraUl.appendChild(cameraLi);
    });
    contentTd.appendChild(cameraUl);
    tr.appendChild(contentTd);

    const actionsTd = document.createElement("td");
    const deleteBtn = document.createElement("button");
    deleteBtn.className = "btn btn-danger btn-sm me-2";
    deleteBtn.textContent = "Удалить";
    deleteBtn.id = "deleteBtn";
    deleteBtn.dataset.id = nomination.id;
    actionsTd.appendChild(deleteBtn);

    const editBtn = document.createElement("button");
    editBtn.className = "btn btn-warning btn-sm";
    editBtn.textContent = "Изменить";
    editBtn.id = "editBtn";
    editBtn.dataset.id = nomination.id;
    actionsTd.appendChild(editBtn);

    tr.appendChild(actionsTd);
    tableBody.appendChild(tr);
  });
}

async function deleteNomination(id) {
  if (confirm("Вы действительно хотите удалить номинацию?")) {
    try {
      await fetch(`/api/nominations/${id}`, { method: "DELETE" });
      await loadNominations();
    } catch (error) {
      console.error("Error deleting nomination:", error);
    }
  }
}

async function loadCameras(filter = {}) {
  let url = "/api/cameras/nominations";

  if (filter.nominationId) {
    url += `/?nominationId=${filter.nominationId}`;
  }

  try {
    const response = await fetch(url);
    const cameras = await response.json();
    populateCameraTable(cameras);
  } catch (error) {
    console.error("Error loading cameras:", error);
  }
}

function populateCameraTable(cameras) {
  const tableBody = document.querySelector("#addGroupModal table tbody");
  tableBody.innerHTML = "";

  cameras.forEach((camera) => {
    const tr = document.createElement("tr");

    const urlTd = document.createElement("td");
    urlTd.textContent = camera.url;
    tr.appendChild(urlTd);

    const nameTd = document.createElement("td");
    nameTd.textContent = camera.name;
    tr.appendChild(nameTd);

    const selectTd = document.createElement("td");
    selectTd.className = "text-center";
    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.value = camera.id;

    if (selectedCameraIds.includes(camera.id)) {
      checkbox.checked = true;
    }

    selectTd.appendChild(checkbox);
    tr.appendChild(selectTd);

    tableBody.appendChild(tr);
  });
}

// Filter cameras based on selected filters
function filterCameras() {
  const urlFilter = document.getElementById("filterUrl").value.toLowerCase();
  const nameFilter = document.getElementById("filterName").value.toLowerCase();
  const rows = document.querySelectorAll("#addGroupModal table tbody tr");

  rows.forEach((row) => {
    const rowUrl = row
      .querySelector("td:nth-child(2)")
      .textContent.toLowerCase();
    const rowName = row
      .querySelector("td:nth-child(3)")
      .textContent.toLowerCase();

    const matchesUrl = rowUrl.includes(urlFilter);
    const matchesName = rowName.includes(nameFilter);

    if (matchesUrl && matchesName) {
      row.style.display = "";
    } else {
      row.style.display = "none";
    }
  });
}

$("#addGroupModal").on("hidden.bs.modal", function () {
  currentNominationId = null;
  selectedCameraIds = [];
});
