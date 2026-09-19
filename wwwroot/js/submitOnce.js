// Visar pågående tillstånd och hindrar dubbelklick på formulärens primära knapp.
document.addEventListener('submit', function (event) {
    var button = event.target.querySelector('[data-submit]');
    if (!button || button.disabled) {
        return;
    }
    button.disabled = true;
    button.textContent = 'Ett ögonblick…';
});
