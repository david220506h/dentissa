import { defineConfig } from 'vitest/config';

// Pruebas de los paquetes compartidos y del panel (jsdom + Testing Library + axe).
// Las pruebas de extremo a extremo y la verificación visual van con Playwright (frontend/e2e).
export default defineConfig({
  esbuild: { jsx: 'automatic' },
  test: {
    environment: 'jsdom',
    include: [
      'packages/*/src/**/*.test.{ts,tsx}',
      'packages/*/test/**/*.test.ts',
      'frontend/src/**/*.test.{ts,tsx}',
    ],
    setupFiles: ['./vitest.setup.ts'],
    restoreMocks: true,
  },
});
