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

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ProveedorSesion>
          <Rutas />
        </ProveedorSesion>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
