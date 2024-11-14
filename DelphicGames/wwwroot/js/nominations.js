class PlatformManager {
  constructor() {
    this.platforms = [];
  }

  async loadPlatforms() {
    try {
      const response = await fetch("/api/platforms");
      this.platforms = await response.json();
    } catch (error) {
      console.error("Ошибка загрузки платформ:", error);
    }
  }

  displayPlatformTokens(nominationPlatforms = []) {
    const container = document.getElementById("platformTokensContainer");
    container.innerHTML = "";

    this.platforms.forEach((platform) => {
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
}

class CameraManager {
  constructor() {
    this.selectedCameraIds = [];
    this.cameras = [];
  }

  async loadCameras(filter = {}) {
    let url = "/api/cameras/nominations";

    if (filter.nominationId) {
      url += `/?nominationId=${filter.nominationId}`;
    }

    try {
      const response = await fetch(url);
      this.cameras = await response.json();
      this.populateCameraTable();
    } catch (error) {
      console.error("Error loading cameras:", error);
    }
  }

  populateCameraTable() {
    const tableBody = document.querySelector("#addGroupModal table tbody");
    tableBody.innerHTML = "";

    this.cameras.forEach((camera) => {
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

      if (this.selectedCameraIds.includes(camera.id)) {
        checkbox.checked = true;
      }

      selectTd.appendChild(checkbox);
      tr.appendChild(selectTd);

      tableBody.appendChild(tr);
    });
  }

  handleCheckboxChange(e) {
    if (e.target && e.target.type === "checkbox") {
      const cameraId = parseInt(e.target.value);
      if (e.target.checked) {
        if (!this.selectedCameraIds.includes(cameraId)) {
          this.selectedCameraIds.push(cameraId);
        }
      } else {
        this.selectedCameraIds = this.selectedCameraIds.filter(
          (id) => id !== cameraId
        );
      }
    }
  }
}
class NominationManager {
  constructor() {
    this.currentNominationId = null;
    this.platformManager = new PlatformManager();
    this.cameraManager = new CameraManager();
    this.notyf = new Notyf({
      duration: 4000,
      position: {
        x: "right",
        y: "top",
      },
    });
  }

  async init() {
    await this.loadNominations();

    await this.platformManager.loadPlatforms();
    await this.cameraManager.loadCameras();
    await this.populateFilterDropdowns();

    document.getElementById("table-body").addEventListener("click", (event) => {
      if (event.target.id === "deleteBtn") {
        const id = event.target.dataset.id;
        this.deleteNomination(id);
      } else if (event.target.id === "editBtn") {
        const nominationId = event.target.dataset.id;
        this.openEditModal(nominationId);
      }
    });

    document
      .querySelector('[data-bs-target="#addGroupModal"]')
      .addEventListener("click", async () => {
        this.currentNominationId = null;
        document.getElementById("groupName").value = "";
        document.getElementById("streamUrl").value = "";
        this.cameraManager.selectedCameraIds = [];
        document.getElementById("platformTokensContainer").innerHTML = "";

        await this.platformManager.loadPlatforms();
        await this.cameraManager.loadCameras();
        await this.populateFilterDropdowns();
        this.platformManager.displayPlatformTokens();

        document.getElementById("nominationForm").onsubmit = (e) => {
          e.preventDefault();
          this.addNomination();
        };
      });

    document
      .getElementById("filterUrlModal")
      .addEventListener("change", this.filterCameras);
    document
      .getElementById("filterNameModal")
      .addEventListener("change", this.filterCameras);

    document
      .querySelector("#addGroupModal table tbody")
      .addEventListener("change", (e) =>
        this.cameraManager.handleCheckboxChange(e)
      );
  }

  async loadNominations() {
    try {
      const response = await fetch("/api/nominations/with-cameras");
      const nominations = await response.json();
      this.populateNominationsTable(nominations);
    } catch (error) {
      console.error("Error loading nominations:", error);
    }
  }

  populateNominationsTable(nominations) {
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

  async deleteNomination(id) {
    if (confirm("Вы действительно хотите удалить номинацию?")) {
      try {
        await fetch(`/api/nominations/${id}`, { method: "DELETE" });
        await this.loadNominations();
      } catch (error) {
        console.error("Error deleting nomination:", error);
      }
    }
  }

  async openEditModal(nominationId) {
    this.currentNominationId = nominationId;
    document.getElementById("groupName").value = "";
    document.getElementById("streamUrl").value = "";
    this.cameraManager.selectedCameraIds = [];
    document.getElementById("platformTokensContainer").innerHTML = "";

    try {
      const response = await fetch(`/api/nominations/${nominationId}`);
      const nomination = await response.json();

      document.getElementById("groupName").value = nomination.name;
      document.getElementById("streamUrl").value = nomination.streamUrl || "";
      this.cameraManager.selectedCameraIds = nomination.cameras.map(
        (camera) => camera.id
      );

      await this.platformManager.loadPlatforms();

      this.platformManager.displayPlatformTokens(nomination.platforms);

      await this.cameraManager.loadCameras({ nominationId });
      await this.populateFilterDropdowns();

      document.getElementById("nominationForm").onsubmit = (e) => {
        e.preventDefault();
        this.updateNomination(nominationId);
      };

      $("#addGroupModal").modal("show");
    } catch (error) {
      console.error("Error loading nomination:", error);
    }
  }

  async addNomination() {
    const nominationName = document.getElementById("groupName").value.trim();
    const streamUrl = document.getElementById("streamUrl").value.trim();

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
      cameraIds: this.cameraManager.selectedCameraIds,
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
        await this.loadNominations();
      } else {
        const errorData = await response.json();
        this.notyf.error(errorData.error || "Ошибка при добавлении номинации.");
      }
    } catch (error) {
      console.error("Error adding nomination:", error);
      alert("An error occurred while adding the nomination.");
    }
  }

  async updateNomination(nominationId) {
    const nominationName = document.getElementById("groupName").value.trim();
    const streamUrl = document.getElementById("streamUrl").value.trim();

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
      cameraIds: this.cameraManager.selectedCameraIds,
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
        await this.loadNominations();
      } else {
        const errorData = await response.json();
        alert("Ошибка при обновлении номинации: " + errorData.error);
      }
    } catch (error) {
      console.error("Ошибка при обновлении номинации:", error);
    }
  }

  async populateFilterDropdowns() {
    try {
      const response = await fetch("/api/cameras");
      const cameras = await response.json();

      const filterUrlModal = document.getElementById("filterUrlModal");
      const filterNameModal = document.getElementById("filterNameModal");

      filterUrlModal.length = 1;
      filterNameModal.length = 1;

      const uniqueUrls = [...new Set(cameras.map((camera) => camera.url))];
      const uniqueNames = [...new Set(cameras.map((camera) => camera.name))];

      uniqueUrls.forEach((url) => {
        const option = document.createElement("option");
        option.value = url;
        option.textContent = url;
        filterUrlModal.appendChild(option);
      });

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

  filterCameras() {
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
}

const nominationManager = new NominationManager();
nominationManager.init();
