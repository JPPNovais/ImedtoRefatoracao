/* eslint-env node */
module.exports = {
    root: true,
    extends: [
        "plugin:vue/vue3-recommended",
        "@typescript-eslint/recommended",
        "prettier",
    ],
    parser: "vue-eslint-parser",
    parserOptions: {
        parser: "@typescript-eslint/parser",
    },
    rules: {
        "@typescript-eslint/no-explicit-any": "warn",
        "@typescript-eslint/no-non-null-assertion": "off",
    },
};
