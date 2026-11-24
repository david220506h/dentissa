import { defineConfig, devices } from '@playwright/test';

// Solo desarrollo: verificación visual y de accesibilidad con datos de ejemplo del diseño.
// Nunca se conecta a la API real ni usa datos de pacientes.
const PUERTO = 5199;

export default defineConfig({
  testDir: './e2e',
  outputDir: './e2e/resultados',
  fullyParallel: true,
  reporter: [['list']],
  use: {
    ...devices['Desktop Chrome'],
    baseURL: `http://127.0.0.1:${PUERTO}`,
    locale: 'es-MX',
    timezoneId: 'America/Mexico_City',
  },
  expect: {
    toHaveScreenshot: { maxDiffPixelRatio: 0.002, animations: 'disabled' },
  },
  webServer: {
    command: `npx vite --host 127.0.0.1 --port ${PUERTO} --strictPort`,
    url: `http://127.0.0.1:${PUERTO}/catalogo.html`,
    reuseExistingServer: false,
    timeout: 120_000,
  },
});
