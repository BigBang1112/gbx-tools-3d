function scrollToIndex(listElement, elementIndex) {
    if (!listElement || elementIndex == -1) {
        return;
    }

    const firstElement = listElement.querySelector('.input');

    if (!firstElement) {
        return;
    }

    // gives me precise height of 13.2 instead of just 13
    var firstElementHeight = firstElement.getBoundingClientRect().height;

    listElement.scrollTop = firstElementHeight * elementIndex;

    // additionally scroll to center
    const firstActiveElement = listElement.querySelector('.input.active');

    if (!firstActiveElement) {
        return;
    }

    firstActiveElement.scrollIntoView({ behavior: "instant", block: "center" });
}