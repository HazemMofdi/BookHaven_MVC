var dataTable;

$(document).ready(function () {
    loadDataTable();
});


function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/Admin/User/GetAll' },
        "columns":
            [{data: 'name', "width": "25%" },
            { data: 'email', "width": "15%" },
            { data: 'phoneNumber', "width": "10%" },
            { data: 'company.name', "width": "20%" },
            { data: 'role', "width": "10%" },

                {
                    data: { id: "id", lockoutEnd: "lockoutEnd" },
                    render: function (data) {

                        var today = new Date().getTime();
                        var lockout = new Date(data.lockoutEnd).getTime();

                        if (lockout > today) {
                            return `
                                <div class="text-center d-flex justify-content-center gap-2">
                                    <a onclick=LockUnlock('${data.id}') class="btn btn-danger text-white" style="cursor:pointer; width:100px;">
                                        <i class="bi bi-lock-fill"></i> Lock
                                    </a>
                                    <a href="/Admin/User/RoleManagement?id=${data.id}" class="btn btn-primary text-white" style="cursor:pointer; width:150px;">
                                        <i class="bi bi-pencil-square"></i> Permission
                                    </a>
                                </div>  
                                `
                        }
                        else {
                            return `
                                <div class="text-center d-flex justify-content-center gap-2">
                                    <a onclick=LockUnlock('${data.id}') class="btn btn-success text-white" style="cursor:pointer; width:100px;">
                                        <i class="bi bi-unlock-fill"></i> Unlock
                                    </a>
                                    <a href="/Admin/User/RoleManagement?id=${data.id}" class="btn btn-primary text-white" style="cursor:pointer; width:150px;">
                                        <i class="bi bi-pencil-square"></i> Permission
                                    </a>
                                </div>
                                `
                        }
                    },
                    width: "25%"
                }
            ]
    });
}


function LockUnlock(Id) {
    $.ajax({
        type: "POST",
        url: '/Admin/User/LockUnlock',
        data: JSON.stringify(Id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                // Reload data but keep the current page
                dataTable.ajax.reload();
            }
        }
    });
}

