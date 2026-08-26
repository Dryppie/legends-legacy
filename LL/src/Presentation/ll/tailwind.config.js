/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["var(--ll-font-body)"],
        serif: ["var(--ll-font-display)"],
      },
      keyframes: {
        fadeInUp: {
          "0%": {
            opacity: "0",
            transform: "translateY(10px)",
          },
          "100%": {
            opacity: "1",
            transform: "translateY(0)",
          },
        },
      },
      animation: {
        "fade-in-up": "fadeInUp 0.4s ease-in-out forwards",
      },
      backgroundImage: {
        texture: `linear-gradient(
            to right,
            rgba(73, 46, 37, 0.1) 0%,
            rgba(52, 35, 33, 0.3) 30%,
            rgba(20, 19, 27, 0.3) 60%,
            rgba(23, 25, 37, 0.3) 80%,
            rgba(25, 31, 47, 0.0) 100%
          ), url('assets/core/texture.png')`,
      },
      colors: {
        primary: "rgb(var(--ll-color-primary-rgb) / <alpha-value>)",
        secondary: "rgb(var(--ll-color-text-muted-rgb) / <alpha-value>)",
        gray: "rgb(var(--ll-color-surface-neutral-rgb) / <alpha-value>)",
        light_gray: "rgb(var(--ll-color-text-muted-rgb) / <alpha-value>)",
        danger: "rgb(var(--ll-color-danger-rgb) / <alpha-value>)",
        success: "rgb(var(--ll-color-success-rgb) / <alpha-value>)",
        warning: "rgb(var(--ll-color-warning-rgb) / <alpha-value>)",
        info: "rgb(var(--ll-color-info-rgb) / <alpha-value>)",
        zinc: {
          500: "rgb(var(--ll-color-text-muted-rgb) / <alpha-value>)",
          600: "rgb(var(--ll-color-text-disabled-rgb) / <alpha-value>)",
        },
        white: "#fff",
        royal: {
          primary: "#03131E",
          secondary: "#003340",
        },
        ancient: {
          primary: "#daa520",
          secondary: "#f4e4bc",
        },
        blood: {
          primary: "#8b0000",
          secondary: "#8B3E3E",
        },
      },
    },
  },
  plugins: [],
};
