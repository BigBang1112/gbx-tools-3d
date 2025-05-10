let seeking;
let onWindowMousemove;
let onWindowMouseup;

export function addHandlers(element, dotNetHelper) {
    element.addEventListener("mousedown", async (event) => {
        event.preventDefault();
        if (event.button !== 0) return; // Only left mouse button
        seeking = true;
        await seek(dotNetHelper, element, event.clientX);
    });

    element.addEventListener("mousemove", async (event) => {
        await dotNetHelper.invokeMethodAsync("ShowPreviewTime", getPercent(event.clientX, element));
    });

    onWindowMousemove = async (event) => {
        if (seeking) {
            await seek(dotNetHelper, element, event.clientX);
        }
    };

    onWindowMouseup = async (event) => {
        if (seeking) {
            await seek(dotNetHelper, element, event.clientX);
            seeking = false;
            await dotNetHelper.invokeMethodAsync("EndSeekAsync");
        }
    };

    window.addEventListener("mousemove", onWindowMousemove);
    window.addEventListener("mouseup", onWindowMouseup);
}

export function removeHandlers() {
    if (onWindowMousemove) {
        window.removeEventListener("mousemove", onWindowMousemove);
        onWindowMousemove = null;
    }
    if (onWindowMouseup) {
        window.removeEventListener("mouseup", onWindowMouseup);
        onWindowMouseup = null;
    }
}

function getPercent(clientX, element) {
    const rect = element.getBoundingClientRect();
    const offsetX = Math.max(0, Math.min(clientX - rect.left, element.clientWidth));
    return offsetX / element.clientWidth;
}

async function seek(dotNetHelper, element, clientX) {
    const percent = getPercent(clientX, element);
    await dotNetHelper.invokeMethodAsync("Seek", percent, true);
}

export function isSeeking() {
    return seeking;
}

export function stopSeeking() {
    seeking = false;
}