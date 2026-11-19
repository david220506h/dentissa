import { useState } from "react";
import Nav from "../../components/nav";
import "./Dashboard.css";
import Citas from "../../components/citas";
import Pacientes from "../../components/pacientes";
import Servicios from "../../components/servicios";
import Ofertas from "../../components/ofertas";
import Usuarios from "../../components/usuarios";
import Comentarios from "../../components/comentarios"
import { FaUserCircle } from 'react-icons/fa';
import { ROLES } from "../../constants/roles";

// ── Íconos ───────────────────────────────────────────────
const Icons = {
  dashboard: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
    </svg>
  ),
  citas: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" /><line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  ),
  pacientes: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  ),
  servicios: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" /><polyline points="14 2 14 8 20 8" /><line x1="16" y1="13" x2="8" y2="13" /><line x1="16" y1="17" x2="8" y2="17" />
    </svg>
  ),
  ofertas: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="12" y1="1" x2="12" y2="23" /><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6" />
    </svg>
  ),
  comentarios: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="18" height="14" rx="2" /><line x1="8" y1="21" x2="16" y2="21" /><line x1="12" y1="17" x2="12" y2="21" />
    </svg>
  ),
  usuarios: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="8.5" cy="7" r="4" /><line x1="20" y1="8" x2="20" y2="14" /><line x1="23" y1="11" x2="17" y2="11" />
    </svg>
  ),
  pulse: () => (
    <svg width="38" height="38" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
    </svg>
  ),
  clock: () => (
    <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#F5A623" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10" /><polyline points="12 6 12 12 16 14" />
    </svg>
  ),
  patients: () => (
    <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#E91E8C" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  ),
  plus: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" />
    </svg>
  ),
  addPatient: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="8.5" cy="7" r="4" /><line x1="20" y1="8" x2="20" y2="14" /><line x1="23" y1="11" x2="17" y2="11" />
    </svg>
  ),
  viewPatients: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" />
    </svg>
  ),
  search: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="11" cy="11" r="8" /><line x1="21" y1="21" x2="16.65" y2="16.65" />
    </svg>
  ),
  close: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
    </svg>
  ),
  phone: () => (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 12 19.79 19.79 0 0 1 1.61 3.39 2 2 0 0 1 3.6 1.21h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 8.84a16 16 0 0 0 6 6l.95-.95a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 21.73 16.92z" />
    </svg>
  ),
  mail: () => (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
      <polyline points="22,6 12,13 2,6" />
    </svg>
  ),
  calendar: () => (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" />
      <line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" />
    </svg>
  ),
};

// ── Lista de pacientes compartida ─────────────────────────
const PACIENTES_DATA = [
  { id: 1, nombre: "María García López",   edad: 34, telefono: "+34 600 111 222", email: "maria.garcia@email.com",  proximaCita: "27/11/2024" },
  { id: 2, nombre: "Juan Pérez Martínez",  edad: 28, telefono: "+34 600 333 444", email: "juan.perez@email.com",    proximaCita: "27/11/2024" },
  { id: 3, nombre: "Ana Rodríguez Santos", edad: 45, telefono: "+34 600 555 666", email: "ana.rodriguez@email.com", proximaCita: "27/11/2024" },
];

function getInitials(nombre) {
  return nombre.split(" ").slice(0, 3).map(n => n[0]).join("").toUpperCase();
}

// ── Modal Ver Pacientes ───────────────────────────────────
function ModalPacientes({ onClose }) {
  const [busqueda, setBusqueda] = useState("");

  const filtrados = PACIENTES_DATA.filter(p =>
    p.nombre.toLowerCase().includes(busqueda.toLowerCase())
  );

  return (
    <div className="vp-overlay" onClick={onClose}>
      <div className="vp-modal" onClick={e => e.stopPropagation()}>
        <div className="vp-header">
          <div>
            <h3 className="vp-title">Pacientes</h3>
            <p className="vp-subtitle">{PACIENTES_DATA.length} pacientes registrados</p>
          </div>
          <button className="vp-close" onClick={onClose}><Icons.close /></button>
        </div>

        <div className="vp-search">
          <Icons.search />
          <input
            autoFocus
            type="text"
            placeholder="Buscar paciente por nombre..."
            value={busqueda}
            onChange={e => setBusqueda(e.target.value)}
          />
          {busqueda && (
            <button className="vp-clear" onClick={() => setBusqueda("")}>
              <Icons.close />
            </button>
          )}
        </div>

        <div className="vp-list">
          {filtrados.length > 0 ? (
            filtrados.map(p => (
              <div key={p.id} className="vp-card">
                <div className="vp-card__left">
                  <div className="vp-avatar">{getInitials(p.nombre)}</div>
                  <div>
                    <p className="vp-nombre">{p.nombre}</p>
                    <p className="vp-edad">{p.edad} años</p>
                  </div>
                </div>
                <div className="vp-card__right">
                  <div className="vp-info-row"><Icons.phone /><span>{p.telefono}</span></div>
                  <div className="vp-info-row"><Icons.mail /><span>{p.email}</span></div>
                  <div className="vp-info-row"><Icons.calendar /><span>Próxima: {p.proximaCita}</span></div>
                </div>
              </div>
            ))
          ) : (
            <div className="vp-empty">
              <p>No se encontró ningún paciente con ese nombre</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Nav items ────────────────────────────────────────────
// roles: quién ve cada sección (el backend vuelve a validar en cada petición)
const RECEPCION = [ROLES.ADMIN, ROLES.SECRETARIA];
const SOLO_ADMIN = [ROLES.ADMIN];

const NAV_ITEMS = [
  { id: "dashboard",   label: "Dashboard",   icon: Icons.dashboard,   roles: RECEPCION },
  { id: "citas",       label: "Citas",        icon: Icons.citas,       roles: RECEPCION },
  { id: "pacientes",   label: "Pacientes",    icon: Icons.pacientes,   roles: RECEPCION },
  { id: "servicios",   label: "Servicios",    icon: Icons.servicios,   roles: SOLO_ADMIN },
  { id: "ofertas",     label: "Ofertas",      icon: Icons.ofertas,     roles: SOLO_ADMIN },
  { id: "comentarios", label: "Comentarios",  icon: Icons.comentarios, roles: SOLO_ADMIN },
  { id: "usuarios",    label: "Usuarios",     icon: Icons.usuarios,    roles: SOLO_ADMIN },
];

const itemsPorRol = (rol) => NAV_ITEMS.filter((item) => item.roles.includes(rol));

// ── Panel nav ────────────────────────────────────────────
function PanelNav({ items, active, onNavigate }) {
  return (
    <nav className="nav">
      <div className="panel-nav">
        {items.map(({ id, label, icon: Icon }) => (
          <button
            key={id}
            className={`panel-nav__item${active === id ? " panel-nav__item--active" : ""}`}
            onClick={() => onNavigate(id)}
            aria-current={active === id ? "page" : undefined}
          >
            <Icon />
            {label}
          </button>
        ))}
      </div>
    </nav>
  );
}

// ── Sección: Dashboard ───────────────────────────────────
function SectionDashboard({ usuario, onVerPacientes, onNuevaCita, onNuevoPaciente }) {
  return (
    <main className="content">
      <div className="welcome-card">
        <div>
          <h2>¡Bienvenido/a {usuario.nombre}</h2>
          <p>Gestiona todo tu consultorio desde un solo lugar</p>
        </div>
        <div className="welcome-icon">
          <Icons.pulse />
        </div>
      </div>

      <div className="stats-row">
        <div className="stat-card">
          <div>
            <div className="stat-label">Pendientes</div>
            <div className="stat-value">0</div>
          </div>
          <div className="stat-icon"><Icons.clock /></div>
        </div>
        <div className="stat-card">
          <div>
            <div className="stat-label">Pacientes</div>
            <div className="stat-value">{PACIENTES_DATA.length}</div>
          </div>
          <div className="stat-icon"><Icons.patients /></div>
        </div>
      </div>

      <div className="quick-actions">
        <div className="actions-grid">

          {/* Nueva Cita */}
          <button className="action-btn" onClick={onNuevaCita}>
            <div className="btn-icon"><Icons.plus /></div>
            <div className="btn-text">
              <strong>Nueva Cita</strong>
              <span>Agendar cita</span>
            </div>
          </button>

          {/* Nuevo Paciente */}
          <button className="action-btn" onClick={onNuevoPaciente}>
            <div className="btn-icon"><Icons.addPatient /></div>
            <div className="btn-text">
              <strong>Nuevo Paciente</strong>
              <span>Registrar paciente</span>
            </div>
          </button>

          {/* Ver Pacientes */}
          <button className="action-btn" onClick={onVerPacientes}>
            <div className="btn-icon"><Icons.viewPatients /></div>
            <div className="btn-text">
              <strong>Ver Pacientes</strong>
              <span>{PACIENTES_DATA.length} pacientes</span>
            </div>
          </button>

        </div>
      </div>
    </main>
  );
}

// ── Placeholder secciones vacías ─────────────────────────
function SectionVacia({ nombre }) {
  return (
    <main className="content">
      <div className="welcome-card">
        <div>
          <h2 style={{ textTransform: "capitalize" }}>{nombre}</h2>
          <p>Esta sección está en construcción</p>
        </div>
        <div className="welcome-icon"><Icons.pulse /></div>
      </div>
    </main>
  );
}

// ── Root: Dashboard ──────────────────────────────────────
function Dashboard({ usuario, setUsuario, onVerPrincipal }) {
  const [seccion,      setSeccion]      = useState("dashboard");
  const [verPacientes, setVerPacientes] = useState(false);

  const items = itemsPorRol(usuario.rol);
  const permitida = (id) => items.some((item) => item.id === id);

  const renderSeccion = () => {
    if (!permitida(seccion)) return (
      <main className="content">
        <div className="welcome-card">
          <div>
            <h2>Sin acceso</h2>
            <p>No tienes permiso para ver esta sección.</p>
          </div>
        </div>
      </main>
    );
    switch (seccion) {
      case "dashboard":   return (
        <SectionDashboard 
          usuario={usuario}
          onVerPacientes={() => setVerPacientes(true)}
          onNuevaCita={() => setSeccion("citas")}
          onNuevoPaciente={() => setSeccion("pacientes")}
        />
      );
      case "citas":       return <Citas />;
      case "pacientes":   return <Pacientes />;
      case "servicios":   return <Servicios />;
      case "comentarios": return <Comentarios />;
      case "ofertas":     return <Ofertas />;
      case "usuarios":    return <Usuarios />;
      default:            return <SectionVacia nombre={seccion} />;
    }
  };

  return (
    <div>
      <Nav
        usuario={usuario}
        setUsuario={setUsuario}
        mostrarMenu={false}
        onVerPrincipal={onVerPrincipal}
      />
      <PanelNav items={items} active={seccion} onNavigate={setSeccion} />
      {renderSeccion()}

      {verPacientes && (
        <ModalPacientes onClose={() => setVerPacientes(false)} />
      )}
    </div>
  );
}

export default Dashboard;