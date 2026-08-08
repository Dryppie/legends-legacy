// assets/env.js

(function (window) {
  window["env"] = window["env"] || {};
  // Environment variables
  window.env.environment = "dev";
  window.env.apiBaseUrl = "http://localhost:7050";
  window.env.chatApiRoot = "https://localhost:7095/chat";
  window.env.isLocal = "true";
  window.env.googleClientId =
    "431775673466-3ut1k7ilm8g6bu66njohs5tc7aiochti.apps.googleusercontent.com";
  window.env.maintenanceEnabled = "false";
  window.env.maintenanceMessage =
    "Legend%27s%20Legacy%20is%20undergoing%20maintenance.";
  window.env.maintenanceExpectedBack = "";
})(this);
