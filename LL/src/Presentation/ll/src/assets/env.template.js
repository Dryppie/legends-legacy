// assets/env.template.js

(function (window) {
  window.env = window.env || {};
  // Environment variables
  window.env.environment = "${environment}";
  window.env.apiBaseUrl = "${apiBaseUrl}";
})(this);
