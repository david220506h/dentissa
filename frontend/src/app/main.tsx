import '@dentissa/tokens/fuente.css';
import '@dentissa/tokens/tokens.css';
import '@dentissa/tokens/base.css';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { Rutas } from './Rutas';
import { ProveedorSesion } from './sesion';

// Datos solo en memoria: nada de pacientes se guarda en el navegador (plan, sección 7).
const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: true, gcTime: 5 * 60_000 } },
});

// TRANSICIÓN: el panel nuevo vive en /panel mientras el anterior (citas y usuarios) sigue en /.
// En "chore: retirar diseño anterior del panel" pasa a index.html y la base vuelve a "/".
export const BASE_PANEL = '/panel';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter basename={BASE_PANEL}>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
