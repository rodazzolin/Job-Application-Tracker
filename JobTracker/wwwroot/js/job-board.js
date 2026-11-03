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

    function setCardStatus(card, status) {
        card.classList.remove(statusClass(card.dataset.status));
        card.classList.add(statusClass(status));
        card.dataset.status = status;

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
        const archivedCount = archivedList.querySelectorAll(".job-card").length;
        document.getElementById("archived-count").textContent = archivedCount;
        archivedSection.classList.toggle("d-none", archivedCount === 0);
        refreshSummary();
    }

    // ---------- Summary strip ----------
    // Same rules as JobBoardSummary.FromCards (C#), computed from the cards' data attributes.
    const responseStatuses = ["Interviewing", "OfferReceived"];

    // Today as "yyyy-mm-dd" in local time, matching the server's DateTime.Today (same machine).
    function todayString() {
        const d = new Date();
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
    }

    function refreshSummary() {
        const weekStart = new Date();
        weekStart.setHours(0, 0, 0, 0);
        weekStart.setDate(weekStart.getDate() - 6);   // Last 7 days, today included.

        let active = 0, appliedThisWeek = 0, applied = 0, responded = 0, offers = 0;
        document.querySelectorAll("#job-board .job-card, #archived-list .job-card").forEach(card => {
            const status = card.dataset.status;
            if (status !== "Archived")
                active++;
            if (status === "OfferReceived")
                offers++;

            const appliedAt = card.dataset.appliedAt;
            if (!appliedAt)
                return;
            applied++;
            if (card.dataset.hasResponse === "true")
                responded++;
            if (new Date(`${appliedAt}T00:00`) >= weekStart)   // Parsed as local midnight.
                appliedThisWeek++;
        });

        const set = (key, value) => {
            const element = document.querySelector(`[data-summary="${key}"]`);
            if (element)
                element.textContent = value;
        };
        set("active", active);
        set("applied-this-week", appliedThisWeek);
        set("offers", offers);
        set("response-rate", applied === 0 ? "—" : `${Math.round(100 * responded / applied)}%`);
        set("response-detail", applied === 0 ? "no applications yet" : `${responded} of ${applied} reached interviews`);
    }

    // Applies the server's rules to the card's summary data after a status change
    // (see JobApplicationService.ChangeStatus): Saved clears AppliedAt; leaving Saved sets it to today
    // if empty; reaching Interviewing/Offer counts as a response from then on.
    function applySummaryRules(card, status) {
        if (status === "Saved")
            delete card.dataset.appliedAt;
        else if (!card.dataset.appliedAt)
            card.dataset.appliedAt = todayString();
        if (responseStatuses.includes(status))
            card.dataset.hasResponse = "true";
    }

    // ---------- Ordering ----------
    // Same rules as JobBoardSortExtensions.ApplySort (C#); keep the two in sync. Every sort breaks ties
    // by most recently updated, and a card that was just moved is the most recently updated of all,
    // so it goes before any card it ties with.
    const sort = board.dataset.sort;
    const titleCollator = new Intl.Collator(undefined, { sensitivity: "base", numeric: true });
    const titleOf = card => card.querySelector(".job-card-title").textContent.trim();

    // Whether the just-moved card `card` belongs before `other` in the list.
    function goesBefore(card, other) {
        switch (sort) {
            case "Applied":
                // "yyyy-mm-dd" strings compare like dates; no date ("") sorts after every date.
                return (card.dataset.appliedAt ?? "") >= (other.dataset.appliedAt ?? "");
            case "Title":
                return titleCollator.compare(titleOf(card), titleOf(other)) <= 0;
            default:   // "Updated"
                return true;
        }
    }

    // Puts the card before the first card it belongs ahead of (or at the end of the cards).
    // The list may also hold non-card elements (the "Drag a card here" placeholder), which are skipped.
    function insertSorted(list, card) {
        const others = [...list.querySelectorAll(":scope > .job-card")].filter(c => c !== card);
        const next = others.find(other => goesBefore(card, other));
        if (next)
            list.insertBefore(card, next);
        else if (others.length > 0)
            others[others.length - 1].after(card);
        else
            list.prepend(card);
    }

    // ---------- Card date label ----------
    // Same rules as JobCardViewModel.AgeLabel (C#): follows the sort, like the card order does.
    const dayCount = days => days === 1 ? "1 day" : `${days} days`;

    // "Today" or "12 days" since a "yyyy-mm-dd" date, never negative (DateDisplay.DateAge).
    function dateAge(dateString) {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const days = Math.max(0, Math.round((today - new Date(`${dateString}T00:00`)) / 86_400_000));   // Round: DST days aren't 24h.
        return days === 0 ? "Today" : dayCount(days);
    }

    function updateAgeLabel(card, ageLabel, statusChanged) {
        if (sort === "Applied")
            ageLabel.textContent = card.dataset.appliedAt ? dateAge(card.dataset.appliedAt) : "";
        else if (sort === "Updated" && statusChanged)
            ageLabel.textContent = "Today";   // Time in status restarts on a status change.
        // "Title": no date label.
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
    async function moveCard(card, status, origin) {
        const ageLabel = card.querySelector(".job-card-age");
        const previous = {
            status: card.dataset.status,
            age: ageLabel.textContent,
            appliedAt: card.dataset.appliedAt,
            hasResponse: card.dataset.hasResponse,
        };

        // Update the UI first so the move feels instant. Summary rules run before placing the card,
        // since they can change its AppliedAt (which the "Date applied" sort uses).
        setCardStatus(card, status);
        applySummaryRules(card, status);
        updateAgeLabel(card, ageLabel, status !== previous.status);
        insertSorted(listFor(status), card);
        refreshCounts();
        card.classList.add("is-saving");

        try {
            await saveStatus(card.dataset.id, status);
        } catch (error) {
            console.error(error);
            setCardStatus(card, previous.status);
            ageLabel.textContent = previous.age;
            if (previous.appliedAt)
                card.dataset.appliedAt = previous.appliedAt;
            else
                delete card.dataset.appliedAt;
            card.dataset.hasResponse = previous.hasResponse;
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

    // Fires on the list a card was dropped into (columns and the Archived grid).
    function onAdd(evt) {
        const origin = { list: evt.from, next: evt.from.children[evt.oldIndex] ?? null };
        moveCard(evt.item, evt.to.dataset.status, origin);
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
        moveCard(card, item.dataset.status, origin);
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
        onStart: evt => {
            isDragging = true;
            document.body.classList.add("is-dragging-job");
            // An archived card being dragged out shouldn't get the "Drop here to archive" overlay.
            document.body.classList.toggle("is-dragging-archived", evt.from === archivedList);
        },
        onEnd: () => {
            document.body.classList.remove("is-dragging-job", "is-dragging-archived");
            if (isDragging)
                lastDragEnd = Date.now();
            isDragging = false;
        },
    };

    board.querySelectorAll(".board-list").forEach(list =>
        Sortable.create(list, { ...shared, group: { name: "jobs" }, onAdd }));

    // The Archived grid works both ways: drag cards out of it, or drop board cards into it to archive them.
    Sortable.create(archivedList, { ...shared, group: { name: "jobs" }, onAdd });

    // Archived starts collapsed; remember if you open it. Storage can be blocked (e.g. private windows),
    // so every access is wrapped and failure just means "collapsed".
    const archivedCollapse = document.getElementById("archived-collapse");
    const archivedToggle = document.querySelector(".archived-toggle");
    const storageKey = "jobBoard.archivedOpen";
    try {
        if (localStorage.getItem(storageKey) === "true") {
            archivedCollapse.classList.add("show");   // Open instantly, without the animation.
            archivedToggle.classList.remove("collapsed");
            archivedToggle.setAttribute("aria-expanded", "true");
        }
    } catch { }
    archivedCollapse.addEventListener("shown.bs.collapse", () => { try { localStorage.setItem(storageKey, "true"); } catch { } });
    archivedCollapse.addEventListener("hidden.bs.collapse", () => { try { localStorage.removeItem(storageKey); } catch { } });

    // Reload the board as soon as a different time range or sort is picked.
    const optionsForm = document.getElementById("board-options-form");
    optionsForm.querySelectorAll("select").forEach(select =>
        select.addEventListener("change", () => optionsForm.submit()));

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
