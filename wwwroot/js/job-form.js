
const statusElement = document.getElementById("Status");
const appliedAtGroup = document.getElementById("applied-at-group");
const collapse = bootstrap.Collapse.getOrCreateInstance(appliedAtGroup, {toggle:false});

function updateAppliedAtVisibility() {
    if(statusElement.value === "0")
        collapse.hide();
    else   
        collapse.show();
}

statusElement.addEventListener("change", updateAppliedAtVisibility);
updateAppliedAtVisibility(); 