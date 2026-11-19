import { useState } from "react";
import "./Comentarios.css";

const COMENTARIOS_INICIALES = [
  { id: 1, nombre: "María García",  servicio: "Limpieza Dental", comentario: "Excelente servicio, muy profesional",    fecha: "19/11/2024", estrellas: 5 },
  { id: 2, nombre: "Juan Pérez",    servicio: "Ortodoncia",      comentario: "Muy satisfecho con el tratamiento",      fecha: "21/11/2024", estrellas: 4 },
  { id: 3, nombre: "Ana Rodríguez", servicio: "Blanqueamiento",  comentario: "Resultado increíble, lo recomiendo 100%", fecha: "25/11/2024", estrellas: 5 },
];

const DeleteIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
    <path d="M10 11v6" /><path d="M14 11v6" />
    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
  </svg>
);

function Estrellas({ valor }) {
  return (
    <div className="estrellas">
      {[1, 2, 3, 4, 5].map(i => (
        <span key={i} className={i <= valor ? "estrella on" : "estrella off"}>★</span>
      ))}
    </div>
  );
}

export default function Comentarios() {
  const [comentarios, setComentarios] = useState(COMENTARIOS_INICIALES);

  const handleEliminar = (id) =>
    setComentarios(prev => prev.filter(c => c.id !== id));

  return (
    <main className="content">
      <div className="com-header">
        <div>
          <h2 className="com-title">Gestión de Comentarios</h2>
          <p className="com-subtitle">Revisa y administra los comentarios de pacientes</p>
        </div>
      </div>

      <div className="com-list">
        {comentarios.map(c => (
          <div key={c.id} className="com-card">
            <div className="com-card__top">
              <div>
                <p className="com-nombre">{c.nombre}</p>
                <p className="com-servicio">{c.servicio}</p>
              </div>
              <div className="com-card__right">
                <Estrellas valor={c.estrellas} />
                <button className="com-delete" title="Eliminar" onClick={() => handleEliminar(c.id)}>
                  <DeleteIcon />
                </button>
              </div>
            </div>
            <p className="com-texto">{c.comentario}</p>
            <p className="com-fecha">{c.fecha}</p>
          </div>
        ))}

        {comentarios.length === 0 && (
          <p className="com-empty">No hay comentarios registrados.</p>
        )}
      </div>
    </main>
  );
}