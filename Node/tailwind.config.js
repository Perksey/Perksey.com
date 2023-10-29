/** @type {import('tailwindcss').Config} */
import typography from "@tailwindcss/typography";

module.exports = {
  content: ["./*.tmp.html"],
  theme: {
    extend: {},
  },
  plugins: [typography],
  darkMode: "class",
  future: {
    hoverOnlyWhenSupported: true
  }
}

