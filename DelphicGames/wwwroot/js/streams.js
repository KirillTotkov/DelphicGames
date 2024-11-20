const notyf = new Notyf({
  duration: 4000,
  position: {
    x: "right",
    y: "top",
  },
});

let currentNominationId = null;

let isBulkStopping = false;

document.addEventListener("DOMContentLoaded", async () => {
  fetchAndRenderNominations();
  document
    .getElementById("submitAddDayBtn")
    .addEventListener("click", handleAddDay);

  document
    .getElementById("addPlatformBtn")
    .addEventListener("click", addPlatform);

  document
    .getElementById("editStreamForm")
    .addEventListener("submit", handleEditStream);

  const modalElement = document.getElementById("addDayModal");

  modalElement.addEventListener("hidden.bs.modal", () => {
    document.getElementById("addDayForm").reset();
    document.getElementById("addedPlatformsTable").innerHTML = "";
    currentNominationId = null;
  });

  document
    .getElementById("nominations-list")
    .addEventListener("click", async (event) => {
      if (event.target && event.target.classList.contains("add-day-btn")) {
        currentNominationId = event.target.getAttribute("data-nomination-id");
        const lastStreamUrl = await getLastStreamUrl(currentNominationId);
        if (lastStreamUrl) {
          document.getElementById("streamUrlInput").value = lastStreamUrl;
        }
      }
    });

  document
    .getElementById("nominations-list")
    .addEventListener("click", async (event) => {
      if (
        event.target &&
        event.target.classList.contains("delete-stream-btn")
      ) {
        const row = event.target.closest("tr");
        const streamId = row.getAttribute("data-id");

        if (confirm("Вы уверены, что хотите удалить эту трансляцию?")) {
          try {
            const response = await fetch(`/api/streams?id=${streamId}`, {
              method: "DELETE",
            });

            if (response.ok) {
              notyf.success("Трансляция удалена.");
              row.remove();
            } else {
              const error = await response.text();
              notyf.error(error || "Ошибка при удалении трансляции.");
            }
          } catch (error) {
            console.error("Error deleting stream:", error);
            notyf.error("Ошибка при удалении трансляции.");
          }
        }
      }
    });

  document
    .getElementById("nominations-list")
    .addEventListener("click", async (event) => {
      if (
        event.target &&
        event.target.classList.contains("change-stream-btn")
      ) {
        const row = event.target.closest("tr");
        const streamId = row.getAttribute("data-id");
        const day = row.cells[0].innerText;
        const streamUrl = row.cells[1].innerText;
        const platform = row.cells[2].innerText;
        const platformUrl = row.cells[3].innerText;
        const token = row.cells[4].innerText;

        document.getElementById("editStreamId").value = streamId;
        document.getElementById("editDayDropdown").value = day;
        document.getElementById("editStreamUrlInput").value = streamUrl;
        document.getElementById("editPlatformNameInput").value = platform;
        document.getElementById("editPlatformUrlInput").value = platformUrl;
        document.getElementById("editTokenInput").value = token;

        const editModal = new bootstrap.Modal(
          document.getElementById("editStreamModal")
        );
        editModal.show();
      }
    });

  const launchBtn = document.getElementById("launchStreamsBtn");
  launchBtn.addEventListener("click", launchStreamsForDay);

  const stopBtn = document.getElementById("stopStreamsBtn");
  stopBtn.addEventListener("click", stopStreamsForDay);

  await startSignalRConnection();
});

async function startSignalRConnection() {
  connection = new signalR.HubConnectionBuilder().withUrl("/streamHub").build();

  connection.on("StreamStatusChanged", handleStreamStatusChanged);

  try {
    await connection.start();
  } catch (err) {
    console.error("Error connecting to SignalR:", err);
    setTimeout(startSignalRConnection, 5000);
  }
}

async function handleEditStream(event) {
  event.preventDefault();

  const streamId = document.getElementById("editStreamId").value;
  const day = parseInt(document.getElementById("editDayDropdown").value);
  const streamUrl = document.getElementById("editStreamUrlInput").value.trim();
  const platformName = document
    .getElementById("editPlatformNameInput")
    .value.trim();
  const platformUrl = document
    .getElementById("editPlatformUrlInput")
    .value.trim();
  const token = document.getElementById("editTokenInput").value.trim();

  if (!day || !streamUrl || !platformName || !platformUrl || !token) {
    notyf.error("Пожалуйста, заполните все поля.");
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
      notyf.success("Трансляция обновлена успешно.");
      await fetchAndRenderNominations();

      // Hide the modal
      const editModalEl = document.getElementById("editStreamModal");
      const editModal = bootstrap.Modal.getInstance(editModalEl);
      editModal.hide();
    } else {
      const error = await response.text();
      notyf.error(error);
    }
  } catch (error) {
    console.error("Error updating stream:", error);
    notyf.error("Не удалось обновить трансляцию.");
  }
}

function handleStreamStatusChanged(streamStatusDto) {
  const { streamId, status, errorMessage } = streamStatusDto;

  // Find the row corresponding to the StreamId
  const row = document.querySelector(`tr[data-id="${streamId}"]`);
  if (!row) return;

  const toggleInput = row.querySelector(".stream-toggle");
  console.log(status, errorMessage);

  if (status === "Error") {
    // Show notification
    notyf.error(`Ошибка в трансляции: ${errorMessage}`);

    // Highlight the stream row in red
    row.classList.add("table-danger");
    row.classList.remove("table-success");

    // Update the toggle switch
    toggleInput.checked = false;
  } else if (status === "Completed") {
    // Show notification
    if (!isBulkStopping) {
      notyf.success(`Трансляция завершена.`);
    }

    // Remove any highlighting
    row.classList.remove("table-danger", "table-success");

    // Update the toggle switch
    toggleInput.checked = false;
  } else if (status === "Running") {
    // Highlight the stream row in green

    // row.classList.add("table-success");
    row.classList.remove("table-danger");

    // Update the toggle switch
    toggleInput.checked = true;
  }
}

async function fetchAndRenderNominations() {
  const data = await fetchData();
  if (data) {
    const nominationsList = document.getElementById("nominations-list");
    nominationsList.innerHTML = "";
    data.forEach((nomination) => {
      const accordionItem = document.createElement("div");
      accordionItem.className = "accordion-item";
      accordionItem.innerHTML = `
        <h2 class="accordion-header" id="heading${nomination.nominationId}">
          <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse"
            data-bs-target="#collapse${
              nomination.nominationId
            }" aria-expanded="false"
            aria-controls="collapse${nomination.nominationId}">
            ${nomination.nomination}
          </button>
        </h2>
        <div id="collapse${
          nomination.nominationId
        }" class="accordion-collapse collapse" data-bs-parent="#nominations-list"
          aria-labelledby="heading${nomination.nominationId}">
          <div class="accordion-body">
            <div class="d-flex justify-content-end mt-1">
              <button class="btn btn-success add-day-btn" data-nomination-id="${
                nomination.nominationId
              }"
                data-bs-toggle="modal" data-bs-target="#addDayModal">Добавить день</button>
            </div>
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
                ${nomination.streams
                  .sort((a, b) => a.day - b.day)
                  .map(
                    (stream) => `
                  <tr data-id="${stream.id}">
                    <td>${stream.day}</td>
                    <td>${stream.streamUrl}</td>
                    <td>${stream.platformName}</td>
                    <td>${stream.platformUrl}</td>
                    <td>${stream.token}</td>
                    <td>
                        <div class="form-check form-switch">
                            <input class="form-check-input stream-toggle" type="checkbox" ${
                              stream.isActive ? "checked" : ""
                            }>
                        </div>
                      </td>
                      <td>
                      <button class="btn btn-danger btn-sm delete-stream-btn">Удалить</button>
                      <button class="btn btn-warning btn-sm change-stream-btn ms-2">Изменить</button>
                    </td>
                  </tr>
                `
                  )
                  .join("")}
              </tbody>
            </table>
          </div>
        </div>
      `;
      nominationsList.appendChild(accordionItem);
    });
  }

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

  const streamToggles = document.querySelectorAll(".stream-toggle");
  streamToggles.forEach((toggle) => {
    toggle.addEventListener("change", handleStreamToggle);
  });
}

async function fetchData() {
  try {
    const response = await fetch("/api/streams");
    if (!response.ok) {
      throw new Error("Failed to fetch data");
    }
    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Failed to fetch data:", error);
    notyf.error("Не удалось загрузить данные");
  }
}

function addPlatform() {
  const platformName = document
    .getElementById("platformNameInput")
    .value.trim();
  const platformUrl = document.getElementById("urlInput").value.trim();
  const token = document.getElementById("tokenInput").value.trim();

  if (!platformName || !platformUrl || !token) {
    notyf.error("Пожалуйста, заполните все поля платформы");
    return;
  }

  const table = document.getElementById("addedPlatformsTable");
  const row = table.insertRow();

  row.innerHTML = `
    <td>${platformName}</td>
    <td>${platformUrl}</td>
    <td>${token}</td>
    <td>
      <button class="btn btn-danger btn-sm remove-platform-btn">Удалить</button>
    </td>
  `;

  row.querySelector(".remove-platform-btn").addEventListener("click", () => {
    row.remove();
  });

  // Clear input fields
  document.getElementById("platformNameInput").value = "";
  document.getElementById("urlInput").value = "";
  document.getElementById("tokenInput").value = "";
}

async function handleAddDay(event) {
  event.preventDefault();

  if (!currentNominationId) {
    notyf.error("Номинация не выбрана");
    return;
  }

  const day = document.getElementById("dayDropdown").value;
  if (!day) {
    notyf.error("Пожалуйста, выберите день");
    return;
  }

  const streamUrl = document.getElementById("streamUrlInput").value.trim();

  const table = document.getElementById("addedPlatformsTable");
  const rows = table.querySelectorAll("tr");
  if (rows.length === 0) {
    notyf.error("Пожалуйста, добавьте хотя бы одну платформу");
    return;
  }

  const dayStreams = [];
  rows.forEach((row) => {
    const cols = row.querySelectorAll("td");
    dayStreams.push({
      platformName: cols[0].textContent,
      platformUrl: cols[1].textContent,
      token: cols[2].textContent,
    });
  });

  const dayDto = {
    nominationId: parseInt(currentNominationId),
    streamUrl: streamUrl,
    day: parseInt(day),
    dayStreams: dayStreams,
  };

  try {
    const response = await fetch("/api/streams", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(dayDto),
    });

    if (response.ok) {
      notyf.success("День добавлен успешно");
      await fetchAndRenderNominations();

      // Open the accordion for the current nominationId
      const collapseElement = document.getElementById(
        `collapse${currentNominationId}`
      );
      const bsCollapse = new bootstrap.Collapse(collapseElement, {
        toggle: true,
      });
      $("#addDayModal").modal("hide");

      // Reset the form
      document.getElementById("addDayForm").reset();
      clearPlatforms();
      currentNominationId = null;
    } else {
      const error = await response.text();
      notyf.error(error);
    }
  } catch (error) {
    console.error("Error adding day:", error);
    notyf.error("Ошибка при добавлении дня");
  }
}

function clearPlatforms() {
  document.getElementById("addedPlatformsTable").innerHTML = "";
}

async function getLastStreamUrl(nominationId) {
  try {
    const response = await fetch(`/api/streams/${nominationId}`);
    if (!response.ok) {
      throw new Error("Failed to fetch streams");
    }
    const data = await response.json();
    if (data.length === 0 || data.streams.length == 0) return "";
    const lastStream = data.streams[data.streams.length - 1];
    return lastStream.streamUrl;
  } catch (error) {
    console.error("Error fetching last stream URL:", error);
    return "";
  }
}

function handleStreamToggle(event) {
  const checkbox = event.target;
  const row = checkbox.closest("tr");
  const streamId = row.getAttribute("data-id");

  if (!streamId) return;

  if (checkbox.checked) {
    // Start the stream

    checkbox.disabled = true;
    fetch(`/api/streams/start/${streamId}`, {
      method: "POST",
    })
      .then((response) => {
        if (response.ok) {
          checkbox.disabled = false;
          notyf.success("Трансляция начата.");
        } else {
          throw new Error("Failed to start stream.");
        }
      })
      .catch((error) => {
        console.error(error);
        notyf.error("Ошибка при запуске трансляции.");
        // Revert the checkbox state
        checkbox.checked = false;
      });
  } else {
    // Stop the stream

    checkbox.disabled = true;
    fetch(`/api/streams/stop/${streamId}`, {
      method: "POST",
    })
      .then((response) => {
        if (response.ok) {
          // notyf.success("Трансляция остановлена.");
          checkbox.disabled = false;
        } else {
          checkbox.disabled = false;
          throw new Error("Failed to stop stream.");
        }
      })
      .catch((error) => {
        console.error(error);
        notyf.error("Ошибка при остановке трансляции.");
        // Revert the checkbox state
        checkbox.checked = true;
        checkbox.disabled = false;
      });
  }
}

function launchStreamsForDay() {
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
    .then((response) => {
      if (response.ok) {
        notyf.success("Трансляции запущены.");
      } else {
        return response.json().then((data) => {
          notyf.error(data.error || "Ошибка при запуске трансляций.");
        });
      }
    })
    .catch((error) => {
      notyf.error(error.error || "Ошибка при запуске трансляций.");
    });
}

function stopStreamsForDay() {
  const daySelect = document.getElementById("launchDayDropdown");
  const day = daySelect.value;

  if (!day) {
    alert("Пожалуйста, выберите день.");
    return;
  }

  isBulkStopping = true;

  fetch(`/api/streams/stop/day/${day}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
  })
    .then((response) => {
      isBulkStopping = false;
      if (response.ok) {
        notyf.success("Трансляции остановлены.");
      } else {
        return response.json().then((data) => {
          notyf.error(data.error || "Ошибка при остановке трансляций.");
        });
      }
    })
    .catch((error) => {
      isBulkStopping = false;
      notyf.error(error.error || "Ошибка при остановке трансляций.");
    });
}
