// Status changes on the job board (Views/JobApplications/Index.cshtml), by drag-and-drop or by
// each card's status menu. Both move the card immediately (optimistic update), save via POST to
// UpdateStatus, and put the card back if the save fails. The menu can also delete a job (DeleteFromBoard).
(() => {
    const board = document.getElementById("job-board");
    if (!board)
        return;

    const updateUrl = board.dataset.updateUrl;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const archivedSection = document.getElementById("archived-section");
    const archivedList = document.getElementById("archived-list");
    const toast = bootstrap.Toast.getOrCreateInstance(document.getElementById("board-toast"));
    const toastMessage = document.getElementById("board-toast-message");

    function showError(message) {
        toastMessage.textContent = message;
        toast.show();
    }

    // Time the last real drag ended, used to swallow the click that follows a drop.
    let lastDragEnd = 0;
    let isDragging = false;

    const statusClass = status => "status-" + status.toLowerCase();

    function setCardStatus(card, status, label) {
        card.classList.remove(statusClass(card.dataset.status));
        card.classList.add(statusClass(status));
        card.dataset.status = status;
        card.querySelector(".status-badge").textContent = label;

        // Keep the card's menu in sync: the current status is highlighted and can't be picked.
        card.querySelectorAll(".job-card-menu [data-status]").forEach(item => {
            const isCurrent = item.dataset.status === status;
            item.classList.toggle("active", isCurrent);
            item.disabled = isCurrent;
            if (isCurrent)
                item.setAttribute("aria-current", "true");
            else
                item.removeAttribute("aria-current");
        });
    }

    // The list a card with this status belongs in.
    const listFor = status =>
        status === "Archived" ? archivedList : board.querySelector(`.board-list[data-status="${status}"]`);

    // Recomputes column counts, empty placeholders and Archived visibility from the DOM.
    function refreshCounts() {
        board.querySelectorAll(".board-column").forEach(column => {
            const count = column.querySelectorAll(".job-card").length;
            column.querySelector(".board-column-count").textContent = count;
            column.querySelector(".board-column-empty").classList.toggle("d-none", count > 0);
        });
        archivedSection.classList.toggle("d-none", !archivedList.querySelector(".job-card"));
    }

    async function saveStatus(id, status) {
        const body = new FormData();
        body.append("ID", id);
        body.append("Status", status);
        const response = await fetch(updateUrl, {
            method: "POST",
            headers: { "RequestVerificationToken": token },
            body,
        });
        if (!response.ok)
            throw new Error(`Status update failed with HTTP ${response.status}`);
    }

    // Shared by drag and menu. `origin` = { list, next }: where to put the card back if saving fails.
    async function moveCard(card, status, label, origin) {
        const ageLabel = card.querySelector(".job-card-age");
        const previous = {
            status: card.dataset.status,
            label: card.querySelector(".status-badge").textContent,
            age: ageLabel.textContent,
        };

        // Update the UI first so the move feels instant. Lists are ordered by last update,
        // so a moved card goes to the top of its new list.
        if (status !== previous.status)
            ageLabel.textContent = "Today";
        setCardStatus(card, status, label);
        listFor(status).prepend(card);
        refreshCounts();
        card.classList.add("is-saving");

        try {
            await saveStatus(card.dataset.id, status);
        } catch (error) {
            console.error(error);
            setCardStatus(card, previous.status, previous.label);
            ageLabel.textContent = previous.age;
            origin.list.insertBefore(card, origin.next?.parentNode === origin.list ? origin.next : null);
            refreshCounts();
            showError("Couldn't update the job's status. Please try again.");
        } finally {
            card.classList.remove("is-saving");
        }
    }

    // Briefly highlights a card so you can see where it landed (menu moves have no drag animation).
    function highlight(card) {
        card.classList.remove("is-moved");
        void card.offsetWidth;   // Restart the animation if it's already running.
        card.classList.add("is-moved");
        card.addEventListener("animationend", () => card.classList.remove("is-moved"), { once: true });
    }

    // Fires on the list a card was dropped into (columns and the Archive drop zone).
    function onAdd(evt) {
        const origin = { list: evt.from, next: evt.from.children[evt.oldIndex] ?? null };
        moveCard(evt.item, evt.to.dataset.status, evt.to.dataset.statusLabel, origin);
    }

    // Status menu: picking an item moves the card just like a drop would.
    document.addEventListener("click", event => {
        const item = event.target.closest(".job-card-menu [data-status]");
        if (!item)
            return;

        const card = item.closest(".job-card");
        if (item.dataset.status === card.dataset.status)
            return;

        bootstrap.Dropdown.getInstance(card.querySelector('[data-bs-toggle="dropdown"]'))?.hide();
        const origin = { list: card.parentElement, next: card.nextElementSibling };
        moveCard(card, item.dataset.status, item.dataset.statusLabel, origin);
        highlight(card);
    });

    // ---------- Delete ----------
    // Unlike status changes, deleting waits for the server before removing the card:
    // it can't be undone, so the card shouldn't vanish unless the delete really happened.
    const deleteModalElement = document.getElementById("delete-modal");
    const deleteModal = bootstrap.Modal.getOrCreateInstance(deleteModalElement);
    const deleteLabel = document.getElementById("delete-job-label");
    const deleteConfirm = document.getElementById("delete-confirm");
    const deleteSpinner = deleteConfirm.querySelector(".spinner-border");
    let cardToDelete = null;

    // Menu → "Delete…": open the confirmation for that card.
    document.addEventListener("click", event => {
        const item = event.target.closest('.job-card-menu [data-action="delete"]');
        if (!item)
            return;

        cardToDelete = item.closest(".job-card");
        bootstrap.Dropdown.getInstance(cardToDelete.querySelector('[data-bs-toggle="dropdown"]'))?.hide();
        const title = cardToDelete.querySelector(".job-card-title").textContent.trim();
        const company = cardToDelete.querySelector(".job-card-company").textContent.trim();
        deleteLabel.textContent = `${title} at ${company}`;
        deleteModal.show();
    });

    function setDeleting(deleting) {
        deleteConfirm.disabled = deleting;
        deleteSpinner.classList.toggle("d-none", !deleting);
    }

    deleteConfirm.addEventListener("click", async () => {
        const card = cardToDelete;
        if (!card)
            return;

        setDeleting(true);
        try {
            const body = new FormData();
            body.append("id", card.dataset.id);
            const response = await fetch(board.dataset.deleteUrl, {
                method: "POST",
                headers: { "RequestVerificationToken": token },
                body,
            });
            // 404 means it's already gone (e.g. deleted in another tab), so remove the card anyway.
            if (!response.ok && response.status !== 404)
                throw new Error(`Delete failed with HTTP ${response.status}`);

            deleteModal.hide();
            removeCard(card);
        } catch (error) {
            console.error(error);
            deleteModal.hide();
            showError("Couldn't delete the job. Please try again.");
        } finally {
            setDeleting(false);
        }
    });

    deleteModalElement.addEventListener("hidden.bs.modal", () => { cardToDelete = null; });

    // Fades the card out, then removes it and updates the counts.
    function removeCard(card) {
        let removed = false;
        const finish = () => {
            if (removed)
                return;
            removed = true;
            card.remove();
            refreshCounts();
        };
        card.classList.add("is-removing");
        card.addEventListener("animationend", finish, { once: true });
        setTimeout(finish, 400);   // In case the animation doesn't run (e.g. reduced motion).
    }

    const shared = {
        sort: false,                 // No manual ordering within a list.
        draggable: ".job-card",      // Placeholders and labels aren't draggable items.
        filter: ".job-card-menu",    // Pressing the status menu doesn't start a drag...
        preventOnFilter: false,      // ...and its click still goes through to open the menu.
        animation: 150,
        forceFallback: true,         // Consistent drag visuals across browsers.
        fallbackOnBody: true,        // Attach the floating copy to <body> so columns can't clip or offset it.
        fallbackTolerance: 4,        // A small wobble during a click isn't a drag.
        delay: 150,                  // On touch, a short press starts a drag so scrolling still works.
        delayOnTouchOnly: true,
        ghostClass: "job-card-ghost",
        chosenClass: "job-card-chosen",
        fallbackClass: "job-card-floating",
        onStart: () => {
            isDragging = true;
            document.body.classList.add("is-dragging-job");
        },
        onEnd: () => {
            document.body.classList.remove("is-dragging-job");
            if (isDragging)
                lastDragEnd = Date.now();
            isDragging = false;
        },
    };

    board.querySelectorAll(".board-list").forEach(list =>
        Sortable.create(list, { ...shared, group: { name: "jobs" }, onAdd }));

    // Archived cards can be dragged out, but not dropped back in directly (use a drop zone).
    Sortable.create(archivedList, { ...shared, group: { name: "jobs", put: false } });

    document.querySelectorAll(".drop-zone").forEach(zone =>
        Sortable.create(zone, { ...shared, group: { name: "jobs", pull: false }, onAdd }));

    // Reload the board as soon as a different time range is picked.
    const rangeForm = document.getElementById("range-form");
    rangeForm.querySelector("select").addEventListener("change", () => rangeForm.submit());

    // Cards are links, which browsers let you drag as URLs. SortableJS uses its own pointer-based
    // dragging (forceFallback), so block the browser's native drag on cards entirely.
    document.addEventListener("dragstart", event => {
        // Dragging selected text can report the text node itself, so check its parent too.
        const element = event.target instanceof Element ? event.target : event.target.parentElement;
        if (element?.closest(".job-card"))
            event.preventDefault();
    });

    // A drop ends with a click on the card's link; don't navigate in that case.
    document.addEventListener("click", event => {
        if (Date.now() - lastDragEnd < 300 && event.target.closest(".job-card"))
            event.preventDefault();
    }, true);
})();
