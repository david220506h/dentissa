import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import process from 'node:process'

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
    // catalogo.html no entra a la compilación: solo se usa en desarrollo
    plugins: [
      react({
        babel: {
          plugins: [['babel-plugin-react-compiler']],
        },
      }),
    ],
  }
})
