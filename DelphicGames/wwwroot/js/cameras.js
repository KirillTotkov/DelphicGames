const notyf = new Notyf({
  duration: 4000,
  position: {
    x: "right",
    y: "top",
  },
});

let currentCameraId = null;

const createCamera = async () => {
  const cameraNameInput = document.getElementById("addCameraName");
  const cameraUrlInput = document.getElementById("addCameraUrl");

  const cameraName = cameraNameInput.value.trim();
  const cameraUrl = cameraUrlInput.value.trim();

  const createCameraData = {
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
      const addCameraModal = bootstrap.Modal.getInstance(
        document.getElementById("addCameraModal")
      );
      addCameraModal.hide();

      cameraNameInput.value = "";
      cameraUrlInput.value = "";
    } else {
      const errorData = await response.json();
      notyf.error(errorData.error || "Ошибка при создании камеры.");
    }
  } catch (error) {
    console.error("Error creating camera:", error);
  }
};

const deleteCameraPlatform = async (id) => {
  if (!confirm("Вы уверены, что хотите удалить камеру?")) {
    return;
  }

  try {
    const response = await fetch(`api/cameras/${id}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    });

    if (response.ok) {
      await drawCameraTable();
    } else {
      const errorData = await response.json();
      notyf.error(errorData.error || "Ошибка при удалении камеры.");
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
      tr.dataset.id = camera.id;

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
      editButton.setAttribute("data-bs-toggle", "modal");
      editButton.setAttribute("data-bs-target", "#editCameraModal");
      actionsTd.appendChild(editButton);

      tr.appendChild(actionsTd);
      tbody.appendChild(tr);
    });

    attachEditButtonListeners();
  } catch (error) {
    console.error("Error drawing table:", error);
  }
};

const attachEditButtonListeners = () => {
  document.querySelectorAll(".edit-camera-button").forEach((button) => {
    button.removeEventListener("click", handleEditButtonClick);
    button.addEventListener("click", handleEditButtonClick);
  });
};
const handleEditButtonClick = (event) => {
  const tr = event.currentTarget.closest("tr");
  currentCameraId = tr.dataset.id;
  const cameraUrl = tr.querySelector("td:nth-child(2)").textContent;

  document.getElementById("editCameraUrl").value = cameraUrl;
};

document.addEventListener("DOMContentLoaded", async () => {
  await drawCameraTable();

  const addCameraModalElement = document.getElementById("addCameraModal");
  addCameraModalElement.addEventListener("hidden.bs.modal", () => {
    document.getElementById("addCameraForm").reset();
  });

  document
    .getElementById("addCameraForm")
    .addEventListener("submit", async (event) => {
      event.preventDefault();
      await createCamera();
    });

  document
    .getElementById("editCameraForm")
    .addEventListener("submit", async (event) => {
      event.preventDefault();

      const newUrl = document.getElementById("editCameraUrl").value.trim();

      try {
        const response = await fetch(`/api/cameras/${currentCameraId}`, {
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
          currentCameraId = null;
        } else {
          const errorData = await response.json();
          notyf.error(errorData.error || "Ошибка при обновлении камеры.");
        }
      } catch (error) {
        console.error("Error updating camera:", error);
        notyf.error("Ошибка при обновлении камеры.");
      }
    });
});
