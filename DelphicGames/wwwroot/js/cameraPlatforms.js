async function createCameraPlatform() {
    var inputs = document
        .getElementById('addCameraForm')
        .querySelectorAll('input');

    var region = inputs[0].value;
    var cameraName = inputs[1].value;
    var url = inputs[2].value;
    // var tokenVk = inputs[3].value;
    // var tokenOk = inputs[4].value;
    // var tokenRb = inputs[5].value;
    // var tokenTg = inputs[6].value;

    const createCameraPlatformData = {
        Name: cameraName,
        Url: url,
    }

    try {
        const response = await fetch("api/cameraplatform/CreateCameraPlatform", {
            method: "post",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(createCameraPlatformData)
        });

        if (response.ok) {
            drawCameraTable();
        }
    } catch (error) {
        console.error("Error creating cam:", error);
    }

    $('#addCameraModal').modal('hide');
    inputs[0].value = '';
    inputs[1].value = '';
    inputs[2].value = '';


}

async function deleteCameraPlatform(id) {

    const deleteCameraPlatformData = {
        id: id
    }

    const response = await fetch(`api/cameraplatform/${id}`, {
        method: "delete",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(deleteCameraPlatformData)
    });


    drawCameraTable();
}

async function drawCameraTable() {
    const response = await fetch("api/cameraplatform/GetCameraPlatforms", {
        method: "get",
        headers: {
            "Content-Type": "application/json",
        }
    });

    if (!response.ok) {
        return;
    }

    const json = await response.json();

    $('#cameraTable').find('tbody').empty();
    for (var key in json) {
        var row = json[key];

        $('#cameraTable').find('tbody')
            .append($('<tr>')
                .append($('<td>').text('Город'))
                .append($('<td>').text(row['name']))
                .append($('<td>').text(row['url']))
                .append($('<td>').text(row['tokenVK']))
                .append($('<td>').text(row['tokenOK']))
                .append($('<td>').text(row['tokenRT']))
                .append($('<td>').text(row['tokenTG']))
                .append($('<td>').append(`
                    <button type="button" class="btn btn-warning btn-sm" data-bs-toggle="modal" data-bs-target="#editCameraModal">Изменить</button>
                    <button type="button" class="btn btn-danger btn-sm" onclick="deleteCameraPlatform(${row['id']})">Удалить</button>`
                ))
            );

    }
}

$(document).ready(async function () {

    drawCameraTable();


});
