// Autofill for the Create page (Views/JobApplications/Create.cshtml).
// Sends the pasted URL to JobApplicationsController.Lookup and copies the returned
// fields into the job form. Nothing is saved; the user reviews and submits as usual.
(() => {
    const form = document.getElementById("autofill-form");
    if (!form)
        return;

    const urlInput = document.getElementById("autofill-url");
    const button = document.getElementById("autofill-button");
    const spinner = button.querySelector(".spinner-border");
    const status = document.getElementById("autofill-status");

    // Job form input IDs (from asp-for) → readable names for the status message.
    const fields = {
        Title: "title",
        CompanyName: "company",
        Location: "location",
        PostedAt: "posted date",
        PostingURL: "posting URL",
    };

    function setStatus(message, kind) {
        status.textContent = message;
        status.classList.toggle("text-success", kind === "success");
        status.classList.toggle("text-danger", kind === "error");
    }

    function setLoading(loading) {
        button.disabled = loading;
        urlInput.readOnly = loading;
        spinner.classList.toggle("d-none", !loading);
    }

    // The JSON uses camelCase (title, companyName, ...); look keys up case-insensitively
    // so names like "postingURL" vs "postingUrl" both match.
    function getField(data, name) {
        const key = Object.keys(data).find(k => k.toLowerCase() === name.toLowerCase());
        return key === undefined ? null : data[key];
    }

    function fill(data, pastedUrl) {
        const filled = [];
        for (const [id, label] of Object.entries(fields)) {
            let value = getField(data, id);
            if (id === "PostingURL" && !value)
                value = pastedUrl;   // Fall back to the link the user pasted.
            if (value === null || value === undefined || value === "")
                continue;

            const input = document.getElementById(id);
            input.value = value;
            // Let validation and any listeners react as if the user had typed it.
            input.dispatchEvent(new Event("input", { bubbles: true }));
            input.dispatchEvent(new Event("change", { bubbles: true }));
            input.classList.remove("is-autofilled");
            void input.offsetWidth;  // Restart the highlight animation if it's already applied.
            input.classList.add("is-autofilled");
            filled.push(label);
        }
        return filled;
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();

        const url = urlInput.value.trim();
        if (!url || !urlInput.checkValidity()) {
            setStatus("Enter a full link, starting with https://", "error");
            urlInput.focus();
            return;
        }

        setLoading(true);
        setStatus("Reading the posting…");
        try {
            const response = await fetch(`${form.dataset.lookupUrl}?url=${encodeURIComponent(url)}`, {
                headers: { "Accept": "application/json" },
            });

            if (response.status === 404) {
                setStatus("Couldn't read that page. Fill in the details below.", "error");
                return;
            }
            if (response.status === 400) {
                setStatus("That link doesn't look valid.", "error");
                return;
            }
            if (!response.ok)
                throw new Error(`Lookup failed with HTTP ${response.status}`);

            const filled = fill(await response.json(), url);
            setStatus(filled.length > 0
                ? `Filled ${filled.join(", ")}. Check the details, then save.`
                : "Found the posting, but it had no details to fill in.",
                filled.length > 0 ? "success" : "error");
        } catch (error) {
            console.error(error);
            setStatus("Something went wrong reading the posting. Please try again.", "error");
        } finally {
            setLoading(false);
        }
    });
})();
