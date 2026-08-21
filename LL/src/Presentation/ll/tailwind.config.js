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
        primary: "#F9DCA0",
        secondary: "#D1D1D1",
        gray: "#363636",
        light_gray: "#6D6D6D",
        danger: "#ff7782",
        success: "#41f1b6",
        warning: "#ffbb55",
        white: "#fff",
        info_dark: "#7d8da1",
        info_light: "#dce1eb",
        dark: "#363949",
        light: "rgba(132, 139, 200, 0.18)",
        primary_variant: "#111e88",
        dark_variant: "#677483",
        background: "#f6f6f9",
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
