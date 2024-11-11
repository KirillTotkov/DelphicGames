let nominationChoices;
const notyf = new Notyf({
    duration: 4000,
    position: {
        x: "right",
        y: "top",
    },
});

const choicesOptions = {
    noResultsText: "Нет доступных вариантов",
    noChoicesText: "Нет доступных вариантов для выбора",
    removeItemButton: true,
    searchEnabled: true,
    placeholder: true,
};

async function fetchData() {
    try {
        const [platformsResponse, broadcastsResponse] = await Promise.all([
            fetch("/api/platforms"),
            fetch("/api/streams"),
        ]);

        if (!platformsResponse.ok || !broadcastsResponse.ok) {
            throw new Error("Network response was not ok");
        }

        const platforms = await platformsResponse.json();
        const broadcasts = await broadcastsResponse.json();

        populateTable(platforms, broadcasts);
        populateFilterOptions(broadcasts);
        updateAllHeaderCheckboxes(platforms);
    } catch (error) {
        console.error("Failed to fetch data:", error);
        notyf.error("Не удалось загрузить данные");
    }
}

function populateTable(platforms, broadcasts) {
    const table = document.getElementById("main_table");
    const thead = table.querySelector("thead");
    const tbody = table.querySelector("tbody");

    thead.innerHTML = "";
    tbody.innerHTML = "";

    const headerRow1 = document.createElement("tr");
    const headerRow2 = document.createElement("tr");

    const headers = ["URL", "Номинация"];
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

        ["url", "nomination"].forEach((key) => {
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
                    toggleBroadcast(broadcast.nominationId, platform.id, checkbox.checked);
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

    // Initialize or Reinitialize DataTable
    if ($.fn.DataTable.isDataTable("#main_table")) {
        $("#main_table").DataTable().destroy();
    }

    $("#main_table").DataTable({
        searching: true,
        ordering: true,
        language: {
            info: "Показано c _START_ по _END_ из _TOTAL_ записей",
            lengthMenu: "_MENU_&nbsp;записей на страницу",
            emptyTable: "Нет данных",
            zeroRecords: "Нет совпадений",
            infoEmpty: "",
            infoFiltered: "(отфильтровано из _MAX_ записей)",
            search: "Поиск:",
        },
        columnDefs: [
            {orderable: true, targets: [0, 1]},
            {orderable: false, targets: "_all"},
        ],
    });
}

function populateFilterOptions(broadcasts) {
    const unique = (arr) => [...new Set(arr)].sort();
    
    populateChoices(
        "#nomination_filter",
        unique(broadcasts.map((b) => b.nomination)),
        "Выберите номинацию"
    );
}

function populateChoices(selector, options, placeholder) {
    const select = document.querySelector(selector);
    select.innerHTML = "";

    options.forEach((option) => {
        const opt = document.createElement("option");
        opt.value = option;
        opt.textContent = option;
        select.appendChild(opt);
    });

    const choiceInstance = new Choices(selector, {
        ...choicesOptions,
        placeholderValue: placeholder,
    });

    if (selector === "#nomination_filter") nominationChoices = choiceInstance;
}

async function toggleBroadcast(nominationId, platformId, isActive) {
    const action = isActive ? "start" : "stop";
    const checkbox = document.querySelector(
        `input[data-nomination-id="${nominationId}"][data-platform-id="${platformId}"]`
    );

    checkbox.style.accentColor = "yellow";

    try {
        const response = await fetch(`/api/streams/${action}`, {
            method: "POST",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({nominationId, platformId}),
        });

        if (!response.ok) {
            throw new Error("Network response was not ok");
        }
        checkbox.style.accentColor = "";
    } catch (error) {
        console.error("Toggle stream failed:", error);
        checkbox.style.accentColor = "red";
        notyf.error(
            isActive
                ? `Не удалось запустить трансляцию`
                : "Не удалось остановить трансляцию"
        );
    }
}

async function toggleAllPlatforms(platformId, isChecked) {
    const checkboxes = document.querySelectorAll(
        `input[data-platform-id="${platformId}"]`
    );
    for (const checkbox of checkboxes) {
        if (!checkbox.disabled) {
            checkbox.checked = isChecked;
            await checkbox.dispatchEvent(new Event("change"));
        }
    }
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

function applyFilters() {
    const [nominationValues] = [
        nominationChoices.getValue(true),
    ];

    $.fn.DataTable.ext.search = [];

    $.fn.DataTable.ext.search = [
        function (settings, data) {
            const [, nomination] = data;
            return (
                (nominationValues.length
                    ? nominationValues.includes(nomination)
                    : true)
            );
        },
    ];

    $("#main_table").DataTable().draw();
}

document
    .querySelectorAll("#nomination_filter")
    .forEach((element) => element.addEventListener("change", applyFilters));

document.addEventListener("DOMContentLoaded", fetchData);
