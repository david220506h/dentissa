// Lint del código TypeScript (paquetes compartidos y panel).
// El JavaScript que queda (src/migrar-a-sitio, servicios y roles) no se revisa aquí: se migra al sitio en Astro.
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
      // Una región con desplazamiento propio (tablas anchas) debe poder recibir el foco del teclado
      // (WCAG 2.1.1; regla scrollable-region-focusable de axe).
      'jsx-a11y/no-noninteractive-tabindex': ['error', { roles: ['tabpanel', 'region'] }],
    },
  },
);
