const fetchRegions = async () => {
  try {
    const response = await fetch("/api/regions", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.ok) {
      const regions = await response.json();
      const regionSelect = document.getElementById("addCameraRegion");
      const filterRegionSelect = document.getElementById("filterRegion");

      regions.forEach((region) => {
        const option = document.createElement("option");
        option.value = region.id;
        option.textContent = region.name;
        regionSelect.appendChild(option);

        const filterOption = document.createElement("option");
        filterOption.value = region.id;
        filterOption.textContent = region.name;
        filterRegionSelect.appendChild(filterOption);
      });
    }
  } catch (error) {
    console.error("Error fetching regions:", error);
  }
};

const fetchCities = async (regionId, citySelectId) => {
  try {
    const response = await fetch(`/api/regions/${regionId}/cities`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.ok) {
      const cities = await response.json();
      const citySelect = document.getElementById(citySelectId);
      citySelect.innerHTML = '<option value="">Выберите город</option>';

      cities.forEach((city) => {
        const option = document.createElement("option");
        option.value = city.id;
        option.textContent = city.name;
        citySelect.appendChild(option);
      });
    }
  } catch (error) {
    console.error("Error fetching cities:", error);
  }
};

const createCamera = async () => {
  const regionSelect = document.getElementById("addCameraRegion");
  const citySelect = document.getElementById("addCameraCity");
  const cameraNameInput = document.getElementById("addCameraName");
  const cameraUrlInput = document.getElementById("addCameraUrl");

  const regionId = regionSelect.value;
  const cityId = citySelect.value;
  const cameraName = cameraNameInput.value.trim();
  const cameraUrl = cameraUrlInput.value.trim();

  if (!regionId || !cityId || !cameraName || !cameraUrl) {
    alert("Пожалуйста, заполните все поля.");
    return;
  }

  const createCameraData = {
    city: parseInt(cityId),
    name: cameraName,
    url: cameraUrl,
  };

  try {
    const response = await fetch("/api/cameras", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(createCameraData),
    });

    if (response.ok) {
      await drawCameraTable();
    } else {
      const errorData = await response.json();
      alert(errorData.Error || "Ошибка при создании камеры.");
    }
  } catch (error) {
    console.error("Error creating camera:", error);
  }

  const addCameraModal = bootstrap.Modal.getInstance(
    document.getElementById("addCameraModal")
  );
  addCameraModal.hide();
  regionSelect.value = "";
  citySelect.innerHTML = '<option value="">Выберите город</option>';
  cameraNameInput.value = "";
  cameraUrlInput.value = "";
};

const deleteCameraPlatform = async (id) => {
  if (!confirm("Вы уверены, что хотите удалить камеру?")) {
    return;
  }

  try {
    const response = await fetch(`api/cameraplatform/${id}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.ok) {
      await drawCameraTable();
    }
  } catch (error) {
    console.error("Error deleting camera:", error);
  }
};
const drawCameraTable = async () => {
  try {
    const response = await fetch("api/cameras", {
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

      const cityId = document.createElement("td");
      cityId.textContent = camera.city;
      tr.appendChild(cityId);

      const nameTd = document.createElement("td");
      nameTd.textContent = camera.name;
      tr.appendChild(nameTd);

      const urlTd = document.createElement("td");
      urlTd.textContent = camera.url;
      tr.appendChild(urlTd);

      const actionsTd = document.createElement("td");

      const deleteButton = document.createElement("button");
      deleteButton.type = "button";
      deleteButton.className = "btn btn-danger btn-sm me-2";
      deleteButton.textContent = "Удалить";
      deleteButton.addEventListener("click", () =>
        deleteCameraPlatform(camera.id)
      );
      actionsTd.appendChild(deleteButton);

      const editButton = document.createElement("button");
      editButton.type = "button";
      editButton.className = "btn btn-warning btn-sm edit-camera-button";
      editButton.textContent = "Изменить";
      editButton.dataset.id = camera.id;
      editButton.dataset.url = camera.url;
      editButton.setAttribute("data-bs-toggle", "modal");
      editButton.setAttribute("data-bs-target", "#editCameraModal");
      actionsTd.appendChild(editButton);

      tr.appendChild(actionsTd);
      tbody.appendChild(tr);
    });

    // Attach event listeners after updating the table
    attachEditButtonListeners();
  } catch (error) {
    console.error("Error drawing table:", error);
  }
};
document.addEventListener("DOMContentLoaded", async () => {
  await fetchRegions();
  await drawCameraTable();

  document.getElementById("addCameraRegion").addEventListener("change", (e) => {
    const regionId = e.target.value;
    fetchCities(regionId, "addCameraCity");
  });

  document
    .getElementById("addCameraButton")
    .addEventListener("click", async () => {
      await createCamera();
    });

  document
    .getElementById("saveCameraChanges")
    .addEventListener("click", async () => {
      const cameraId = document.getElementById("editCameraId").value;
      const newUrl = document.getElementById("editCameraUrl").value.trim();

      if (!newUrl) {
        alert("URL не может быть пустым.");
        return;
      }

      try {
        const response = await fetch(`/api/cameras/${cameraId}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ url: newUrl }),
        });

        if (response.ok) {
          await drawCameraTable();
          const editCameraModal = bootstrap.Modal.getInstance(
            document.getElementById("editCameraModal")
          );
          editCameraModal.hide();
        } else {
          const errorData = await response.json();
          alert(errorData.Error || "Ошибка при обновлении камеры.");
        }
      } catch (error) {
        console.error("Error updating camera:", error);
      }
    });
});
const attachEditButtonListeners = () => {
  document.querySelectorAll(".edit-camera-button").forEach((button) => {
    button.removeEventListener("click", handleEditButtonClick);
    button.addEventListener("click", handleEditButtonClick);
  });
};
const handleEditButtonClick = (event) => {
  const cameraId = event.currentTarget.dataset.id;
  const cameraUrl = event.currentTarget.dataset.url;

  document.getElementById("editCameraId").value = cameraId;
  document.getElementById("editCameraUrl").value = cameraUrl;
};
