// Drag-and-drop for the job board (Views/JobApplications/Index.cshtml).
// Moves cards immediately (optimistic update), saves via POST to UpdateStatus,
// and puts the card back if the save fails.
(() => {
    const board = document.getElementById("job-board");
    if (!board)
        return;

    const updateUrl = board.dataset.updateUrl;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const archivedSection = document.getElementById("archived-section");
    const archivedList = document.getElementById("archived-list");
    const toast = bootstrap.Toast.getOrCreateInstance(document.getElementById("board-toast"));

    // Time the last real drag ended, used to swallow the click that follows a drop.
    let lastDragEnd = 0;
    let isDragging = false;

    const statusClass = status => "status-" + status.toLowerCase();

    function setCardStatus(card, status, label) {
        card.classList.remove(statusClass(card.dataset.status));
        card.classList.add(statusClass(status));
        card.dataset.status = status;
        card.querySelector(".status-badge").textContent = label;
    }

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

    // Fires on the list a card was dropped into (columns and drop zones).
    async function onAdd(evt) {
        const card = evt.item;
        const target = evt.to;
        const origin = {
            list: evt.from,
            next: evt.from.children[evt.oldIndex] ?? null,
            status: card.dataset.status,
            label: card.querySelector(".status-badge").textContent,
            age: card.querySelector(".job-card-age").textContent,
        };

        // Update the UI first so the move feels instant.
        // Columns are ordered by last update, so a moved card goes to the top;
        // cards dropped on a drop zone go to the Archived grid.
        if (target.dataset.status !== origin.status)
            card.querySelector(".job-card-age").textContent = "Today";
        setCardStatus(card, target.dataset.status, target.dataset.statusLabel);
        (target.classList.contains("drop-zone") ? archivedList : target).prepend(card);
        refreshCounts();
        card.classList.add("is-saving");

        try {
            await saveStatus(card.dataset.id, card.dataset.status);
        } catch (error) {
            console.error(error);
            setCardStatus(card, origin.status, origin.label);
            card.querySelector(".job-card-age").textContent = origin.age;
            origin.list.insertBefore(card, origin.next?.parentNode === origin.list ? origin.next : null);
            refreshCounts();
            toast.show();
        } finally {
            card.classList.remove("is-saving");
        }
    }

    const shared = {
        sort: false,                 // No manual ordering within a list.
        draggable: ".job-card",      // Placeholders and labels aren't draggable items.
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
