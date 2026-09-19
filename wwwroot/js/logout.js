// Utloggningen måste postas med antiforgery-token; formuläret ligger i _Layout där det
// finns en HttpContext som kan rendera token.
window.submitLogout = function () {
    var form = document.getElementById('logout-form');
    if (form) {
        form.submit();
    }
};
