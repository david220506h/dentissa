import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

/**
 * Estado de la conexión. Al perderla se vacía la caché de datos (solo en memoria): el panel muestra
 * "Sin conexión con la clínica" y nunca datos guardados de pacientes.
 */
export function useConexion(): { enLinea: boolean; reintentar: () => void } {
  const queryClient = useQueryClient();
  const [enLinea, setEnLinea] = useState(() => (typeof navigator === 'undefined' ? true : navigator.onLine));

  useEffect(() => {
    const alConectar = () => setEnLinea(true);
    const alDesconectar = () => {
      queryClient.clear();
      setEnLinea(false);
    };
    window.addEventListener('online', alConectar);
    window.addEventListener('offline', alDesconectar);
    return () => {
      window.removeEventListener('online', alConectar);
      window.removeEventListener('offline', alDesconectar);
    };
  }, [queryClient]);

  const reintentar = () => {
    if (navigator.onLine) {
      setEnLinea(true);
      void queryClient.invalidateQueries();
    }
  };

  return { enLinea, reintentar };
}
