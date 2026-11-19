/**
 * Splits the start-up screen into two phases. Blazor reports progress only for downloads
 * (--blazor-load-percentage on <html>); once that reaches 100% the runtime is still starting,
 * migrating the in-memory database and dealing the first hand, which takes seconds. This
 * marks that second phase so the loader can say so instead of sitting on "100%".
 */
(function () {
    "use strict";
    var root = document.documentElement;
    var observer = new MutationObserver(function () {
        if (root.style.getPropertyValue("--blazor-load-percentage").trim() === "100%") {
            root.setAttribute("data-load-phase", "starting");
            observer.disconnect();
        }
    });
    observer.observe(root, { attributes: true, attributeFilter: ["style"] });
})();
