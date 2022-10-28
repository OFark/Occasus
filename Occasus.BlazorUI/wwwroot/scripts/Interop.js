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

window.clipboardCopy = {
    copyText: function (codeElement)  {
        return navigator.clipboard.writeText(codeElement).then(function () {
            return true;
        })
            .catch(function (error) {
                alert(error);
                return false;
            });
    },

    pasteText: function () {
        return navigator.clipboard.readText().then(clipText => clipText)
            .catch(function (error) {
                alert(error);
                return null;
            });
    }
}