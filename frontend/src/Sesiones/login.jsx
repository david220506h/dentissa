import './login.css';
import { useState } from 'react';
import { FaEye, FaEyeSlash } from 'react-icons/fa';
import { login } from '../services/authService';

function Login({ setUsuario, onClose }) {
  const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const usuario = await login(email, password);
      setUsuario(usuario);
      if(onClose) onClose();
      alert("haz iniciado sesion");
    } catch (err) {
      setError(err.message || "Error al iniciar sesión");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className='overlay'>
      <div className='login'>
        <h1 className='login-title'>Iniciar Sesión</h1>
        <p className='login-subtitle'>Por favor, ingrese sus credenciales para continuar</p>

        {error && <p style={{ color: 'red', textAlign: 'center', fontSize: '13px' }}>{error}</p>}

        <form onSubmit={handleSubmit}>
          <div className="datos">
            <label>Correo Electrónico*</label>
            <input type="email" className="email" required placeholder='correo@gmail.com'
              value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>

          <div className="datos">
            <label>Contraseña*</label>
            <div className='password-container'>
              <input type={showPassword ? "text" : "password"} className="password password-input"
                required placeholder='Contraseña'
                value={password} onChange={(e) => setPassword(e.target.value)} />
              <span className='eye-icon' onClick={() => setShowPassword(!showPassword)}
                style={{ marginLeft: '10px', cursor: 'pointer' }}>
                {showPassword ? <FaEyeSlash /> : <FaEye />}
              </span>
            </div>
          </div>

          <button type="submit" className="IN" disabled={loading}>
            {loading ? "Ingresando..." : "Iniciar sesión"}
          </button>
        </form>
      </div>
    </div>
  );
}

export default Login;