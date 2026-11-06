// The company page (Views/Companies/Details.cshtml): filter auto-submit, and each job row's "⋯" menu.
// The menu uses the same endpoints as the board (JobApplications/UpdateStatus and DeleteFromBoard).
// Unlike the board, it reloads the page after a change: the numbers, the per-status counts in the
// filter, and the row's place in the filtered, sorted list all come from the server.
(() => {
    // Re-run the search as soon as a dropdown changes.
    const filterForm = document.getElementById("company-filter-form");
    filterForm?.querySelectorAll("select").forEach(select =>
        select.addEventListener("change", () => filterForm.requestSubmit()));

    const table = document.getElementById("company-jobs");
    if (!table)
        return;   // No rows (e.g. nothing matches the filters).

    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const toast = bootstrap.Toast.getOrCreateInstance(document.getElementById("company-toast"));
    const toastMessage = document.getElementById("company-toast-message");

    function showError(message) {
        toastMessage.textContent = message;
        toast.show();
    }

    // POSTs form fields with the anti-forgery token in a header; throws unless the response is OK
    // (or one of `allowedStatuses`).
    async function post(url, fields, allowedStatuses = []) {
        const body = new FormData();
        Object.entries(fields).forEach(([name, value]) => body.append(name, value));
        const response = await fetch(url, {
            method: "POST",
            headers: { "RequestVerificationToken": token },
            body,
        });
        if (!response.ok && !allowedStatuses.includes(response.status))
            throw new Error(`Request to ${url} failed with HTTP ${response.status}`);
    }

    const closeMenu = row =>
        bootstrap.Dropdown.getInstance(row.querySelector('[data-bs-toggle="dropdown"]'))?.hide();

    // ---------- Status changes ----------
    table.addEventListener("click", async event => {
        const item = event.target.closest(".job-card-menu [data-status]");
        if (!item)
            return;

        const row = item.closest(".company-job-row");
        if (item.dataset.status === row.dataset.status)
            return;

        closeMenu(row);
        row.classList.add("is-saving");
        try {
            await post(table.dataset.updateUrl, { ID: row.dataset.id, Status: item.dataset.status });
            location.reload();
        } catch (error) {
            console.error(error);
            row.classList.remove("is-saving");
            showError("Couldn't update the job's status. Please try again.");
        }
    });

    // ---------- Delete ----------
    // Waits for the server before changing the page, since a delete can't be undone.
    const deleteModalElement = document.getElementById("delete-modal");
    const deleteModal = bootstrap.Modal.getOrCreateInstance(deleteModalElement);
    const deleteLabel = document.getElementById("delete-job-label");
    const deleteConfirm = document.getElementById("delete-confirm");
    const deleteSpinner = deleteConfirm.querySelector(".spinner-border");
    let rowToDelete = null;

    table.addEventListener("click", event => {
        const item = event.target.closest('.job-card-menu [data-action="delete"]');
        if (!item)
            return;

        rowToDelete = item.closest(".company-job-row");
        closeMenu(rowToDelete);
        deleteLabel.textContent = rowToDelete.querySelector(".company-job-title").textContent.trim();
        deleteModal.show();
    });

    function setDeleting(deleting) {
        deleteConfirm.disabled = deleting;
        deleteSpinner.classList.toggle("d-none", !deleting);
    }

    deleteConfirm.addEventListener("click", async () => {
        const row = rowToDelete;
        if (!row)
            return;

        setDeleting(true);
        try {
            // 404 means it's already gone (e.g. deleted in another tab), which is fine too.
            await post(table.dataset.deleteUrl, { id: row.dataset.id }, [404]);
            // Deleting a company's last job deletes the company, so this page would be a 404.
            if (Number(table.dataset.total) <= 1)
                location.href = table.dataset.companiesUrl;
            else
                location.reload();
        } catch (error) {
            console.error(error);
            setDeleting(false);
            deleteModal.hide();
            showError("Couldn't delete the job. Please try again.");
        }
    });

    deleteModalElement.addEventListener("hidden.bs.modal", () => { rowToDelete = null; });
})();
