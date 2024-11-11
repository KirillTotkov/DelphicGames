let currentNominationId = null;
let selectedCameraIds = [];
let platforms = [];

async function loadPlatforms() {
  try {
    const response = await fetch("/api/platforms");
    platforms = await response.json();
  } catch (error) {
    console.error("Ошибка загрузки платформ:", error);
  }
}

function displayPlatformTokens(nominationPlatforms = []) {
  const container = document.getElementById("platformTokensContainer");
  container.innerHTML = "";

  platforms.forEach((platform) => {
    const div = document.createElement("div");
    div.classList.add("mb-2");

    const label = document.createElement("label");
    label.textContent = platform.name;
    label.classList.add("form-label");

    const input = document.createElement("input");
    input.type = "text";
    input.classList.add("form-control");
    input.name = `platformToken_${platform.id}`;
    input.dataset.platformId = platform.id;

    // Если редактируем номинацию, предзаполняем токен
    const existingPlatform = nominationPlatforms.find(
      (np) => np.platformId === platform.id
    );
    if (existingPlatform) {
      input.value = existingPlatform.token || "";
    }

    div.appendChild(label);
    div.appendChild(input);

    container.appendChild(div);
  });
}

document.addEventListener("DOMContentLoaded", async () => {
  await loadNominations();

  // Delete nomination
  document.getElementById("table-body").addEventListener("click", (event) => {
    if (event.target.id === "deleteBtn") {
      const id = event.target.dataset.id;
      deleteNomination(id);
    }
  });

  // Filter cameras by URL or name using dropdowns
  document
    .getElementById("filterUrlModal")
    .addEventListener("change", filterCameras);
  document
    .getElementById("filterNameModal")
    .addEventListener("change", filterCameras);

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

// Обработчик открытия модального окна для добавления номинации
document
  .querySelector('[data-bs-target="#addGroupModal"]')
  .addEventListener("click", async () => {
    currentNominationId = null;
    document.getElementById("groupName").value = "";
    document.getElementById("streamUrl").value = "";
    selectedCameraIds = [];
    document.getElementById("platformTokensContainer").innerHTML = "";

    await loadPlatforms();
    await loadCameras();
    await populateFilterDropdowns();
    displayPlatformTokens(await loadPlatforms());

    // Устанавливаем обработчик сохранения для добавления
    document.getElementById("notification-save").onclick = addNomination;
  });

async function populateFilterDropdowns() {
  try {
    const response = await fetch("/api/cameras");
    const cameras = await response.json();

    const filterUrlModal = document.getElementById("filterUrlModal");
    const filterNameModal = document.getElementById("filterNameModal");

    // Get unique URLs and camera names
    const uniqueUrls = [...new Set(cameras.map((camera) => camera.url))];
    const uniqueNames = [...new Set(cameras.map((camera) => camera.name))];

    // Populate URL filter
    uniqueUrls.forEach((url) => {
      const option = document.createElement("option");
      option.value = url;
      option.textContent = url;
      filterUrlModal.appendChild(option);
    });

    // Populate camera name filter
    uniqueNames.forEach((name) => {
      const option = document.createElement("option");
      option.value = name;
      option.textContent = name;
      filterNameModal.appendChild(option);
    });
  } catch (error) {
    console.error("Error populating filter dropdowns:", error);
  }
}

async function addNomination() {
  const nominationName = document.getElementById("groupName").value.trim();
  const streamUrl = document.getElementById("streamUrl").value.trim();

  if (!nominationName || !streamUrl) {
    alert("Пожалуйста, заполните все поля.");
    return;
  }

  const platformTokens = [];
  document
    .querySelectorAll("#platformTokensContainer input")
    .forEach((input) => {
      const token = input.value.trim();
      if (token) {
        platformTokens.push({
          platformId: parseInt(input.dataset.platformId),
          token: token,
        });
      }
    });

  const nominationData = {
    name: nominationName,
    streamUrl: streamUrl,
    cameraIds: selectedCameraIds,
    platforms: platformTokens,
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

// Обработчик кнопки редактирования номинации
document
  .getElementById("table-body")
  .addEventListener("click", async (event) => {
    if (event.target.id === "editBtn") {
      const nominationId = event.target.dataset.id;
      await openEditModal(nominationId);
    }
  });

async function openEditModal(nominationId) {
  currentNominationId = nominationId;
  document.getElementById("groupName").value = "";
  document.getElementById("streamUrl").value = "";
  selectedCameraIds = [];
  document.getElementById("platformTokensContainer").innerHTML = "";

  try {
    const response = await fetch(`/api/nominations/${nominationId}`);
    const nomination = await response.json();

    document.getElementById("groupName").value = nomination.name;
    document.getElementById("streamUrl").value = nomination.streamUrl || "";
    selectedCameraIds = nomination.cameras.map((camera) => camera.id);

    await loadPlatforms();

    // Отображаем платформы с предзаполненными токенами
    displayPlatformTokens(nomination.platforms);

    await loadCameras({ nominationId });
    await populateFilterDropdowns();

    document.getElementById("notification-save").onclick = () =>
      updateNomination(nominationId);

    $("#addGroupModal").modal("show");
  } catch (error) {
    console.error("Error loading nomination:", error);
  }
}

async function updateNomination(nominationId) {
  const nominationName = document.getElementById("groupName").value.trim();
  const streamUrl = document.getElementById("streamUrl").value.trim();

  if (!nominationName || !streamUrl) {
    alert("Пожалуйста, заполните все поля.");
    return;
  }

  const platformTokens = [];
  document
    .querySelectorAll("#platformTokensContainer input")
    .forEach((input) => {
      const token = input.value.trim();
      if (token) {
        platformTokens.push({
          platformId: parseInt(input.dataset.platformId),
          token: token,
        });
      }
    });

  const nominationData = {
    name: nominationName,
    streamUrl: streamUrl,
    cameraIds: selectedCameraIds,
    platforms: platformTokens,
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
      alert("Ошибка при обновлении номинации: " + errorData.error);
    }
  } catch (error) {
    console.error("Ошибка при обновлении номинации:", error);
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

    const streamTd = document.createElement("td");
    streamTd.textContent = nomination.streamUrl;
    tr.appendChild(streamTd);

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

// Filter cameras based on selected dropdown values
function filterCameras() {
  const urlFilter = document
    .getElementById("filterUrlModal")
    .value.toLowerCase();
  const nameFilter = document
    .getElementById("filterNameModal")
    .value.toLowerCase();
  const rows = document.querySelectorAll("#addGroupModal table tbody tr");

  rows.forEach((row) => {
    const url = row.cells[0].textContent.toLowerCase();
    const name = row.cells[1].textContent.toLowerCase();

    const matchesUrl = !urlFilter || url === urlFilter;
    const matchesName = !nameFilter || name === nameFilter;
    row.style.display = matchesUrl && matchesName ? "" : "none";
  });
}

$("#addGroupModal").on("hidden.bs.modal", function () {
  currentNominationId = null;
  selectedCameraIds = [];
  document.getElementById("filterUrlModal").options.length = 1;
  document.getElementById("filterNameModal").options.length = 1;
});
