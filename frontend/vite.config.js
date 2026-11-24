import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

// TRANSICIÓN: el panel nuevo vive en /panel (panel.html) mientras el anterior sigue en /.
// Toda ruta /panel/... del servidor de desarrollo sirve panel.html, como hará el hosting.
const panelEnRutas = {
  name: 'panel-en-rutas',
  configureServer(server) {
    server.middlewares.use((req, _res, next) => {
      // Solo navegaciones del panel; los módulos y archivos (/src/..., /@vite/...) pasan intactos
      if (req.url && /^\/panel(\/[^.]*)?(\?.*)?$/.test(req.url)) req.url = '/panel.html'
      next()
    })
  },
}

// Variables en frontend/.env.local (no versionado; ver .env.example)
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  // Hosts extra permitidos en el servidor de desarrollo (por ejemplo, un túnel), separados por comas
  const allowedHosts = (env.DEV_ALLOWED_HOSTS || '')
    .split(',')
    .map((h) => h.trim())
    .filter(Boolean)

  return {
    server: {
      host: true,
      allowedHosts,
      proxy: {
        '/api': {
          //configuracion para decirle al frontend haga peticiones al puerto del backend
          target: env.API_PROXY_TARGET || 'http://localhost:5058',
          changeOrigin: true,
        }
      }
    },
    build: {
      rollupOptions: {
        // catalogo.html queda fuera: solo existe en desarrollo
        input: {
          index: fileURLToPath(new URL('./index.html', import.meta.url)),
          panel: fileURLToPath(new URL('./panel.html', import.meta.url)),
        },
      },
    },
    plugins: [
      panelEnRutas,
      react({
        babel: {
          plugins: [['babel-plugin-react-compiler']],
        },
      }),
    ],
  }
})
