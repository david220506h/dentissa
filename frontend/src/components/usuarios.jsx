import { useState, useEffect } from "react";
import { authFetch } from "../services/authService";
import "./usuarios.css";

const API = "/api/Usuarios";

const PlusIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

const DeleteIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
    <path d="M10 11v6" /><path d="M14 11v6" />
    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
  </svg>
);

const CloseIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
  </svg>
);

const ROL_COLORES = {
  admin:      "badge-admin",
  asistente:  "badge-asistente",
  dentista:   "badge-admin",
  paciente:   "badge-paciente",
};

// ── Modal nuevo usuario ───────────────────────────────────
function Modal({ onClose, onGuardar }) {
  const [form, setForm] = useState({
    nombre: "", apellido: "", email: "", telefono: "",
    password: "", rol: "paciente",
  });
  const [guardando, setGuardando] = useState(false);
  const [error,     setError]     = useState(null);

  const handleChange = (e) =>
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.nombre.trim() || !form.apellido.trim() || !form.email.trim() || !form.password.trim()) {
      setError("Completa todos los campos obligatorios");
      return;
    }
    setGuardando(true);
    setError(null);
    try {
      const res = await authFetch(API, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });
      if (!res.ok) {
        const txt = await res.text();
        let msg = "Error al crear usuario";
        try { msg = JSON.parse(txt).mensaje || msg; } catch {}
        throw new Error(msg);
      }
      onGuardar();
    } catch (err) {
      setError(err.message);
    } finally {
      setGuardando(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h3>Nuevo Usuario</h3>
          <button className="modal-close" onClick={onClose}><CloseIcon /></button>
        </div>

        {error && <div className="modal-error">⚠️ {error}</div>}

        <form className="modal-form" onSubmit={handleSubmit}>

          <div className="modal-row">
            <div className="modal-field">
              <label>Nombre *</label>
              <input name="nombre" value={form.nombre} onChange={handleChange}
                placeholder="Nombre" required />
            </div>
            <div className="modal-field">
              <label>Apellido *</label>
              <input name="apellido" value={form.apellido} onChange={handleChange}
                placeholder="Apellido" required />
            </div>
          </div>

          <div className="modal-field">
            <label>Email *</label>
            <input name="email" type="email" value={form.email} onChange={handleChange}
              placeholder="correo@ejemplo.com" required />
          </div>

          <div className="modal-field">
            <label>Teléfono</label>
            <input name="telefono" value={form.telefono} onChange={handleChange}
              placeholder="+52 000 000 0000" />
          </div>

          <div className="modal-field">
            <label>Contraseña *</label>
            <input name="password" type="password" value={form.password} onChange={handleChange}
              placeholder="Mínimo 6 caracteres" required />
          </div>

          <div className="modal-field">
            <label>Rol</label>
            <select name="rol" value={form.rol} onChange={handleChange}>
              <option value="paciente">Paciente</option>
              <option value="asistente">Asistente</option>
              <option value="dentista">Dentista</option>
              <option value="admin">Admin</option>
            </select>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-cancelar" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn-guardar" disabled={guardando}>
              {guardando ? "Creando..." : "Crear usuario"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ── Componente principal ──────────────────────────────────
export default function Usuarios() {
  const [usuarios,  setUsuarios]  = useState([]);
  const [cargando,  setCargando]  = useState(false);
  const [error,     setError]     = useState(null);
  const [showModal, setShowModal] = useState(false);

  useEffect(() => { cargarUsuarios(); }, []);

  const cargarUsuarios = async () => {
    setCargando(true);
    setError(null);
    try {
      const res = await authFetch(API);
      if (!res.ok) throw new Error("Error al cargar usuarios");
      setUsuarios(await res.json());
    } catch (err) {
      setError(err.message);
    } finally {
      setCargando(false);
    }
  };

  const handleEliminar = async (id) => {
    if (!confirm("¿Seguro que deseas eliminar este usuario?")) return;
    try {
      const res = await authFetch(`${API}/${id}`, { method: "DELETE" });
      if (!res.ok) throw new Error("Error al eliminar");
      await cargarUsuarios();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleGuardar = () => {
    setShowModal(false);
    cargarUsuarios();
  };

  return (
    <main className="content">

      <div className="usuarios-header">
        <div>
          <h2 className="usuarios-title">Gestión de Usuarios</h2>
          <p className="usuarios-subtitle">Administra cuentas de usuarios del sistema</p>
        </div>
        <button className="btn-nueva-cita" onClick={() => setShowModal(true)}>
          <PlusIcon /> Nuevo Usuario
        </button>
      </div>

      {error && (
        <div className="citas-error">
          ⚠️ {error}
          <button onClick={() => setError(null)}>✕</button>
        </div>
      )}

      <div className="usuarios-table-wrapper">
        {cargando && usuarios.length === 0 ? (
          <p className="citas-empty">Cargando usuarios...</p>
        ) : (
          <table className="usuarios-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Email</th>
                <th>Teléfono</th>
                <th>Rol</th>
                <th>Fecha Registro</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {usuarios.map(u => (
                <tr key={u.id}>
                  <td>{u.nombre} {u.apellido}</td>
                  <td>{u.email}</td>
                  <td>{u.telefono || "—"}</td>
                  <td>
                    <span className={`u-badge ${ROL_COLORES[u.rol] || "badge-paciente"}`}>
                      {u.rol}
                    </span>
                  </td>
                  <td>{u.fechaRegistro}</td>
                  <td>
                    <button className="cita-btn cita-btn--delete" title="Eliminar"
                      onClick={() => handleEliminar(u.id)}>
                      <DeleteIcon />
                    </button>
                  </td>
                </tr>
              ))}
              {usuarios.length === 0 && (
                <tr>
                  <td colSpan={6} className="citas-empty">No hay usuarios registrados.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      {showModal && (
        <Modal onClose={() => setShowModal(false)} onGuardar={handleGuardar} />
      )}

    </main>
  );
}