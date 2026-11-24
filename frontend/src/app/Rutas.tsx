import { Navigate, Route, Routes } from 'react-router-dom';
import { IniciarSesion } from '../features/auth/IniciarSesion';
import { NoEncontrado, Proximamente } from '../features/estados/Estados';
import { Agenda } from '../features/agenda/Agenda';
import { Equipo } from '../features/equipo/Equipo';
import { Inicio } from '../features/inicio/Inicio';
import { FichaPaciente, Pacientes } from '../features/pacientes/Pacientes';
import { Solicitudes } from '../features/solicitudes/Solicitudes';
import { Disposicion } from './Disposicion';
import { SECCIONES } from './permisos';
import { RutaProtegida } from './RutaProtegida';

// Todas las secciones de la matriz existen como ruta; las que aún no tienen pantalla muestran
// "Próximamente" (Servicios y Ofertas por separado, decisión 14.4). Inicio, Agenda, Pacientes (con su
// ficha en /pacientes/:id) y Equipo tienen pantalla.
const PANTALLAS = { inicio: Inicio, agenda: Agenda, solicitudes: Solicitudes, pacientes: Pacientes, equipo: Equipo } as const;
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
                {(() => {
                  const Pantalla = PANTALLAS[s.id as keyof typeof PANTALLAS];
                  return Pantalla ? <Pantalla /> : <Proximamente titulo={s.etiqueta} icono={s.icono} />;
                })()}
              </RutaProtegida>
            }
          />
        ))}
        <Route
          path="/pacientes/:id"
          element={
            <RutaProtegida seccion="pacientes">
              <FichaPaciente />
            </RutaProtegida>
          }
        />
        <Route path="*" element={<NoEncontrado />} />
      </Route>
      <Route path="/registro" element={<Navigate to="/iniciar-sesion" replace />} />
    </Routes>
  );
}
