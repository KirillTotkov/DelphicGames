class CameraManager {
  constructor() {
    this.selectedCameraIds = [];
    this.cameras = [];
    this.tableBody = document.querySelector("#addGroupModal table tbody");
  }

  async loadCameras(filter = {}) {
    let url = "/api/cameras/nominations";

    if (filter.nominationId) {
      url += `/?nominationId=${filter.nominationId}`;
    }

    try {
      const response = await fetch(url);
      if (!response.ok) throw new Error("Failed to fetch cameras");
      this.cameras = await response.json();
      this.populateCameraTable();
    } catch (error) {
      console.error("Error loading cameras:", error);
    }
  }

  populateCameraTable() {
    this.tableBody.innerHTML = "";

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

      this.tableBody.appendChild(tr);
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
        this.cameraManager.selectedCameraIds = [];

        await this.cameraManager.loadCameras();
        await this.populateFilterDropdowns();

        document.getElementById("nominationForm").onsubmit = (e) => {
          e.preventDefault();
          this.addNomination();
        };
      });

    document
      .getElementById("filterUrlModal")
      .addEventListener("change", (e) => this.filterCameras(e));
    document
      .getElementById("filterNameModal")
      .addEventListener("change", (e) => this.filterCameras(e));

    document
      .querySelector("#addGroupModal table tbody")
      .addEventListener("change", (e) =>
        this.cameraManager.handleCheckboxChange(e)
      );

    document.getElementById("exportExcelBtn").addEventListener("click", () => {
      window.location.href = "/api/nominations/export";
    });

    const searchInput = document.getElementById("searchInput");

    searchInput.addEventListener("input", () => {
      const searchValue = searchInput.value.toLowerCase();
      const tableRows = document.querySelectorAll("#table-body tr");

      tableRows.forEach((row) => {
        const name = row.querySelector("td:first-child").textContent.toLowerCase();
        row.style.display = name.includes(searchValue) ? "" : "none";
      });
    });

  }

  async loadNominations() {
    try {
      const response = await fetch("/api/nominations/with-cameras");
      if (!response.ok) throw new Error("Failed to fetch nominations");
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
        const response = await fetch(`/api/nominations/${id}`, {
          method: "DELETE",
        });
        if (!response.ok) throw new Error("Delete failed");
        await this.loadNominations();
      } catch (error) {
        console.error("Error deleting nomination:", error);
      }
    }
  }

  async openEditModal(nominationId) {
    this.currentNominationId = nominationId;
    this.resetForm();

    try {
      const response = await fetch(`/api/nominations/${nominationId}`);
      if (!response.ok) throw new Error("Failed to fetch nomination");
      const nomination = await response.json();

      document.getElementById("groupName").value = nomination.name;
      this.cameraManager.selectedCameraIds = nomination.cameras.map(
        (camera) => camera.id
      );

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

  resetForm() {
    this.currentNominationId = null;
    document.getElementById("groupName").value = "";
    this.cameraManager.selectedCameraIds = [];
  }

  async addNomination() {
    const nominationData = this.getNominationData();

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
    const nominationData = this.getNominationData();

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
        this.notyf.error(errorData.error || "Ошибка при обновлении номинации.");
      }
    } catch (error) {
      console.error("Ошибка при обновлении номинации:", error);
    }
  }

  getNominationData() {
    return {
      name: document.getElementById("groupName").value.trim(),
      cameraIds: this.cameraManager.selectedCameraIds,
    };
  }

  async populateFilterDropdowns() {
    try {
      const cameras = this.cameraManager.cameras;

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

    const rows = this.cameraManager.tableBody.querySelectorAll("tr");

    rows.forEach((row) => {
      const [url, name] = row.cells;
      const matchesUrl =
        !urlFilter || url.textContent.toLowerCase() === urlFilter;
      const matchesName =
        !nameFilter || name.textContent.toLowerCase() === nameFilter;
      row.style.display = matchesUrl && matchesName ? "" : "none";
    });
  }
}

const nominationManager = new NominationManager();
nominationManager.init();
