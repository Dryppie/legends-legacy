module.exports = (config) => {
  config.set({
    basePath: "",
    frameworks: ["jasmine", "@angular-devkit/build-angular"],
    plugins: [
      require("karma-jasmine"),
      require("karma-chrome-launcher"),
      require("karma-jasmine-html-reporter"),
      require("karma-coverage"),
      require("@angular-devkit/build-angular/plugins/karma"),
    ],
    client: {
      clearContext: false,
    },
    reporters: ["progress", "kjhtml"],
    customLaunchers: {
      ChromeHeadlessCI: {
        base: "ChromeHeadless",
        flags: [
          "--no-sandbox",
          "--disable-gpu",
          "--disable-gpu-sandbox",
          "--disable-dev-shm-usage",
          "--disable-features=UseSkiaRenderer,Vulkan",
        ],
      },
    },
  });
};
