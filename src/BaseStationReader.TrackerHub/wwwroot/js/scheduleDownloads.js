window.scheduleDownloads = {
    downloadCsv: async function (fileName, streamReference) {
        // Use a temporary object URL so large schedules do not have to be embedded in the page markup.
        const buffer = await streamReference.arrayBuffer();
        const url = URL.createObjectURL(new Blob([buffer], { type: "text/csv;charset=utf-8" }));
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(url);
    }
};
