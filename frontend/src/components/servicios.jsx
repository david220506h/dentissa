import { useState } from "react";
import "./servicios.css";

// ── Íconos ───────────────────────────────────────────────
const PlusIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

const EditIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
  </svg>
);

const DeleteIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
    <path d="M10 11v6" /><path d="M14 11v6" />
    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
  </svg>
);

const CloseIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
  </svg>
);

// ── Datos iniciales ───────────────────────────────────────
const SERVICIOS_INICIALES = [
  {
    id: 1,
    nombre: "Limpieza Dental",
    descripcion: "Limpieza profesional completa con eliminación de sarro y placa bacteriana. Incluye pulido y flúor.",
    precio: 60,
    duracion: "30 min",
    imagen: "https://images.unsplash.com/photo-1588776814546-1ffbb172cfa7?w=600&q=80",
  },
  {
    id: 2,
    nombre: "Ortodoncia",
    descripcion: "Consulta y seguimiento de ortodoncia. Corrección de la posición de los dientes mediante brackets.",
    precio: 150,
    duracion: "45 min",
    imagen: "https://images.unsplash.com/photo-1606811841689-23dfddce3e95?w=600&q=80",
  },
  {
    id: 3,
    nombre: "Blanqueamiento Dental",
    descripcion: "Blanqueamiento dental profesional para una sonrisa más brillante y blanca.",
    precio: 200,
    duracion: "60 min",
    imagen: "https://images.unsplash.com/photo-1598256989014-8b53e8a5f0c5?w=600&q=80",
  },
  {
    id: 4,
    nombre: "Implantes Dentales",
    descripcion: "Colocación de implantes dentales de titanio. Solución permanente para dientes perdidos.",
    precio: 900,
    duracion: "90 min",
    imagen: "https://images.unsplash.com/photo-1629909614468-7c57c1c5b7d8?w=600&q=80",
  },
  {
    id: 5,
    nombre: "Endodoncia",
    descripcion: "Tratamiento de conductos para salvar dientes con infección o daño en la pulpa dental.",
    precio: 350,
    duracion: "75 min",
    imagen: "https://images.unsplash.com/photo-1581093057305-25f4e5e81ad6?w=600&q=80",
  },
  {
    id: 6,
    nombre: "Consulta General",
    descripcion: "Revisión dental completa con diagnóstico y plan de tratamiento personalizado.",
    precio: 40,
    duracion: "30 min",
    imagen: "https://images.unsplash.com/photo-1537368910025-700350fe46c7?w=600&q=80",
  },
];

const VACIO = { nombre: "", descripcion: "", precio: "", duracion: "", imagen: "" };

// ── Modal ─────────────────────────────────────────────────
function Modal({ servicio, onClose, onSave }) {
  const [form, setForm] = useState(servicio || VACIO);
  const esNuevo = !servicio;

  const handleChange = (e) =>
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!form.nombre.trim()) return;
    onSave(form);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <h3>{esNuevo ? "Nuevo Servicio" : "Editar Servicio"}</h3>
          <button className="modal-close" onClick={onClose}><CloseIcon /></button>
        </div>

        <form className="modal-form" onSubmit={handleSubmit}>
          <div className="modal-field">
            <label>Nombre *</label>
            <input name="nombre" value={form.nombre} onChange={handleChange}
              placeholder="Ej. Limpieza Dental" required />
          </div>
          <div className="modal-field">
            <label>Descripción</label>
            <textarea name="descripcion" value={form.descripcion} onChange={handleChange}
              placeholder="Describe el servicio..." rows={3} />
          </div>
          <div className="modal-row">
            <div className="modal-field">
              <label>Precio (€)</label>
              <input name="precio" type="number" value={form.precio} onChange={handleChange}
                placeholder="0" min="0" />
            </div>
            <div className="modal-field">
              <label>Duración</label>
              <input name="duracion" value={form.duracion} onChange={handleChange}
                placeholder="Ej. 30 min" />
            </div>
          </div>
          <div className="modal-field">
            <label>URL de imagen</label>
            <input name="imagen" value={form.imagen} onChange={handleChange}
              placeholder="https://..." />
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-cancelar" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn-guardar">
              {esNuevo ? "Crear servicio" : "Guardar cambios"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ── Tarjeta de servicio ───────────────────────────────────
function ServicioCard({ servicio, onEdit, onDelete }) {
  return (
    <div className="servicio-card">
      <div className="servicio-img-wrapper">
        {servicio.imagen
          ? <img src={servicio.imagen} alt={servicio.nombre} className="servicio-img" />
          : <div className="servicio-img-placeholder" />
        }
        <div className="servicio-card-actions">
          <button className="svc-btn svc-btn--edit" title="Editar" onClick={() => onEdit(servicio)}>
            <EditIcon />
          </button>
          <button className="svc-btn svc-btn--delete" title="Eliminar" onClick={() => onDelete(servicio.id)}>
            <DeleteIcon />
          </button>
        </div>
      </div>

      <div className="servicio-body">
        <h3 className="servicio-nombre">{servicio.nombre}</h3>
        <p className="servicio-desc">{servicio.descripcion}</p>
        <div className="servicio-meta">
          <div className="servicio-meta-row">
            <span className="meta-label">💰 Precio:</span>
            <span className="meta-value">€{servicio.precio}</span>
          </div>
          <div className="servicio-meta-row">
            <span className="meta-label">🕐 Duración:</span>
            <span className="meta-value meta-pink">{servicio.duracion}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Componente principal ──────────────────────────────────
export default function Servicios() {
  const [servicios, setServicios] = useState(SERVICIOS_INICIALES);
  const [modal, setModal] = useState(null); // null | "nuevo" | servicio obj

  const handleNuevo = () => setModal("nuevo");

  const handleEditar = (servicio) => setModal(servicio);

  const handleEliminar = (id) =>
    setServicios(prev => prev.filter(s => s.id !== id));

  const handleGuardar = (form) => {
    if (modal === "nuevo") {
      setServicios(prev => [
        ...prev,
        { ...form, id: Date.now(), precio: Number(form.precio) },
      ]);
    } else {
      setServicios(prev =>
        prev.map(s => s.id === modal.id
          ? { ...s, ...form, precio: Number(form.precio) }
          : s
        )
      );
    }
    setModal(null);
  };

  return (
    <main className="content">

      {/* Encabezado */}
      <div className="servicios-header">
        <div>
          <h2 className="servicios-title">Gestión de Servicios</h2>
          <p className="servicios-subtitle">Administra los servicios dentales ofrecidos</p>
        </div>
        <button className="btn-nueva-cita" onClick={handleNuevo}>
          <PlusIcon />
          Nuevo Servicio
        </button>
      </div>

      {/* Grid */}
      <div className="servicios-grid">
        {servicios.map(s => (
          <ServicioCard
            key={s.id}
            servicio={s}
            onEdit={handleEditar}
            onDelete={handleEliminar}
          />
        ))}
      </div>

      {/* Modal */}
      {modal !== null && (
        <Modal
          servicio={modal === "nuevo" ? null : modal}
          onClose={() => setModal(null)}
          onSave={handleGuardar}
        />
      )}

    </main>
  );
}