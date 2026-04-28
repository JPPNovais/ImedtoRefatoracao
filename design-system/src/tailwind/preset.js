function withHSL(variable) {
  return ({ opacityValue }) =>
    opacityValue !== undefined
      ? `hsl(var(${variable}) / ${opacityValue})`
      : `hsl(var(${variable}))`
}

export default {
  darkMode: ["class"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["Nunito", "system-ui", "sans-serif"],
      },
      colors: {
        background:  withHSL("--background"),
        foreground:  withHSL("--foreground"),
        card:        { DEFAULT: withHSL("--card"), foreground: withHSL("--card-foreground") },
        primary:     { DEFAULT: withHSL("--primary"), foreground: withHSL("--primary-foreground"), light: withHSL("--primary-light"), dark: withHSL("--primary-dark") },
        secondary:   { DEFAULT: withHSL("--secondary"), foreground: withHSL("--secondary-foreground") },
        muted:       { DEFAULT: withHSL("--muted"), foreground: withHSL("--muted-foreground") },
        accent:      { DEFAULT: withHSL("--accent"), foreground: withHSL("--accent-foreground") },
        destructive: { DEFAULT: withHSL("--destructive"), foreground: withHSL("--destructive-foreground") },
        border:      withHSL("--border"),
        input:       withHSL("--input"),
        ring:        withHSL("--ring"),
        success:     withHSL("--success"),
        warning:     withHSL("--warning"),
        info:        withHSL("--info"),
        error:       withHSL("--error"),
        neutral:     withHSL("--neutral"),
        popover:     { DEFAULT: withHSL("--card"), foreground: withHSL("--card-foreground") },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        "accordion-down": { from: { height: "0" }, to: { height: "var(--reka-accordion-content-height)" } },
        "accordion-up":   { from: { height: "var(--reka-accordion-content-height)" }, to: { height: "0" } },
        "fade-in":        { from: { opacity: "0" }, to: { opacity: "1" } },
        "fade-out":       { from: { opacity: "1" }, to: { opacity: "0" } },
        "slide-in-from-top":    { from: { transform: "translateY(-8px)", opacity: "0" }, to: { transform: "translateY(0)", opacity: "1" } },
        "slide-in-from-right":  { from: { transform: "translateX(100%)" }, to: { transform: "translateX(0)" } },
        "slide-out-to-right":   { from: { transform: "translateX(0)" }, to: { transform: "translateX(100%)" } },
        "slide-in-from-left":   { from: { transform: "translateX(-100%)" }, to: { transform: "translateX(0)" } },
        "slide-out-to-left":    { from: { transform: "translateX(0)" }, to: { transform: "translateX(-100%)" } },
        "slide-in-from-bottom": { from: { transform: "translateY(8px)", opacity: "0" }, to: { transform: "translateY(0)", opacity: "1" } },
        "zoom-in":  { from: { transform: "scale(0.95)", opacity: "0" }, to: { transform: "scale(1)", opacity: "1" } },
        "zoom-out": { from: { transform: "scale(1)", opacity: "1" }, to: { transform: "scale(0.95)", opacity: "0" } },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up":   "accordion-up 0.2s ease-out",
        "fade-in":        "fade-in 0.15s ease-out",
        "fade-out":       "fade-out 0.15s ease-out",
        "slide-in-from-top":    "slide-in-from-top 0.15s ease-out",
        "slide-in-from-right":  "slide-in-from-right 0.2s ease-out",
        "slide-out-to-right":   "slide-out-to-right 0.2s ease-out",
        "slide-in-from-left":   "slide-in-from-left 0.2s ease-out",
        "slide-out-to-left":    "slide-out-to-left 0.2s ease-out",
        "slide-in-from-bottom": "slide-in-from-bottom 0.15s ease-out",
        "zoom-in":  "zoom-in 0.15s ease-out",
        "zoom-out": "zoom-out 0.15s ease-out",
      },
    },
  },
}
