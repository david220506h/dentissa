import '@dentissa/tokens/tokens.css';
import '@dentissa/tokens/fuente.css';
import '@dentissa/tokens/base.css';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { Catalogo } from './Catalogo';

// Catálogo interno de componentes: solo en desarrollo (frontend/catalogo.html no entra en la compilación).
createRoot(document.getElementById('raiz')!).render(
  <StrictMode>
    <Catalogo />
  </StrictMode>,
);
