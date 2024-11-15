const notyf = new Notyf({
  duration: 4000,
  position: {
    x: "right",
    y: "top",
  },
});

const pendingStreams = {};

async function fetchData() {
  try {
    const [platforms, broadcasts] = await Promise.all([
      fetchJson("/api/platforms"),
      fetchJson("/api/streams"),
    ]);
    populateTable(platforms, broadcasts);
    updateAllHeaderCheckboxes(platforms);
  } catch (error) {
    console.error("Failed to fetch data:", error);
    notyf.error("Не удалось загрузить данные");
  }
}

async function fetchJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error("Network response was not ok");
  return response.json();
}

function populateTable(platforms, broadcasts) {
  const table = document.getElementById("main_table");
  const thead = table.querySelector("thead");
  const tbody = table.querySelector("tbody");

  thead.innerHTML = "";
  tbody.innerHTML = "";

  const headerRow1 = document.createElement("tr");
  const headerRow2 = document.createElement("tr");

  const headers = ["Номинация", "URL"];
  headers.forEach((text) => {
    const th = document.createElement("th");
    th.rowSpan = 2;
    th.textContent = text;
    headerRow1.appendChild(th);
  });

  // Platform headers
  platforms.forEach((platform) => {
    const th = document.createElement("th");
    th.textContent = platform.name;
    headerRow1.appendChild(th);

    const thCheckbox = document.createElement("th");
    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.id = `${platform.id}_all`;
    checkbox.addEventListener("change", () =>
      toggleAllPlatforms(platform.id, checkbox.checked)
    );
    thCheckbox.appendChild(checkbox);
    headerRow2.appendChild(thCheckbox);
  });

  thead.appendChild(headerRow1);
  thead.appendChild(headerRow2);

  // Populate table body
  broadcasts.forEach((broadcast) => {
    const tr = document.createElement("tr");

    ["nomination", "url"].forEach((key) => {
      const td = document.createElement("td");
      td.textContent = broadcast[key];
      tr.appendChild(td);
    });

    platforms.forEach((platform) => {
      const td = document.createElement("td");
      const checkbox = document.createElement("input");
      checkbox.type = "checkbox";
      checkbox.dataset.nominationId = broadcast.nominationId;
      checkbox.dataset.platformId = platform.id;

      const platformStatus = broadcast.platformStatuses.find(
        (ps) => ps.platformId === platform.id
      );
      if (platformStatus) {
        checkbox.checked = platformStatus.isActive;
        checkbox.addEventListener("change", () => {
          toggleBroadcast(
            broadcast.nominationId,
            platform.id,
            checkbox.checked,
            checkbox
          );
          updatePlatformHeaderCheckbox(platform.id);
        });
      } else {
        checkbox.disabled = true;
      }

      td.appendChild(checkbox);
      tr.appendChild(td);
    });

    tbody.appendChild(tr);
  });

  initializeDataTable();
}

function initializeDataTable() {
  if ($.fn.DataTable.isDataTable("#main_table")) {
    $("#main_table").DataTable().destroy();
  }

  $("#main_table").DataTable({
    searching: true,
    ordering: true,
    language: {
      info: "Показано c _START_ по _END_ из _TOTAL_ записей",
      lengthMenu: "_MENU_ записей на страницу",
      emptyTable: "Нет данных",
      zeroRecords: "Нет совпадений",
      infoEmpty: "",
      infoFiltered: "(отфильтровано из _MAX_ записей)",
      search: "Поиск:",
    },
    columnDefs: [
      { orderable: true, targets: [0, 1] },
      { orderable: false, targets: "_all" },
    ],
  });
}

async function toggleBroadcast(nominationId, platformId, isActive, checkbox) {
  const action = isActive ? "start" : "stop";
  const key = `${nominationId}_${platformId}`;

  if (pendingStreams[key] === "starting" && !isActive) {
    notyf.warning(
      "Трансляция запускается, дождитесь её запуска перед остановкой."
    );
    checkbox.checked = true;
    return;
  }

  pendingStreams[key] = isActive ? "starting" : "stopping";
  checkbox.disabled = true;
  checkbox.style.accentColor = "yellow";

  updatePlatformHeaderCheckbox(platformId);

  try {
    const response = await fetch(`/api/streams/${action}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ nominationId, platformId }),
    });

    if (!response.ok) {
      throw new Error("Network response was not ok");
    }

    checkbox.style.accentColor = "";
    checkbox.disabled = false;
    checkbox.checked = isActive;
    updatePlatformHeaderCheckbox(platformId);
  } catch (error) {
    console.error("Toggle stream failed:", error);
    checkbox.style.accentColor = "red";
    checkbox.checked = !isActive;
    notyf.error(
      isActive
        ? "Не удалось запустить трансляцию"
        : "Не удалось остановить трансляцию"
    );
  } finally {
    checkbox.disabled = false;
    delete pendingStreams[key];
    updatePlatformHeaderCheckbox(platformId);
  }
}

async function toggleAllPlatforms(platformId, isChecked) {
  const table = $("#main_table").DataTable();
  const checkboxes = table
    .$("input[data-platform-id='" + platformId + "']", { page: "all" })
    .toArray();

  const promises = checkboxes
    .filter((cb) => cb.checked !== isChecked && !cb.disabled)
    .map((cb) => {
      const nominationId = cb.dataset.nominationId;
      const key = `${nominationId}_${platformId}`;
      pendingStreams[key] = isChecked ? "starting" : "stopping";
      cb.checked = isChecked;
      return toggleBroadcast(nominationId, platformId, isChecked, cb);
    });

  try {
    await Promise.all(promises);
  } catch (error) {
    console.error("Toggle all streams failed:", error);
    notyf.error("Произошла ошибка при обработке трансляций");
  }
}

function updatePlatformHeaderCheckbox(platformId) {
  const table = $("#main_table").DataTable();
  const checkboxes = table
    .$("input[data-platform-id='" + platformId + "']", { page: "all" })
    .toArray();

  const allChecked = checkboxes.every((cb) => cb.checked || cb.disabled);
  const allDisabled = checkboxes.every((cb) => cb.disabled);
  const waiting = checkboxes.some(
    (cb) => pendingStreams[`${cb.dataset.nominationId}_${platformId}`]
  );

  const headerCheckbox = document.getElementById(`${platformId}_all`);

  if (headerCheckbox) {
    headerCheckbox.checked = allChecked;
    headerCheckbox.disabled = allDisabled;
    if (allDisabled) {
      headerCheckbox.checked = false;
    }

    if (waiting) {
      headerCheckbox.disabled = true;
    }
  }
}

function updateAllHeaderCheckboxes(platforms) {
  platforms.forEach((platform) => {
    updatePlatformHeaderCheckbox(platform.id);
  });
}

document.addEventListener("DOMContentLoaded", fetchData);
