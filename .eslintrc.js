module.exports = {
  env: {
    browser: true,
    es2021: true,
  },
  extends: 'eslint:recommended',
  parserOptions: {
    ecmaVersion: 'latest',
  },
  rules: {},
  globals: {
    require: 'readonly',
    module: 'readonly',
    process: 'readonly',
    cache: 'readonly',
    __dirname: 'readonly',
    debounce: 'readonly',
  },
};
