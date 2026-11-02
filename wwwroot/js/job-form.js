// Create/Edit form (Views/Shared/_JobApplicationForm.cshtml): hides Applied At while the status is Saved.
(() => {
    const statusInputs = document.querySelectorAll('input[name="Status"]');
    const appliedAtGroup = document.getElementById("applied-at-group");
    const collapse = bootstrap.Collapse.getOrCreateInstance(appliedAtGroup, { toggle: false });

    function updateAppliedAtVisibility() {
        const selected = document.querySelector('input[name="Status"]:checked')?.value;
        if (selected === "Saved")
            collapse.hide();
        else
            collapse.show();
    }

    statusInputs.forEach(input => input.addEventListener("change", updateAppliedAtVisibility));
    updateAppliedAtVisibility();
})();
