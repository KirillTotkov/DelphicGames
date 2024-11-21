class Notifier {
  constructor() {
    this.notyf = new Notyf({
      duration: 4000,
      position: {
        x: "right",
        y: "top",
      },
    });
  }

  success(message) {
    this.notyf.success(message);
  }

  error(message) {
    this.notyf.error(message);
  }
}

class StreamManager {
  constructor() {
    this.currentNominationId = null;
    this.isBulkStopping = false;
    this.openAccordionIds = new Set();
    this.notifier = new Notifier();
    this.connection = null;

    this.initialize();
  }

  async initialize() {
    document.addEventListener("DOMContentLoaded", async () => {
      await this.fetchAndRenderNominations();
      this.setupEventListeners();
      await this.startSignalRConnection();
    });
  }

  setupEventListeners() {
    const nominationsList = document.getElementById("nominations-list");

    nominationsList.addEventListener("show.bs.collapse", (e) => {
      const accordionId = e.target.id;
      this.openAccordionIds.add(accordionId);
    });

    nominationsList.addEventListener("hide.bs.collapse", (e) => {
      const accordionId = e.target.id;
      this.openAccordionIds.delete(accordionId);
    });

    document
      .getElementById("addStreamForm")
      .addEventListener("submit", (e) => this.handleAddStream(e));

    document
      .getElementById("editStreamForm")
      .addEventListener("submit", (e) => this.handleEditStream(e));

    const modalElement = document.getElementById("addStreamModal");
    modalElement.addEventListener("hidden.bs.modal", () => {
      document.getElementById("addStreamForm").reset();
      this.currentNominationId = null;
    });

    nominationsList.addEventListener("click", async (event) => {
      const target = event.target;
      if (target.classList.contains("add-stream-btn")) {
        await this.handleAddStreamButtonClick(target);
      } else if (target.classList.contains("delete-stream-btn")) {
        await this.handleDeleteStream(target);
      } else if (target.classList.contains("change-stream-btn")) {
        this.handleChangeStream(target);
      }
    });

    document
      .getElementById("launchStreamsBtn")
      .addEventListener("click", () => this.launchStreamsForDay());

    document
      .getElementById("stopStreamsBtn")
      .addEventListener("click", () => this.stopStreamsForDay());
  }

  async handleAddStreamButtonClick(target) {
    this.currentNominationId = target.getAttribute("data-nomination-id");
    const lastStreamUrl = await this.getLastStreamUrl(this.currentNominationId);
    if (lastStreamUrl) {
      document.getElementById("streamUrlInput").value = lastStreamUrl;
    }
  }

  async handleAddStream(event) {
    event.preventDefault();

    const day = document.getElementById("dayDropdown").value;
    const streamUrl = document.getElementById("streamUrlInput").value.trim();
    const platformName = document
      .getElementById("platformNameInput")
      .value.trim();
    const platformUrl = document
      .getElementById("platformUrlInput")
      .value.trim();
    const token = document.getElementById("tokenInput").value.trim();

    if (!this.currentNominationId) {
      this.notifier.error("Номинация не выбрана.");
      return;
    }

    if (!day || !streamUrl) {
      this.notifier.error("Пожалуйста, заполните день и URL потока.");
      return;
    }

    const streamDto = {
      nominationId: parseInt(this.currentNominationId),
      streamUrl: streamUrl,
      day: parseInt(day),
      platformName: platformName,
      platformUrl: platformUrl,
      token: token,
    };

    try {
      const response = await fetch("/api/streams", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(streamDto),
      });

      if (response.ok) {
        this.notifier.success("Трансляция успешно добавлена.");
        await this.fetchAndRenderNominations();

        const modal = bootstrap.Modal.getInstance(
          document.getElementById("addStreamModal")
        );
        modal.hide();
        document.getElementById("addStreamForm").reset();
      } else {
        const errorData = await response.json();
        this.notifier.error(
          errorData.error || "Ошибка при добавлении трансляции."
        );
      }
    } catch (error) {
      console.error("Error adding stream:", error);
      this.notifier.error("Ошибка при добавлении трансляции.");
    }
  }

  async startSignalRConnection() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl("/streamHub")
      .build();

    this.connection.on(
      "StreamStatusChanged",
      async (dto) => await this.handleStreamStatusChanged(dto)
    );

    try {
      await this.connection.start();
    } catch (err) {
      console.error("Error connecting to SignalR:", err);
      setTimeout(() => this.startSignalRConnection(), 5000);
    }
  }

  async handleEditStream(event) {
    event.preventDefault();

    const streamId = document.getElementById("editStreamId").value;
    const day = parseInt(document.getElementById("editDayDropdown").value);
    const streamUrl = document
      .getElementById("editStreamUrlInput")
      .value.trim();
    const platformName = document
      .getElementById("editPlatformNameInput")
      .value.trim();
    const platformUrl = document
      .getElementById("editPlatformUrlInput")
      .value.trim();
    const token = document.getElementById("editTokenInput").value.trim();

    if (!day || !streamUrl) {
      this.notifier.error("Пожалуйста, заполните день и URL потока.");
      return;
    }

    const updateDto = {
      Day: day,
      PlatformName: platformName,
      PlatformUrl: platformUrl,
      Token: token,
      StreamUrl: streamUrl,
    };

    try {
      const response = await fetch(`/api/streams/${streamId}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(updateDto),
      });

      if (response.ok) {
        this.notifier.success("Трансляция обновлена успешно.");
        await this.fetchAndRenderNominations();

        const editModalEl = document.getElementById("editStreamModal");
        const editModal = bootstrap.Modal.getInstance(editModalEl);
        editModal.hide();
      } else {
        const error = await response.json();
        this.notifier.error(error.error || "Ошибка при обновлении трансляции.");
      }
    } catch (error) {
      console.error("Error updating stream:", error);
      this.notifier.error("Не удалось обновить трансляцию.");
    }
  }

  async handleStreamStatusChanged(streamStatusDto) {
    const { streamId, status, errorMessage } = streamStatusDto;
    const row = document.querySelector(`tr[data-id="${streamId}"]`);
    if (!row) return;

    const toggleInput = row.querySelector(".stream-toggle");

    if (status === "Error") {
      this.notifier.error(`Ошибка в трансляции: ${errorMessage}`);
      row.classList.add("table-danger");
      row.classList.remove("table-success");
      toggleInput.checked = false;
    } else if (status === "Completed") {
      if (!this.isBulkStopping) {
        this.notifier.success(`Трансляция завершена.`);
      }
      row.classList.remove("table-danger", "table-success");
      toggleInput.checked = false;
    } else if (status === "Running") {
      row.classList.remove("table-danger");
      toggleInput.checked = true;
    }

    const nominationId = row.closest(".accordion-item").getAttribute("data-id");
    await this.updateStreamCount(nominationId);
  }

  async updateStreamCount(nominationId) {
    const nomination = await this.getNominationById(nominationId);
    const runningStreams = this.getRunningStreamsCount(nomination.streams);
    const totalStreams = nomination.streams.length;
    const streamCountBadge = document.getElementById(
      `streamCount-${nominationId}`
    );
    if (streamCountBadge) {
      streamCountBadge.textContent = `Запущено: ${runningStreams} из ${totalStreams}`;
    }
  }

  async getNominationById(nominationId) {
    try {
      const response = await fetch(`/api/streams/nominations/${nominationId}`);
      if (!response.ok) {
        throw new Error("Failed to fetch nomination by ID");
      }
      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Failed to fetch nomination by ID:", error);
      this.notifier.error("Не удалось получить данные номинации");
    }
  }

  async fetchAndRenderNominations() {
    const data = await this.fetchData();
    if (data && data.length > 0) {
      document.getElementById("streamsHeader").classList.remove("d-none");
      document.getElementById("streamsHeader").classList.add("d-flex");

      const nominationsList = document.getElementById("nominations-list");
      nominationsList.innerHTML = "";
      data.forEach((nomination) => {
        const accordionItem = this.createAccordionItem(nomination);
        nominationsList.appendChild(accordionItem);
      });
    } else {
      this.renderNoData();
      return;
    }

    this.populateLaunchDayDropdown(data);
    this.setupStreamToggleListeners();
    this.restoreOpenAccordions();
  }

  createAccordionItem(nomination) {
    const runningStreams = this.getRunningStreamsCount(nomination.streams);
    const totalStreams = nomination.streams.length;

    const accordionItem = document.createElement("div");
    accordionItem.className = "accordion-item";
    accordionItem.dataset.id = nomination.nominationId;
    accordionItem.innerHTML = `
      <h2 class="accordion-header" id="heading${nomination.nominationId}">
        <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse"
          data-bs-target="#collapse${
            nomination.nominationId
          }" aria-expanded="false"
          aria-controls="collapse${nomination.nominationId}">
          ${nomination.nomination ?? ""}
          <span class="badge bg-primary ms-2" id="streamCount-${
            nomination.nominationId
          }">
          Запущено: ${runningStreams} из ${totalStreams}
        </span>
        </button>
      </h2>
      <div id="collapse${
        nomination.nominationId
      }" class="accordion-collapse collapse" data-bs-parent="#nominations-list"
        aria-labelledby="heading${nomination.nominationId}">
        <div class="accordion-body">
          <div class="d-flex justify-content-end mt-1">
            <button class="btn btn-success add-stream-btn" data-nomination-id="${
              nomination.nominationId
            }"
              data-bs-toggle="modal" data-bs-target="#addStreamModal">Добавить трансляцию</button>
          </div>
          <div class="table-responsive">
            <table id="table${
              nomination.nominationId
            }" class="table table-striped table-hover mt-3">
              <thead class="table-light">
                <tr>
                  <th>День</th>
                  <th>URL Потока</th>
                  <th>Платформа</th>
                  <th>URL Платформы</th>
                  <th>Token</th>
                  <th>Статус</th>
                  <th>Действие</th>
                </tr>
              </thead>
              <tbody>
                ${this.createStreamRows(nomination.streams)}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
    return accordionItem;
  }

  getRunningStreamsCount(streams) {
    return streams.filter((stream) => stream.isActive).length;
  }

  createStreamRows(streams) {
    return streams
      .sort((a, b) => a.day - b.day)
      .map(
        (stream) => `
      <tr data-id="${stream.id}">
        <td>${stream.day ?? ""}</td>
        <td>${stream.streamUrl ?? ""}</td>
        <td>${stream.platformName ?? ""}</td>
        <td>${stream.platformUrl ?? ""}</td>
        <td>${stream.token ?? ""}</td>
        <td>
          ${
            stream.platformName && stream.platformUrl && stream.token
              ? `<div class="form-check form-switch">
                  <input class="form-check-input stream-toggle" type="checkbox" ${
                    stream.isActive ? "checked" : ""
                  }>
                </div>`
              : ""
          }
        </td>          
        <td>
          <div class="d-flex flex-wrap gap-2">
            <button class="btn btn-danger btn-sm delete-stream-btn">Удалить</button>
            <button class="btn btn-warning btn-sm change-stream-btn">Изменить</button>
          </div>
        </td>
      </tr>
    `
      )
      .join("");
  }

  renderNoData() {
    document.getElementById("nominations-list").innerHTML = "";
    document.getElementById("streamsHeader").classList.add("d-none");
    const noData = document.getElementById("noNominationsMessage");
    noData.classList.remove("d-none");
    noData.classList.add("d-block");
  }

  populateLaunchDayDropdown(data) {
    const days = data.flatMap((nomination) =>
      nomination.streams.map((stream) => stream.day)
    );
    const uniqueDays = [...new Set(days)].sort((a, b) => a - b);
    const launchDayDropdown = document.getElementById("launchDayDropdown");
    launchDayDropdown.innerHTML = `
      <option value="">--Выберите день--</option>
      ${uniqueDays
        .map((day) => `<option value="${day}">День ${day}</option>`)
        .join("")}
    `;
  }

  setupStreamToggleListeners() {
    const streamToggles = document.querySelectorAll(".stream-toggle");
    streamToggles.forEach((toggle) => {
      toggle.addEventListener("change", (e) => this.handleStreamToggle(e));
    });
  }

  restoreOpenAccordions() {
    this.openAccordionIds.forEach((id) => {
      const accordion = document.getElementById(id);
      if (accordion) {
        const bsCollapse = new bootstrap.Collapse(accordion, { toggle: false });
        bsCollapse.show();
      }
    });
  }

  async fetchData() {
    try {
      const response = await fetch("/api/streams");
      if (!response.ok) {
        throw new Error("Failed to fetch data");
      }
      const data = await response.json();
      return data;
    } catch (error) {
      console.error("Failed to fetch data:", error);
      this.notifier.error("Не удалось загрузить данные");
    }
  }

  async getLastStreamUrl(nominationId) {
    try {
      const response = await fetch(`/api/streams/nominations/${nominationId}`);
      if (!response.ok) {
        throw new Error("Failed to fetch streams");
      }
      const data = await response.json();
      if (data.length === 0 || data.streams.length === 0) return "";

      const sortedStreams = data.streams.sort((a, b) => b.day - a.day);
      const lastStream = sortedStreams[0];
      return lastStream.streamUrl;
    } catch (error) {
      console.error("Error fetching last stream URL:", error);
      return "";
    }
  }

  async handleDeleteStream(target) {
    const row = target.closest("tr");
    const streamId = row.getAttribute("data-id");

    if (confirm("Вы уверены, что хотите удалить эту трансляцию?")) {
      try {
        const response = await fetch(`/api/streams?id=${streamId}`, {
          method: "DELETE",
        });

        if (response.ok) {
          this.notifier.success("Трансляция удалена.");
          row.remove();
        } else {
          const error = await response.json();
          this.notifier.error(error.error || "Ошибка при удалении трансляции.");
        }
      } catch (error) {
        console.error("Error deleting stream:", error);
        this.notifier.error("Ошибка при удалении трансляции.");
      }
    }
  }

  handleChangeStream(target) {
    const row = target.closest("tr");
    const streamId = row.getAttribute("data-id");
    const cells = row.cells;

    document.getElementById("editStreamId").value = streamId;
    document.getElementById("editDayDropdown").value = cells[0].innerText;
    document.getElementById("editStreamUrlInput").value = cells[1].innerText;
    document.getElementById("editPlatformNameInput").value = cells[2].innerText;
    document.getElementById("editPlatformUrlInput").value = cells[3].innerText;
    document.getElementById("editTokenInput").value = cells[4].innerText;

    const editModal = new bootstrap.Modal(
      document.getElementById("editStreamModal")
    );
    editModal.show();
  }

  handleStreamToggle(event) {
    const checkbox = event.target;
    const row = checkbox.closest("tr");
    const streamId = row.getAttribute("data-id");

    if (!streamId) return;

    if (checkbox.checked) {
      this.startStream(checkbox, streamId);
    } else {
      this.stopStream(checkbox, streamId);
    }
  }

  startStream(checkbox, streamId) {
    checkbox.disabled = true;
    fetch(`/api/streams/start/${streamId}`, {
      method: "POST",
    })
      .then((response) => {
        if (response.ok) {
          checkbox.disabled = false;
          this.notifier.success("Трансляция начата.");
        } else {
          throw new Error("Failed to start stream.");
        }
      })
      .catch((error) => {
        console.error(error);
        this.notifier.error("Ошибка при запуске трансляции.");
        checkbox.checked = false;
        checkbox.disabled = false;
      });
  }

  stopStream(checkbox, streamId) {
    checkbox.disabled = true;
    fetch(`/api/streams/stop/${streamId}`, {
      method: "POST",
    })
      .then((response) => {
        if (response.ok) {
          checkbox.disabled = false;
        } else {
          throw new Error("Failed to stop stream.");
        }
      })
      .catch((error) => {
        console.error(error);
        this.notifier.error("Ошибка при остановке трансляции.");
        checkbox.checked = true;
        checkbox.disabled = false;
      });
  }

  launchStreamsForDay() {
    const daySelect = document.getElementById("launchDayDropdown");
    const day = daySelect.value;

    if (!day) {
      alert("Пожалуйста, выберите день.");
      return;
    }

    fetch(`/api/streams/start/day/${day}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((response) =>
        response.json().then((data) => ({
          ok: response.ok,
          data,
        }))
      )
      .then((response) => {
        console.log(response);
        if (response.ok) {
          this.notifier.success(response.data.message);
        } else {
          this.notifier.error(response.data.error || "Ошибка при запуске трансляций.");
        }
      })
      .catch((error) => {
        this.notifier.error("Ошибка при запуске трансляций.");
      });
  }

  stopStreamsForDay() {
    const daySelect = document.getElementById("launchDayDropdown");
    const day = daySelect.value;

    if (!day) {
      alert("Пожалуйста, выберите день.");
      return;
    }

    this.isBulkStopping = true;

    fetch(`/api/streams/stop/day/${day}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((response) => {
        if (response.ok) {
          this.notifier.success("Трансляции остановлены.");
        } else {
          return response.json().then((data) => {
            this.notifier.error(
              data.error || "Ошибка при остановке трансляций."
            );
          });
        }
      })
      .catch((error) => {
        this.notifier.error("Ошибка сервера при остановке трансляций.");
      })
      .finally(() => {
        setTimeout(() => {
          this.isBulkStopping = false;
        }, 1500);
      });
  }
}

new StreamManager();
