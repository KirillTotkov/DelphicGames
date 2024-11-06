document.addEventListener("DOMContentLoaded", async () => {
    await drawCameraTable();

    document.getElementById("saveTokensButton").addEventListener("click", async () => {
        await saveCameraTokens();
    });
});

const drawCameraTable = async () => {
    try {
        const response = await fetch("/api/cameraplatform/GetCameraPlatforms", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
            },
        });
        if (!response.ok) {
            return;
        }
        const cameras = await response.json();

        const tbody = document.querySelector("#cameraTable tbody");
        tbody.innerHTML = "";

        cameras.forEach((camera) => {
            const tr = document.createElement("tr");

            const cityTd = document.createElement("td");
            cityTd.textContent = camera.city || "N/A";
            tr.appendChild(cityTd);

            const nameTd = document.createElement("td");
            nameTd.textContent = camera.name;
            tr.appendChild(nameTd);

            const urlTd = document.createElement("td");
            urlTd.textContent = camera.url;
            tr.appendChild(urlTd);

            const actionsTd = document.createElement("td");
            const editButton = document.createElement("button");
            editButton.classList.add("btn", "btn-primary", "btn-sm");
            editButton.textContent = "Изменить";
            editButton.addEventListener("click", () => openEditTokensModal(camera.id, camera.name));
            actionsTd.appendChild(editButton);
            tr.appendChild(actionsTd);

            tbody.appendChild(tr);
        });

    } catch (error) {
        console.error("Error drawing camera table:", error);
    }
};

const openEditTokensModal = async (cameraId, cameraName) => {
    document.getElementById("cameraId").value = cameraId;
    document.getElementById("editTokensModalLabel").textContent = `Редактирование токенов для камеры "${cameraName}"`;

    // Fetch platforms
    const platformsResponse = await fetch("/api/cameraplatform/platforms", {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    const platforms = await platformsResponse.json();

    // Fetch existing tokens for this camera
    const cameraPlatformsResponse = await fetch(`/api/cameraplatform/cameraplatform/${cameraId}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
    });
    let cameraPlatforms = [];
    if (cameraPlatformsResponse.ok) {
        cameraPlatforms = await cameraPlatformsResponse.json();
    }

    const platformTokensContainer = document.getElementById("platformTokensContainer");
    platformTokensContainer.innerHTML = "";

    platforms.forEach((platform) => {
        const existingToken = cameraPlatforms.find(cp => cp.platformId === platform.id);

        const div = document.createElement("div");
        div.classList.add("mb-3");

        const label = document.createElement("label");
        label.classList.add("form-label");
        label.textContent = platform.name;
        div.appendChild(label);

        const input = document.createElement("input");
        input.type = "text";
        input.name = `platform_${platform.id}`;
        input.dataset.platformId = platform.id;
        input.classList.add("form-control");
        input.value = existingToken ? existingToken.token : "";
        div.appendChild(input);

        platformTokensContainer.appendChild(div);
    });

    // Show modal
    const editTokensModal = new bootstrap.Modal(document.getElementById("editTokensModal"));
    editTokensModal.show();
};

const saveCameraTokens = async () => {
    const cameraId = document.getElementById("cameraId").value;
    const inputs = document.querySelectorAll("#platformTokensContainer input");
    const platformTokens = [];

    inputs.forEach(input => {
        platformTokens.push({
            platformId: parseInt(input.dataset.platformId),
            token: input.value.trim()
        });
    });

    try {
        const response = await fetch(`/api/cameraplatform/cameraplatform/${cameraId}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({ platformTokens }),
        });

        if (response.ok) {
            // Hide modal
            const editTokensModal = bootstrap.Modal.getInstance(document.getElementById("editTokensModal"));
            editTokensModal.hide();
            alert("Токены успешно сохранены.");
        } else {
            const errorData = await response.json();
            alert(errorData.Error || "Ошибка при сохранении токенов.");
        }

    } catch (error) {
        console.error("Error saving camera tokens:", error);
    }
};