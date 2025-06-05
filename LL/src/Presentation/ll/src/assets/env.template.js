// assets/env.template.js

(function (window) {
  window.env = window.env || {};
  // Environment variables
  window.env.environment = "${environment}";
  window.env.apiBaseUrl = "${apiBaseUrl}";
  window.env.chatApiRoot = "${chatApiRoot}";
  window.env.isLocal = "${isLocal}";
  window.env.googleClientId = "${googleClientId}";
})(this);
