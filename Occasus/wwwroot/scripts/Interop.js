function PreventEnterKey(id) {
    var el = document.getElementById(id);
    if (el && !el.dataset.hasListener) {
        el.addEventListener("keydown", function (event) {
            if (event.keyCode === 13) {
                event.preventDefault();
                return false;
            }

        }, false);
        el.dataset.hasListener = true;
    }
}

