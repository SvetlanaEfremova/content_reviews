function toggleAllCheckboxes() {
    var selectAll = document.getElementById('selectAll');
    var checkboxes = document.getElementsByName('selectedItem');
    for (var i = 0; i < checkboxes.length; i++) {
        checkboxes[i].checked = selectAll.checked;
    }
}

function getSelectedItems() {
    return Array.from(document.getElementsByName('selectedItem'))
        .filter(checkbox => checkbox.checked)
        .map(checkbox => checkbox.value);
}

function performAction(url) {
    var selectedIds = getSelectedItems();
    fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ selectedUserIds: selectedIds })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                } else {
                    location.reload();
                }
            } else {
                alert(data.message);
            }
        })
        .catch(error => {
            alert('An error occurred.');
        });
}

function blockSelected() {
    performAction("/Admin/Block");
}

function unblockSelected() {
    performAction("/Admin/Unblock");
}

function deleteSelected() {
    performAction("/Admin/Delete");
}

document.getElementById('sortingOption').addEventListener('change', function () {
    const selectedValue = this.value;

    fetch(`/Admin/UsersList?sorting=${selectedValue}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => response.text())
    .then(html => {
        const newTable = document.createElement('div');
        newTable.innerHTML = html;
        const oldTable = document.getElementById('UsersTable');
        if (oldTable) {
            const newTableElement = newTable.querySelector('#UsersTable');
            if (newTableElement) {
                oldTable.parentNode.replaceChild(newTableElement, oldTable);
            } else {
                console.error('New table element not found.');
            }
        } else {
            console.error('Old table element not found.');
        }
    })
    .catch(error => {
        console.error('Error occured while sorting items:', error);
    });
});