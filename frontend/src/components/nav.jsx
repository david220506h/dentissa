
import { useState } from 'react';
import Login from '../Sesiones/login';
import Register from '../Sesiones/register';
import { logout } from '../services/authService';
import './nav.css';
import { FaUserCircle } from 'react-icons/fa';
// import {useNavigate} from 'react-router-dom';



const Nav = ({ setUsuario, usuario, mostrarMenu = true, onVerPrincipal, onVerDashboard }) => {
  
  // const navigate = useNavigate();
  
  const [showLogin, setShowLogin] = useState(false);
  const [showRegister, setShowRegister] = useState(false);

  const handleLogout = async () => {
    await logout();
    setUsuario(null);
  };

  const handleRegistroExitoso = () => {
    setShowRegister(false);
    setShowLogin(true);
    
  };

  // const handleVerPerfil = () => {
  //   if (usuario.rol === 'admin') {
  //     navigate('/dashboard');
  //   }else{
  //     navigate('/perfil');
  //   }
  // };
  return (
    <nav className="navbar">
      <div className="logo">
        <span className='logo' onClick={onVerPrincipal}>Dentissa</span>
        <span className='subtitle-logo'><p>Tu sonrisa perfecta</p></span>
        {/* <button className="logo" style={{border:'none'}}><p className='name-logo'>Dentissa</p> */}
        {/* <p className='subtitle-logo'>Tu sonrisa perfecta</p>   */}

      </div>
      {mostrarMenu && (
        <ul className="nav-list">
        <li className="nav-item"><a href="#">Servicios</a></li>
        <li className="nav-item"><a href="#">Nosotros</a></li>
        <li className="nav-item"><a href="#">Testimonios</a></li>
        <li className="nav-item"><a href="#">Contacto</a></li>
      </ul>
      )}
      
      
      <div className='auth-buttons'>
        {usuario ? (
          <>
            <button onClick= {onVerDashboard} className='ver-perfil'><FaUserCircle/> Hola, {usuario.nombre}</button>
            <button onClick={handleLogout} className="btn-login">Cerrar sesión</button>              
            
          </>
        ) : (
          <>
            <button onClick={() => setShowRegister(true)} className="btn-login">Registrarse</button> 
            <button onClick={() => setShowLogin(true)} className="btn-login">Iniciar Sesión</button>
          </>
        )}
      </div>

      {showLogin && (
        <div className="modal-overlay" onClick={() => setShowLogin(false)}>
          <div className="modal-content modal-animation" onClick={(e) => e.stopPropagation()}>
            <button className="modal-close" onClick={() => setShowLogin(false)}>×</button>
            <Login setUsuario={setUsuario} onClose={() => setShowLogin(false)}/>
            <p className='register-link'>
              ¿No tienes cuenta?{" "}
              <button className='button-register' onClick={() => { setShowLogin(false); setShowRegister(true); }}>
                Regístrate aquí
              </button>
            </p>
          </div>
        </div>
      )}

      {showRegister && (
        <div className="modal-overlay" onClick={() => setShowRegister(false)}>
          <div className="modal-content modal-animation" onClick={(e) => e.stopPropagation()}>
            <button className="modal-close" onClick={() => setShowRegister(false)}>×</button>
            <Register onRegistroExitoso={handleRegistroExitoso} />
            <p className='register-link'>
              ¿Ya tienes cuenta?{" "}
              <button className='button-register' onClick={() => { setShowRegister(false); setShowLogin(true); }}>
                Iniciar sesión aquí
              </button>
            </p>
          </div>
        </div>
      )}
    </nav>
  );
};

export default Nav;