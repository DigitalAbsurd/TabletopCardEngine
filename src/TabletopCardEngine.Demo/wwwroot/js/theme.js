/**
 * The demo's Light / Dark theme.
 *
 * A classic script loaded first in <head>, so the theme is on <html data-theme> before the first
 * paint rather than flashing light on a dark-theme visit. Until the visitor picks a theme it
 * follows prefers-color-scheme; a pick is kept in localStorage and wins from then on.
 *
 * AntDesign injects its own stylesheet from AntDesign.lib.module.js, after this script has run, so
 * a MutationObserver rewrites that link to the dark build as soon as it is added. The two builds
 * are complete stylesheets, so swapping the href is the whole switch.
 */
(function () {
    "use strict";

    var storageKey = "tce-demo-theme";
    var lightHref = "_content/AntDesign/css/ant-design-blazor.css";
    var darkHref = "_content/AntDesign/css/ant-design-blazor.dark.css";
    var media = window.matchMedia ? window.matchMedia("(prefers-color-scheme: dark)") : null;

    function stored() {
        try {
            var value = localStorage.getItem(storageKey);
            return value === "light" || value === "dark" ? value : null;
        } catch {
            return null;
        }
    }

    function resolve() {
        return stored() || (media && media.matches ? "dark" : "light");
    }

    function syncAntDesignLink(theme) {
        var links = document.querySelectorAll("link[rel='stylesheet'][href*='_content/AntDesign/css/ant-design-blazor']");
        links.forEach(function (link) {
            var wanted = theme === "dark" ? darkHref : lightHref;
            if (link.getAttribute("href") !== wanted) {
                link.setAttribute("href", wanted);
            }
        });
    }

    function apply(theme) {
        document.documentElement.setAttribute("data-theme", theme);
        syncAntDesignLink(theme);
    }

    apply(resolve());

    new MutationObserver(function () {
        syncAntDesignLink(document.documentElement.getAttribute("data-theme"));
    }).observe(document.head, { childList: true });

    if (media && media.addEventListener) {
        media.addEventListener("change", function () {
            if (!stored()) {
                apply(resolve());
            }
        });
    }

    window.demoTheme = {
        /** @returns {"light" | "dark"} the theme on the page now. */
        get: function () {
            return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
        },
        /** @param {"light" | "dark"} theme - the visitor's pick, kept for the next visit. */
        set: function (theme) {
            var value = theme === "dark" ? "dark" : "light";
            try {
                localStorage.setItem(storageKey, value);
            } catch {
                // Site data is blocked; the pick still applies to this page.
            }
            apply(value);
        }
    };
})();
