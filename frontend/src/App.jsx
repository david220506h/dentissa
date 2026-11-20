import { useState, useEffect } from 'react';
import Principal from './Layouts/LandPague/principal';
import Dashboard from './Layouts/Dashboar/Dashboard';
import { getUsuario, clearSession, MENSAJE_SESION_EXPIRADA } from './services/authService';
import { puedeVerPanel } from './constants/roles';
import './App.css';

function App() {
  const [usuario, setUsuario] = useState(getUsuario());
  const [vistaActual, setVistaActual] = useState('home');
  // Mensaje que abre el inicio de sesión cuando la sesión expira
  const [avisoSesion, setAvisoSesion] = useState(null);

  useEffect(() => {
    const handleSessionExpired = () => {
      clearSession();
      setUsuario(null);
      setAvisoSesion(MENSAJE_SESION_EXPIRADA);
    };
    window.addEventListener("session-expired", handleSessionExpired);
    return () => window.removeEventListener("session-expired", handleSessionExpired);
  }, []);

  if (!usuario) return <Principal usuario={usuario} setUsuario={setUsuario}
                        avisoSesion={avisoSesion} onAvisoVisto={() => setAvisoSesion(null)} />;

  // El panel es para admin, asistente y dentista; el backend valida el rol en cada petición
  if (puedeVerPanel(usuario) && vistaActual !== 'principal')
    return <Dashboard usuario={usuario} setUsuario={setUsuario}
             onVerPrincipal={() => setVistaActual('principal')} />;

  return <Principal usuario={usuario} setUsuario={setUsuario}
           onVerDashboard={() => setVistaActual('dashboard')} />;
}

export default App;
