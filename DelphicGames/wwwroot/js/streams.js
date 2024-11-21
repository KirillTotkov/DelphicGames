const notyf = new Notyf({
  duration: 4000,
  position: {
    x: "right",
    y: "top",
  },
});

let currentNominationId = null;

let isBulkStopping = false;
let openAccordionIds = new Set();

document.addEventListener("DOMContentLoaded", async () => {
  await fetchAndRenderNominations();

  document
    .getElementById("nominations-list")
    .addEventListener("show.bs.collapse", (e) => {
      const accordionId = e.target.id;
      openAccordionIds.add(accordionId);
    });

  document
    .getElementById("nominations-list")
    .addEventListener("hide.bs.collapse", (e) => {
      const accordionId = e.target.id;
      openAccordionIds.delete(accordionId);
    });

  document
    .getElementById("addStreamForm")
    .addEventListener("submit", handleAddStream);

  document
    .getElementById("editStreamForm")
    .addEventListener("submit", handleEditStream);

  const modalElement = document.getElementById("addStreamModal");

  modalElement.addEventListener("hidden.bs.modal", () => {
    document.getElementById("addStreamForm").reset();
    currentNominationId = null;
  });

  document
    .getElementById("nominations-list")
    .addEventListener("click", async (event) => {
      if (event.target && event.target.classList.contains("add-stream-btn")) {
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

async function handleAddStream(event) {
  event.preventDefault();

  const day = document.getElementById("dayDropdown").value;
  const streamUrl = document.getElementById("streamUrlInput").value.trim();
  const platformName = document
    .getElementById("platformNameInput")
    .value.trim();
  const platformUrl = document.getElementById("platformUrlInput").value.trim();
  const token = document.getElementById("tokenInput").value.trim();

  if (!currentNominationId) {
    notyf.error("Номинация не выбрана.");
    return;
  }

  if (!day || !streamUrl) {
    notyf.error("Пожалуйста, заполните день и URL потока.");
    return;
  }

  const streamDto = {
    nominationId: parseInt(currentNominationId),
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
      notyf.success("Трансляция успешно добавлена.");
      await fetchAndRenderNominations();

      const modal = bootstrap.Modal.getInstance(
        document.getElementById("addStreamModal")
      );
      modal.hide();
      document.getElementById("addStreamForm").reset();
    } else {
      const errorData = await response.json();
      notyf.error(errorData.error || "Ошибка при добавлении трансляции.");
    }
  } catch (error) {
    console.error("Error adding stream:", error);
    notyf.error(errorData.error || "Ошибка при добавлении трансляции.");
  }
}

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

  if (!day || !streamUrl) {
    notyf.error("Пожалуйста, заполните день и URL потока.");
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

      const editModalEl = document.getElementById("editStreamModal");
      const editModal = bootstrap.Modal.getInstance(editModalEl);
      editModal.hide();
    } else {
      const error = await response.text();
      notyf.error(error.error || "Ошибка при обновлении трансляции.");
    }
  } catch (error) {
    console.error("Error updating stream:", error);
    notyf.error("Не удалось обновить трансляцию.");
  }
}

function handleStreamStatusChanged(streamStatusDto) {
  const { streamId, status, errorMessage } = streamStatusDto;

  // Находим строку, соответствующую StreamId
  const row = document.querySelector(`tr[data-id="${streamId}"]`);
  if (!row) return;

  const toggleInput = row.querySelector(".stream-toggle");

  if (status === "Error") {
    notyf.error(`Ошибка в трансляции: ${errorMessage}`);

    row.classList.add("table-danger");
    row.classList.remove("table-success");

    toggleInput.checked = false;
  } else if (status === "Completed") {
    if (!isBulkStopping) {
      notyf.success(`Трансляция завершена.`);
    }
    row.classList.remove("table-danger", "table-success");
    toggleInput.checked = false;
  } else if (status === "Running") {
    // row.classList.add("table-success");
    row.classList.remove("table-danger");
    toggleInput.checked = true;
  }
}

async function fetchAndRenderNominations() {
  const data = await fetchData();
  if (data && data.length > 0) {
    document.getElementById("streamsHeader").classList.remove("d-none");
    document.getElementById("streamsHeader").classList.add("d-flex");

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
            ${nomination.nomination ?? ""}
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
                    <td>${stream.day ?? ""}</td>
                    <td>${stream.streamUrl ?? ""}</td>
                    <td>${stream.platformName ?? ""}</td>
                    <td>${stream.platformUrl ?? ""}</td>
                    <td>${stream.token ?? ""}</td>
                    <td>
                      ${
                        stream.platformName &&
                        stream.platformUrl &&
                        stream.token
                          ? `<div class="form-check form-switch">
                              <input class="form-check-input stream-toggle" type="checkbox" ${
                                stream.isActive ? "checked" : ""
                              }>
                            </div>`
                          : ""
                      }
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
  } else {
    document.getElementById("nominations-list").innerHTML = "";

    document.getElementById("streamsHeader").classList.add("d-none");

    const noData = document.getElementById("noNominationsMessage");

    noData.classList.remove("d-none");
    noData.classList.add("d-block");

    return;
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

  openAccordionIds.forEach((id) => {
    const accordion = document.getElementById(id);
    if (accordion) {
      const bsCollapse = new bootstrap.Collapse(accordion, { toggle: false });
      bsCollapse.show();
    }
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

async function getLastStreamUrl(nominationId) {
  try {
    const response = await fetch(`/api/streams/${nominationId}`);
    if (!response.ok) {
      throw new Error("Failed to fetch streams");
    }
    const data = await response.json();
    if (data.length === 0 || data.streams.length == 0) return "";

    const sortedStreams = data.streams.sort((a, b) => b.day - a.day);
    const lastStream = sortedStreams[0];
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
      if (response.ok) {
        notyf.success("Трансляции остановлены.");
      } else {
        return response.json().then((data) => {
          notyf.error(data.error || "Ошибка при остановке трансляций.");
        });
      }
    })
    .catch((error) => {
      notyf.error("Ошибка сервера при остановке трансляций.");
    })
    .finally(() => {
      setTimeout(() => {
        isBulkStopping = false;
      }, 1500);
    });
}
