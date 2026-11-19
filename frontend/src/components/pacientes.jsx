import { useState } from "react";
import "./pacientes.css";

// ── Íconos ───────────────────────────────────────────────
const SearchIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="11" cy="11" r="8" />
    <line x1="21" y1="21" x2="16.65" y2="16.65" />
  </svg>
);

const PlusIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="12" y1="5" x2="12" y2="19" />
    <line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

const EditIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
  </svg>
);

const DeleteIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
    <path d="M10 11v6" /><path d="M14 11v6" />
    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
  </svg>
);

const PhoneIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 12 19.79 19.79 0 0 1 1.61 3.39 2 2 0 0 1 3.6 1.21h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 8.84a16 16 0 0 0 6 6l.95-.95a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 21.73 16.92z" />
  </svg>
);

const MailIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
    <polyline points="22,6 12,13 2,6" />
  </svg>
);

const CalendarIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="4" width="18" height="18" rx="2" />
    <line x1="16" y1="2" x2="16" y2="6" />
    <line x1="8" y1="2" x2="8" y2="6" />
    <line x1="3" y1="10" x2="21" y2="10" />
  </svg>
);

const CloseIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
  </svg>
);

// ── Datos iniciales ───────────────────────────────────────
const PACIENTES_INICIALES = [
  { id: 1, nombre: "Angel David Díaz Navarro",   edad: 21, telefono: "+34 600 111 222", email: "diva.dz@email.com",   proximaCita: "22/06/2005" },
  { id: 2, nombre: "Juan Pérez Martínez",  edad: 28, telefono: "+34 600 333 444", email: "juan.perez@email.com",     proximaCita: "27/11/2024" },
  { id: 3, nombre: "Ana Rodríguez Santos", edad: 45, telefono: "+34 600 555 666", email: "ana.rodriguez@email.com",  proximaCita: "27/11/2024" },
];

const VACIO = { nombre: "", edad: "", telefono: "", email: "", proximaCita: "" };

// ── Iniciales ────────────────────────────────────────────
function getInitials(nombre) {
  return nombre.split(" ").slice(0, 3).map(n => n[0]).join("").toUpperCase();
}

// ── Modal agregar / editar ────────────────────────────────
function Modal({ paciente, onClose, onSave }) {
  const [form, setForm] = useState(paciente || VACIO);
  const esNuevo = !paciente;

  const handleChange = (e) =>
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!form.nombre.trim()) return;
    onSave(form);
  };

  return (
    <div className="pac-modal-overlay" onClick={onClose}>
      <div className="pac-modal" onClick={e => e.stopPropagation()}>

        <div className="pac-modal-header">
          <h3>{esNuevo ? "Nuevo Paciente" : "Editar Paciente"}</h3>
          <button className="pac-modal-close" onClick={onClose}><CloseIcon /></button>
        </div>

        <form className="pac-modal-form" onSubmit={handleSubmit}>
          <div className="pac-modal-field">
            <label>Nombre completo *</label>
            <input name="nombre" value={form.nombre} onChange={handleChange}
              placeholder="Ej. María García López" required />
          </div>

          <div className="pac-modal-row">
            <div className="pac-modal-field">
              <label>Edad</label>
              <input name="edad" type="number" value={form.edad} onChange={handleChange}
                placeholder="0" min="0" max="120" />
            </div>
            <div className="pac-modal-field">
              <label>Teléfono</label>
              <input name="telefono" value={form.telefono} onChange={handleChange}
                placeholder="+34 600 000 000" />
            </div>
          </div>

          <div className="pac-modal-field">
            <label>Email</label>
            <input name="email" type="email" value={form.email} onChange={handleChange}
              placeholder="correo@ejemplo.com" />
          </div>

          <div className="pac-modal-field">
            <label>Próxima cita</label>
            <input name="proximaCita" value={form.proximaCita} onChange={handleChange}
              placeholder="DD/MM/AAAA" />
          </div>

          <div className="pac-modal-actions">
            <button type="button" className="btn-cancelar" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn-guardar">
              {esNuevo ? "Crear paciente" : "Guardar cambios"}
            </button>
          </div>
        </form>

      </div>
    </div>
  );
}

// ── Tarjeta de paciente ───────────────────────────────────
function PacienteCard({ paciente, onEdit, onDelete }) {
  return (
    <div className="paciente-card">
      <div className="paciente-card__header">
        <div className="paciente-avatar">{getInitials(paciente.nombre)}</div>
        <div>
          <h3 className="paciente-nombre">{paciente.nombre}</h3>
          <p className="paciente-edad">{paciente.edad}<br />años</p>
        </div>
      </div>

      <div className="paciente-card__info">
        <div className="paciente-info-row"><PhoneIcon /><span>{paciente.telefono}</span></div>
        <div className="paciente-info-row"><MailIcon /><span>{paciente.email}</span></div>
        <div className="paciente-info-row"><CalendarIcon /><span>Próxima: {paciente.proximaCita}</span></div>
      </div>

      <div className="paciente-card__actions">
        <button className="pac-btn pac-btn--edit" title="Editar" onClick={() => onEdit(paciente)}>
          <EditIcon /> Editar
        </button>
        <button className="pac-btn pac-btn--delete" title="Eliminar" onClick={() => onDelete(paciente.id)}>
          <DeleteIcon /> Eliminar
        </button>
      </div>
    </div>
  );
}

// ── Componente principal ──────────────────────────────────
export default function Pacientes() {
  const [pacientes, setPacientes] = useState(PACIENTES_INICIALES);
  const [busqueda, setBusqueda] = useState("");
  const [modal, setModal] = useState(null); // null | "nuevo" | paciente obj

  const filtrados = pacientes.filter(p =>
    p.nombre.toLowerCase().includes(busqueda.toLowerCase())
  );

  const handleEliminar = (id) =>
    setPacientes(prev => prev.filter(p => p.id !== id));

  const handleGuardar = (form) => {
    if (modal === "nuevo") {
      setPacientes(prev => [...prev, { ...form, id: Date.now(), edad: Number(form.edad) }]);
    } else {
      setPacientes(prev =>
        prev.map(p => p.id === modal.id ? { ...p, ...form, edad: Number(form.edad) } : p)
      );
    }
    setModal(null);
  };

  return (
    <main className="content">

      {/* Toolbar */}
      <div className="pacientes-toolbar">
        <div className="pacientes-search">
          <SearchIcon />
          <input
            type="text"
            placeholder="Buscar paciente..."
            value={busqueda}
            onChange={e => setBusqueda(e.target.value)}
          />
        </div>
        <button className="btn-nueva-cita" onClick={() => setModal("nuevo")}>
          <PlusIcon /> Nuevo Paciente
        </button>
      </div>

      {/* Grid */}
      <div className="pacientes-grid">
        {filtrados.map(p => (
          <PacienteCard
            key={p.id}
            paciente={p}
            onEdit={setModal}
            onDelete={handleEliminar}
          />
        ))}
        {filtrados.length === 0 && (
          <p className="pacientes-empty">No se encontraron pacientes.</p>
        )}
      </div>

      {/* Modal */}
      {modal !== null && (
        <Modal
          paciente={modal === "nuevo" ? null : modal}
          onClose={() => setModal(null)}
          onSave={handleGuardar}
        />
      )}

    </main>
  );
}