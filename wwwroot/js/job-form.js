
const status = document.getElementById("Status");
const appliedAtGroup = document.getElementById("applied-at-group");
const collapse = bootstrap.Collapse.getOrCreateInstance(appliedAtGroup, {toggle:false});

function updateAppliedAtVisibility() {
    if(status.value === "0")
        collapse.hide();
    else   
        collapse.show();
}

status.addEventListener("change", updateAppliedAtVisibility);
updateAppliedAtVisibility(); 