window.enableAnalytics = function (measurementId) {
    if (typeof gtag === "function") {
        gtag('config', measurementId);
        console.log("Google Analytics enabled:", measurementId);
    } else {
        console.error("gtag() not available yet.");
    }
}