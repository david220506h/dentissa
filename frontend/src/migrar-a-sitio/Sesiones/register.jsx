import './register.css';
import { FaEye, FaEyeSlash } from 'react-icons/fa';
import { useState } from 'react';
import { register } from '../../services/authService';

function Register({ onRegistroExitoso }) {
  const [showPassword, setShowPassword] = useState(false);
  const [nombre, setNombre] = useState("");
  const [apellido, setApellido] = useState("");
  const [telefono, setTelefono] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (password !== confirmPassword) {
      setError("Las contraseñas no coinciden");
      return;
    }
    if (password.length < 6) {
      setError("La contraseña debe tener al menos 6 caracteres");
      return;
    }

    setLoading(true);
    try {
      await register(nombre, apellido, email, telefono, password);
      if (onRegistroExitoso) onRegistroExitoso();
    } catch (err) {
      setError(err.message || "Error al registrar");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className='overlay'>
      <div className='register'>
        <h1 className='register-title'>Regístrate aquí</h1>
        <form onSubmit={handleSubmit}>
          <p className='register-subtitle'>Por favor, complete los campos para registrarse</p>

          {error && <p style={{ color: 'red', textAlign: 'center', fontSize: '13px' }}>{error}</p>}

          <div className='datos-register'>
            <label>Nombre*</label>
            <input type='text' required placeholder='Nombre'
              value={nombre} onChange={(e) => setNombre(e.target.value)} />
          </div>
          <div className='datos-register'>
            <label>Apellido*</label>
            <input type='text' required placeholder='Apellido'
              value={apellido} onChange={(e) => setApellido(e.target.value)} />
          </div>
          <div className='datos-register'>
            <label>Teléfono</label>
            <input type='tel' placeholder='Teléfono (opcional)'
              value={telefono} onChange={(e) => setTelefono(e.target.value)} />
          </div>
          <div className='datos-register'>
            <label>Correo electrónico*</label>
            <input type='email' required placeholder='correo@gmail.com'
              value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <div className="datos-register">
            <label>Contraseña*</label>
            <div className='password-container'>
              <input type={showPassword ? "text" : "password"} className="password password-input"
                required placeholder='Mínimo 6 caracteres'
                value={password} onChange={(e) => setPassword(e.target.value)} />
              <span className='eye-icon' onClick={() => setShowPassword(!showPassword)}
                style={{ marginLeft: '10px', cursor: 'pointer' }}>
                {showPassword ? <FaEyeSlash /> : <FaEye />}
              </span>
            </div>
          </div>
          <div className="datos-register">
            <label>Confirmar contraseña*</label>
            <div className='password-container'>
              <input type={showPassword ? "text" : "password"} className="password password-input"
                required placeholder='Repetir contraseña'
                value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
              <span className='eye-icon' onClick={() => setShowPassword(!showPassword)}
                style={{ marginLeft: '10px', cursor: 'pointer' }}>
                {showPassword ? <FaEyeSlash /> : <FaEye />}
              </span>
            </div>
          </div>

          <button type="submit" className="IN" disabled={loading}>
            {loading ? "Registrando..." : "Registrarse"}
          </button>
        </form>
      </div>
    </div>
  );
}

export default Register;