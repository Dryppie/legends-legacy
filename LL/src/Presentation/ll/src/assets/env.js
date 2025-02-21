// assets/env.js

(function (window) {
  window["env"] = window["env"] || {};
  // Environment variables
  window.env.environment = "dev";
  window.env.apiBaseUrl = "https://localhost:7060";
  window.env.isLocal = "true";
})(this);
