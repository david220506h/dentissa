import { useState } from "react";
import "./Ofertas.css";

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
const OFERTAS_INICIALES = [
  {
    id: 1,
    nombre: "Limpieza de Verano",
    servicio: "Limpieza Dental",
    descuento: 20,
    estado: "Inactivo",
    desde: "31/5/2024",
    hasta: "30/8/2024",
  },
  {
    id: 2,
    nombre: "Blanqueamiento Especial",
    servicio: "Blanqueamiento",
    descuento: 15,
    estado: "Activo",
    desde: "31/10/2024",
    hasta: "30/12/2024",
  },
];

const VACIO = {
  nombre: "",
  servicio: "",
  descuento: "",
  estado: "Activo",
  desde: "",
  hasta: "",
};

// ── Modal ─────────────────────────────────────────────────
function Modal({ oferta, onClose, onSave }) {
  const [form, setForm] = useState(oferta || VACIO);
  const esNuevo = !oferta;

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
          <h3>{esNuevo ? "Nueva Oferta" : "Editar Oferta"}</h3>
          <button className="modal-close" onClick={onClose}><CloseIcon /></button>
        </div>

        <form className="modal-form" onSubmit={handleSubmit}>
          <div className="modal-field">
            <label>Nombre *</label>
            <input name="nombre" value={form.nombre} onChange={handleChange}
              placeholder="Ej. Limpieza de Verano" required />
          </div>

          <div className="modal-field">
            <label>Servicio</label>
            <input name="servicio" value={form.servicio} onChange={handleChange}
              placeholder="Ej. Limpieza Dental" />
          </div>

          <div className="modal-row">
            <div className="modal-field">
              <label>Descuento (%)</label>
              <input name="descuento" type="number" value={form.descuento}
                onChange={handleChange} placeholder="0" min="1" max="100" />
            </div>
            <div className="modal-field">
              <label>Estado</label>
              <select name="estado" value={form.estado} onChange={handleChange}>
                <option value="Activo">Activo</option>
                <option value="Inactivo">Inactivo</option>
              </select>
            </div>
          </div>

          <div className="modal-row">
            <div className="modal-field">
              <label>Válido desde</label>
              <input name="desde" value={form.desde} onChange={handleChange}
                placeholder="DD/MM/AAAA" />
            </div>
            <div className="modal-field">
              <label>Válido hasta</label>
              <input name="hasta" value={form.hasta} onChange={handleChange}
                placeholder="DD/MM/AAAA" />
            </div>
          </div>

          <div className="modal-actions">
            <button type="button" className="btn-cancelar" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn-guardar">
              {esNuevo ? "Crear oferta" : "Guardar cambios"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ── Tarjeta de oferta ─────────────────────────────────────
function OfertaCard({ oferta, onEdit, onDelete }) {
  const activo = oferta.estado === "Activo";

  return (
    <div className="oferta-card">
      <div className="oferta-card__top">
        <div>
          <h3 className="oferta-nombre">{oferta.nombre}</h3>
          <p className="oferta-servicio">{oferta.servicio}</p>
        </div>
        <span className={`oferta-badge ${activo ? "oferta-badge--activo" : "oferta-badge--inactivo"}`}>
          {oferta.estado}
        </span>
      </div>

      <div className="oferta-descuento">
        {oferta.descuento}% OFF
      </div>

      <div className="oferta-fechas">
        <div className="oferta-fecha-row">
          <span className="oferta-fecha-label">Válido desde:</span>
          <span className="oferta-fecha-value">{oferta.desde}</span>
        </div>
        <div className="oferta-fecha-row">
          <span className="oferta-fecha-label">Válido hasta:</span>
          <span className="oferta-fecha-value">{oferta.hasta}</span>
        </div>
      </div>

      <div className="oferta-acciones">
        <button className="oferta-btn oferta-btn--edit" onClick={() => onEdit(oferta)}>
          <EditIcon /> Editar
        </button>
        <button className="oferta-btn oferta-btn--delete" onClick={() => onDelete(oferta.id)}>
          <DeleteIcon /> Eliminar
        </button>
      </div>
    </div>
  );
}

// ── Componente principal ──────────────────────────────────
export default function Ofertas() {
  const [ofertas, setOfertas] = useState(OFERTAS_INICIALES);
  const [modal, setModal] = useState(null);

  const handleEditar = (oferta) => setModal(oferta);

  const handleEliminar = (id) =>
    setOfertas(prev => prev.filter(o => o.id !== id));

  const handleGuardar = (form) => {
    if (modal === "nuevo") {
      setOfertas(prev => [
        ...prev,
        { ...form, id: Date.now(), descuento: Number(form.descuento) },
      ]);
    } else {
      setOfertas(prev =>
        prev.map(o => o.id === modal.id
          ? { ...o, ...form, descuento: Number(form.descuento) }
          : o
        )
      );
    }
    setModal(null);
  };

  return (
    <main className="content">

      {/* Encabezado */}
      <div className="ofertas-header">
        <div>
          <h2 className="ofertas-title">Gestión de Ofertas</h2>
          <p className="ofertas-subtitle">Administra descuentos y promociones</p>
        </div>
        <button className="btn-nueva-cita" onClick={() => setModal("nuevo")}>
          <PlusIcon />
          Nueva Oferta
        </button>
      </div>

      {/* Grid */}
      <div className="ofertas-grid">
        {ofertas.map(o => (
          <OfertaCard
            key={o.id}
            oferta={o}
            onEdit={handleEditar}
            onDelete={handleEliminar}
          />
        ))}
        {ofertas.length === 0 && (
          <p className="ofertas-empty">No hay ofertas registradas.</p>
        )}
      </div>

      {/* Modal */}
      {modal !== null && (
        <Modal
          oferta={modal === "nuevo" ? null : modal}
          onClose={() => setModal(null)}
          onSave={handleGuardar}
        />
      )}

    </main>
  );
}