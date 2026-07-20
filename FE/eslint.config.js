import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{js,jsx}'],
    extends: [
      js.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: { ecmaFeatures: { jsx: true } },
    },
    rules: {
      // Data-fetch effects intentionally update loading/data state.
      'react-hooks/set-state-in-effect': 'off',
    },
  },
  {
    files: ['src/context/AuthContext.jsx'],
    rules: {
      // The context module also exports role helpers consumed by route components.
      'react-refresh/only-export-components': 'off',
    },
  },
])
