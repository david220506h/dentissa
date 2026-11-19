import { useState, useEffect } from 'react';
import Principal from './Layouts/LandPague/principal';
import Dashboard from './Layouts/Dashboar/Dashboard';
import { getUsuario, clearSession } from './services/authService';
import './App.css';

function App() {
  const [usuario, setUsuario] = useState(getUsuario());
  const [vistaActual, setVistaActual] = useState('home');

  useEffect(() => {
    const handleSessionExpired = () => {
      clearSession();
      setUsuario(null);
    };
    window.addEventListener("session-expired", handleSessionExpired);
    return () => window.removeEventListener("session-expired", handleSessionExpired);
  }, []);

  if (!usuario) return <Principal usuario={usuario} setUsuario={setUsuario} />;

  if (usuario.rol === "admin" && vistaActual !== 'principal')
    return <Dashboard usuario={usuario} setUsuario={setUsuario} 
             onVerPrincipal={() => setVistaActual('principal')} />;

  return <Principal usuario={usuario} setUsuario={setUsuario} 
           onVerDashboard={() => setVistaActual('dashboard')} />;
}

export default App;