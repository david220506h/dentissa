// Lint del código TypeScript nuevo (paquetes compartidos y panel).
// El código JavaScript anterior del panel conserva su configuración en frontend/eslint.config.js.
import tseslint from 'typescript-eslint';
import jsxA11y from 'eslint-plugin-jsx-a11y';
import reactHooks from 'eslint-plugin-react-hooks';
import globals from 'globals';

export default tseslint.config(
  { ignores: ['**/dist/**', '**/node_modules/**', '**/*.js', '**/*.jsx', '**/*.cjs', '**/*.mjs'] },
  {
    files: ['**/*.{ts,tsx}'],
    extends: [...tseslint.configs.recommended, jsxA11y.flatConfigs.strict],
    plugins: { 'react-hooks': reactHooks },
    languageOptions: { globals: globals.browser },
    rules: {
      ...reactHooks.configs.recommended.rules,
      '@typescript-eslint/consistent-type-imports': 'error',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
    },
  },
);
