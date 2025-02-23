export function addHandlers(element, dotNetHelper) {
    element.addEventListener("change", async (event) => {
        if (event.target.files.length === 0) {
            return;
        }
        const file = event.target.files[0];
        const data = new Uint8Array(await file.arrayBuffer());
        await dotNetHelper.invokeMethodAsync("UploadAsync", file.name, data);
        event.target.value = null;
    });
}