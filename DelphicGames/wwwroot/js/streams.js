async function fetchData() {
  const platformsResponse = await fetch("/api/platforms");
  const platforms = await platformsResponse.json();

  const broadcastsResponse = await fetch("/api/streams");
  const broadcasts = await broadcastsResponse.json();

  populateTable(platforms, broadcasts);
  updateAllHeaderCheckboxes(platforms);
}

function populateTable(platforms, broadcasts) {
  const table = document.getElementById("main_table");
  const thead = table.querySelector("thead");
  const tbody = table.querySelector("tbody");

  // Clear existing headers and body
  thead.innerHTML = "";
  tbody.innerHTML = "";

  // Create header rows
  const headerRow1 = document.createElement("tr");
  const headerRow2 = document.createElement("tr");

  // Static headers in Russian
  const headers = ["URL", "Город", "Номинация", "Имя Камеры"];
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

    // Static columns
    ["url", "city", "nomination", "cameraName"].forEach((key) => {
      const td = document.createElement("td");
      td.textContent = broadcast[key];
      tr.appendChild(td);
    });

    // Platform checkboxes
    platforms.forEach((platform) => {
      const td = document.createElement("td");
      const checkbox = document.createElement("input");
      checkbox.type = "checkbox";
      checkbox.dataset.cameraId = broadcast.cameraId;
      checkbox.dataset.platformId = platform.id;

      const platformStatus = broadcast.platformStatuses.find(
        (ps) => ps.platformId === platform.id
      );
      if (!platformStatus) {
        checkbox.disabled = true;
      } else {
        checkbox.checked = platformStatus.isActive;
        checkbox.addEventListener("change", () => {
          toggleBroadcast(broadcast.cameraId, platform.id, checkbox.checked);
          updatePlatformHeaderCheckbox(platform.id);
        });
      }

      td.appendChild(checkbox);
      tr.appendChild(td);
    });

    tbody.appendChild(tr);
  });
}

function toggleBroadcast(cameraId, platformId, isActive) {
  const action = isActive ? "start" : "stop";
  fetch(`/api/streams/${action}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ cameraId, platformId }),
  });
}

function toggleAllPlatforms(platformId, isChecked) {
  const checkboxes = document.querySelectorAll(
    `input[data-platform-id="${platformId}"]`
  );
  checkboxes.forEach((checkbox) => {
    if (!checkbox.disabled) {
      checkbox.checked = isChecked;
      checkbox.dispatchEvent(new Event("change"));
    }
  });
}

function updatePlatformHeaderCheckbox(platformId) {
  const checkboxes = document.querySelectorAll(
    `input[data-platform-id="${platformId}"]`
  );
  const allChecked = Array.from(checkboxes).every(
    (cb) => cb.checked || cb.disabled
  );
  const allDisabled = Array.from(checkboxes).every((cb) => cb.disabled);
  const headerCheckbox = document.getElementById(`${platformId}_all`);
  if (headerCheckbox) {
    headerCheckbox.checked = allChecked;
    headerCheckbox.disabled = allDisabled;
    if (allDisabled) {
      headerCheckbox.checked = false;
    }
  }
}

function updateAllHeaderCheckboxes(platforms) {
  platforms.forEach((platform) => {
    updatePlatformHeaderCheckbox(platform.id);
  });
}

document.addEventListener("DOMContentLoaded", fetchData);
