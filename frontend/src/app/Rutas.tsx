import { Navigate, Route, Routes } from 'react-router-dom';
import { IniciarSesion } from '../features/auth/IniciarSesion';
import { NoEncontrado, Proximamente } from '../features/estados/Estados';
import { Agenda } from '../features/agenda/Agenda';
import { Inicio } from '../features/inicio/Inicio';
import { Disposicion } from './Disposicion';
import { SECCIONES } from './permisos';
import { RutaProtegida } from './RutaProtegida';

// Todas las secciones de la matriz existen como ruta; las que aún no tienen pantalla muestran
// "Próximamente" (Servicios y Ofertas por separado, decisión 14.4). Inicio, Agenda y Equipo
// llegan con el diseño del arquitecto.
export function Rutas() {
  return (
    <Routes>
      <Route path="/iniciar-sesion" element={<IniciarSesion />} />
      <Route
        element={
          <RutaProtegida>
            <Disposicion />
          </RutaProtegida>
        }
      >
        {SECCIONES.map((s) => (
          <Route
            key={s.id}
            path={s.ruta}
            element={
              <RutaProtegida seccion={s.id}>
                {s.id === 'inicio' ? <Inicio /> : s.id === 'agenda' ? <Agenda /> : <Proximamente titulo={s.etiqueta} icono={s.icono} />}
              </RutaProtegida>
            }
          />
        ))}
        <Route path="*" element={<NoEncontrado />} />
      </Route>
      <Route path="/registro" element={<Navigate to="/iniciar-sesion" replace />} />
    </Routes>
  );
}
