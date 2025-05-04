let seeking;

export function addHandlers(element, dotNetHelper) {
    element.addEventListener("mousedown", async (event) => {
        event.preventDefault();
        seeking = true;
        await seek(dotNetHelper, element, event.clientX);
    });

    element.addEventListener("mousemove", async (event) => {
        await dotNetHelper.invokeMethodAsync("ShowPreviewTime", getPercent(event.clientX, element));
    });

    window.addEventListener("mousemove", async (event) => {
        if (seeking) {
            await seek(dotNetHelper, element, event.clientX);
        }
    });

    window.addEventListener("mouseup", async (event) => {
        if (seeking) {
            await seek(dotNetHelper, element, event.clientX);
            seeking = false;
            await dotNetHelper.invokeMethodAsync("EndSeekAsync");
        }
    });
}

function getPercent(clientX, element) {
    const rect = element.getBoundingClientRect();
    const offsetX = Math.max(0, Math.min(clientX - rect.left, element.clientWidth));
    return offsetX / element.clientWidth;
}

async function seek(dotNetHelper, element, clientX) {
    const percent = getPercent(clientX, element);
    await dotNetHelper.invokeMethodAsync("Seek", percent);
}

export function isSeeking() {
    return seeking;
}

export function stopSeeking() {
    seeking = false;
}