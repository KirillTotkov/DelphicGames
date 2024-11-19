const notyf = new Notyf({
  duration: 4000,
  position: {
    x: "right",
    y: "top",
  },
});

let currentNominationId = null;

document.addEventListener("DOMContentLoaded", () => {
  fetchAndRenderNominations();
  document
    .getElementById("submitAddDayBtn")
    .addEventListener("click", handleAddDay);

  document
    .getElementById("addPlatformBtn")
    .addEventListener("click", addPlatform);

  document
    .getElementById("clearPlatformsBtn")
    .addEventListener("click", clearPlatforms);

  const modalElement = document.getElementById("addDayModal");
  modalElement.addEventListener("hidden.bs.modal", () => {
    document.getElementById("addDayForm").reset();
    document.getElementById("addedPlatformsTable").innerHTML = "";
    currentNominationId = null;
  });

  document
    .getElementById("nominations-list")
    .addEventListener("click", (event) => {
      if (event.target && event.target.classList.contains("add-day-btn")) {
        currentNominationId = event.target.getAttribute("data-nomination-id");
      }
    });

  document
    .getElementById("nominations-list")
    .addEventListener("click", async (event) => {
      if (event.target && event.target.classList.contains("btn-danger")) {
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
});

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
        }" class="accordion-collapse collapse"
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
                  .map(
                    (day) => `
                  <tr data-id="${day.id}">
                    <td>${day.day}</td>
                    <td>${nomination.streamUrl}</td>
                    <td>${day.platformName}</td>
                    <td>${day.platformUrl}</td>
                    <td>${day.token}</td>
                    <td>
                        <div class="form-check form-switch">
                            <input class="form-check-input" type="checkbox">
                        </div>
                      </td>
                      <td>
                      <button class="btn btn-danger btn-sm">Удалить</button>
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

function clearPlatforms() {
  document.getElementById("addedPlatformsTable").innerHTML = "";
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
