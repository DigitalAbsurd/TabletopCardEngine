/**
 * Provides typed localStorage access for C# interop.
 *
 * Handles the JSON serialisation/deserialisation that JavaScript needs
 * so the C# side sees clean booleans instead of strings or null.
 * Gracefully degrades if localStorage is not available (e.g., browser
 * with site data blocked).
 */

/**
 * Reads a boolean flag from localStorage.
 * @param {string} key - The localStorage key.
 * @returns {boolean} true if the stored value is "true" (case-insensitive), false otherwise.
 */
export function getFlag(key) {
    try {
        const value = localStorage.getItem(key);
        return value === "true";
    } catch {
        // localStorage is not available; return false as the safe default.
        return false;
    }
}

/**
 * Writes a boolean flag to localStorage.
 * @param {string} key - The localStorage key.
 * @param {boolean} value - The value to store.
 */
export function setFlag(key, value) {
    try {
        localStorage.setItem(key, value ? "true" : "false");
    } catch {
        // localStorage is not available or is full; silently ignore.
    }
}
