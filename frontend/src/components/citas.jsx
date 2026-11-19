import { useState, useEffect } from "react";
import { authFetch, esErrorDeAcceso } from "../services/authService";
import "./citas.css";

const API      = "/api/Citas";
const API_CLI  = "/api/Citas/clientes";
const API_TRAB = "/api/Citas/trabajadores";
const API_SERV = "/api/Citas/servicios";

// ── Íconos ───────────────────────────────────────────────
const EditIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
  </svg>
);

const DeleteIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6" />
    <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
    <path d="M10 11v6" /><path d="M14 11v6" />
    <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
  </svg>
);

const ConfirmIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
    <polyline points="22 4 12 14.01 9 11.01" />
  </svg>
);

const PlusIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="12" y1="5" x2="12" y2="19" />
    <line x1="5" y1="12" x2="19" y2="12" />
  </svg>
);

const CloseIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
  </svg>
);

const CalIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
    strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
    <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
    <line x1="3" y1="10" x2="21" y2="10"/>
  </svg>
);

// ── Badge estado ─────────────────────────────────────────
function EstadoBadge({ estado }) {
  const clase =
    estado === "confirmada" ? "confirmada" :
    estado === "cancelada"  ? "cancelada"  :
    estado === "completada" ? "completada" : "pendiente";
  return <span className={`cita-badge cita-badge--${clase}`}>{estado}</span>;
}

// ── Modal Nueva Cita ──────────────────────────────────────
function ModalNueva({ onClose, onGuardar }) {
  const [clientes,    setClientes]    = useState([]);
  const [trabajadores, setTrabajadores] = useState([]);
  const [servicios,   setServicios]   = useState([]);
  const [horarios,    setHorarios]    = useState([]);
  const [cargandoH,   setCargandoH]   = useState(false);
  const [guardando,   setGuardando]   = useState(false);
  const [error,       setError]       = useState(null);

  const [form, setForm] = useState({
    clienteId: "", trabajadorId: "", servicioId: "",
    fecha: "", hora: "", motivoExtra: "", agendadoPor: "secretaria",
  });

  // Cargar dropdowns al abrir
  useEffect(() => {
    const cargar = async () => {
      try {
        const [rCli, rTrab, rServ] = await Promise.all([
          authFetch(API_CLI), authFetch(API_TRAB), authFetch(API_SERV),
        ]);
        setClientes(await rCli.json());
        setTrabajadores(await rTrab.json());
        setServicios(await rServ.json());
      } catch (err) {
        setError(esErrorDeAcceso(err) ? err.message : "Error al cargar datos");
      }
    };
    cargar();
  }, []);

  // Cargar horarios al cambiar fecha
  useEffect(() => {
    if (!form.fecha) return;
    const cargarHorarios = async () => {
      setCargandoH(true);
      setForm(prev => ({ ...prev, hora: "" }));
      try {
        const res = await authFetch(`${API}/horarios-disponibles?fecha=${form.fecha}`);
        const data = await res.json();
        setHorarios(data);
      } catch (err) {
        setError(esErrorDeAcceso(err) ? err.message : "Error al cargar horarios");
      } finally {
        setCargandoH(false);
      }
    };
    cargarHorarios();
  }, [form.fecha]);

  const handleChange = (e) =>
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.clienteId || !form.trabajadorId || !form.servicioId || !form.fecha || !form.hora) {
      setError("Completa todos los campos obligatorios");
      return;
    }
    setGuardando(true);
    setError(null);
    try {
      const res = await authFetch(API, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          clienteId:    parseInt(form.clienteId),
          trabajadorId: parseInt(form.trabajadorId),
          servicioId:   parseInt(form.servicioId),
          fecha:        form.fecha,
          hora:         form.hora + ":00",
          motivoExtra:  form.motivoExtra || null,
          agendadoPor:  form.agendadoPor,
        }),
      });
      if (!res.ok) {
        const txt = await res.text();
        let msg = "Error al crear la cita";
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
    <div className="citas-modal-overlay" onClick={onClose}>
      <div className="citas-modal" onClick={e => e.stopPropagation()}>

        <div className="citas-modal-header">
          <div>
            <h3>Agendar Nueva Cita</h3>
            <p>Completa los datos para crear una nueva cita</p>
          </div>
          <button className="citas-modal-close" onClick={onClose}><CloseIcon /></button>
        </div>

        {error && <div className="modal-error">⚠️ {error}</div>}

        <form className="citas-modal-form" onSubmit={handleSubmit}>

          {/* Paciente */}
          <div className="citas-modal-field">
            <label>Paciente <span className="req">*</span></label>
            <select name="clienteId" value={form.clienteId} onChange={handleChange} required>
              <option value="">Seleccionar paciente...</option>
              {clientes.map(c => (
                <option key={c.id} value={c.id}>
                  {c.nombre} {c.apellido}
                </option>
              ))}
            </select>
          </div>

          {/* Trabajador */}
          <div className="citas-modal-field">
            <label>Trabajador <span className="req">*</span></label>
            <select name="trabajadorId" value={form.trabajadorId} onChange={handleChange} required>
              <option value="">Seleccionar trabajador...</option>
              {trabajadores.map(t => (
                <option key={t.id} value={t.id}>
                  {t.nombre} {t.apellido} — {t.especialidad}
                </option>
              ))}
            </select>
          </div>

          {/* Fecha y Hora */}
          <div className="citas-modal-row">
            <div className="citas-modal-field">
              <label>Fecha <span className="req">*</span></label>
              <div className="input-icon">
                <CalIcon />
                <input
                  name="fecha" type="date" value={form.fecha}
                  onChange={handleChange}
                  min={new Date().toISOString().split("T")[0]}
                  required
                />
              </div>
            </div>
            <div className="citas-modal-field">
              <label>Hora <span className="req">*</span></label>
              <select name="hora" value={form.hora} onChange={handleChange}
                disabled={!form.fecha || cargandoH} required>
                <option value="">
                  {cargandoH ? "Cargando..." : !form.fecha ? "Selecciona fecha primero" : "Seleccionar hora..."}
                </option>
                {horarios.map(h => (
                  <option key={h} value={h}>{h}</option>
                ))}
              </select>
            </div>
          </div>

          {/* Servicio */}
          <div className="citas-modal-field">
            <label>Servicio / Tratamiento <span className="req">*</span></label>
            <select name="servicioId" value={form.servicioId} onChange={handleChange} required>
              <option value="">Seleccionar servicio...</option>
              {servicios.map(s => (
                <option key={s.id} value={s.id}>
                  {s.nombre}
                </option>
              ))}
            </select>
          </div>

          {/* Notas adicionales */}
          <div className="citas-modal-field">
            <label>Notas Adicionales</label>
            <textarea
              name="motivoExtra" value={form.motivoExtra}
              onChange={handleChange}
              placeholder="Información adicional sobre la cita..."
              rows={3}
            />
          </div>

          {/* Agendado por */}
          <div className="citas-modal-field">
            <label>Agendado por</label>
            <select name="agendadoPor" value={form.agendadoPor} onChange={handleChange}>
              <option value="secretaria">Secretaria</option>
              <option value="admin">Admin</option>
              <option value="cliente">Cliente</option>
            </select>
          </div>

          <div className="citas-modal-actions">
            <button type="button" className="btn-cancelar" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn-guardar" disabled={guardando}>
              {guardando ? "Agendando..." : "Agendar Cita"}
            </button>
          </div>

        </form>
      </div>
    </div>
  );
}

// ── Modal Editar Cita ─────────────────────────────────────
function ModalEditar({ cita, onClose, onGuardar }) {
  const [trabajadores, setTrabajadores] = useState([]);
  const [horarios,     setHorarios]     = useState([]);
  const [cargandoH,    setCargandoH]    = useState(false);
  const [guardando,    setGuardando]    = useState(false);
  const [error,        setError]        = useState(null);

  const fechaISO = cita.fecha
    ? cita.fecha.split("/").reverse().join("-")
    : "";

  const [form, setForm] = useState({
    trabajadorId: cita.trabajadorId ?? "",
    fecha:        fechaISO,
    hora:         cita.hora ?? "",
    estado:       cita.estado ?? "pendiente",
    costoExtra:   cita.costoExtra ?? "",
    motivoExtra:  cita.motivoExtra ?? "",
  });

  useEffect(() => {
    authFetch(API_TRAB).then(r => r.json()).then(setTrabajadores).catch(() => {});
  }, []);

  useEffect(() => {
    if (!form.fecha) return;
    const cargarHorarios = async () => {
      setCargandoH(true);
      try {
        const res = await authFetch(`${API}/horarios-disponibles?fecha=${form.fecha}`);
        const data = await res.json();
        // Incluir la hora actual de la cita aunque ya esté "ocupada"
        if (cita.hora && !data.includes(cita.hora)) data.unshift(cita.hora);
        setHorarios(data);
      } catch {} finally { setCargandoH(false); }
    };
    cargarHorarios();
  }, [form.fecha]);

  const handleChange = (e) =>
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    setGuardando(true);
    setError(null);
    try {
      const res = await authFetch(`${API}/${cita.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          trabajadorId: parseInt(form.trabajadorId),
          fecha:        form.fecha,
          hora:         form.hora + ":00",
          estado:       form.estado,
          costoExtra:   form.costoExtra ? parseFloat(form.costoExtra) : null,
          motivoExtra:  form.motivoExtra || null,
        }),
      });
      if (!res.ok) {
        const txt = await res.text();
        let msg = "Error al editar la cita";
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
    <div className="citas-modal-overlay" onClick={onClose}>
      <div className="citas-modal" onClick={e => e.stopPropagation()}>

        <div className="citas-modal-header">
          <div>
            <h3>Editar Cita</h3>
            <p>Modifica los datos de la cita</p>
          </div>
          <button className="citas-modal-close" onClick={onClose}><CloseIcon /></button>
        </div>

        {error && <div className="modal-error">⚠️ {error}</div>}

        <form className="citas-modal-form" onSubmit={handleSubmit}>

          <div className="citas-modal-field">
            <label>Trabajador</label>
            <select name="trabajadorId" value={form.trabajadorId} onChange={handleChange}>
              {trabajadores.map(t => (
                <option key={t.id} value={t.id}>
                  {t.nombre} {t.apellido} — {t.especialidad}
                </option>
              ))}
            </select>
          </div>

          <div className="citas-modal-row">
            <div className="citas-modal-field">
              <label>Fecha</label>
              <div className="input-icon">
                <CalIcon />
                <input name="fecha" type="date" value={form.fecha} onChange={handleChange} />
              </div>
            </div>
            <div className="citas-modal-field">
              <label>Hora</label>
              <select name="hora" value={form.hora} onChange={handleChange} disabled={cargandoH}>
                <option value="">{cargandoH ? "Cargando..." : "Seleccionar hora..."}</option>
                {horarios.map(h => (
                  <option key={h} value={h}>{h}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="citas-modal-field">
            <label>Estado</label>
            <select name="estado" value={form.estado} onChange={handleChange}>
              <option value="pendiente">Pendiente</option>
              <option value="confirmada">Confirmada</option>
              <option value="cancelada">Cancelada</option>
              <option value="completada">Completada</option>
            </select>
          </div>

          <div className="citas-modal-row">
            <div className="citas-modal-field">
              <label>Costo Extra</label>
              <input name="costoExtra" type="number" step="0.01"
                value={form.costoExtra} onChange={handleChange} placeholder="0.00" />
            </div>
            <div className="citas-modal-field">
              <label>Motivo Extra</label>
              <input name="motivoExtra" value={form.motivoExtra}
                onChange={handleChange} placeholder="Descripción" />
            </div>
          </div>

          <div className="citas-modal-actions">
            <button type="button" className="btn-cancelar" onClick={onClose}>Cancelar</button>
            <button type="submit" className="btn-guardar" disabled={guardando}>
              {guardando ? "Guardando..." : "Guardar cambios"}
            </button>
          </div>

        </form>
      </div>
    </div>
  );
}

// ── Componente principal ──────────────────────────────────
export default function Citas() {
  const [citas,    setCitas]    = useState([]);
  const [cargando, setCargando] = useState(false);
  const [error,    setError]    = useState(null);
  const [modal,    setModal]    = useState(null); // null | "nuevo" | cita obj

  useEffect(() => { cargarCitas(); }, []);

  const cargarCitas = async () => {
    setCargando(true);
    setError(null);
    try {
      const res = await authFetch(API);
      if (!res.ok) throw new Error("Error al cargar citas");
      setCitas(await res.json());
    } catch (err) {
      setError(err.message);
    } finally {
      setCargando(false);
    }
  };

  const handleEliminar = async (id) => {
    if (!confirm("¿Seguro que deseas eliminar esta cita?")) return;
    try {
      const res = await authFetch(`${API}/${id}`, { method: "DELETE" });
      if (!res.ok) throw new Error("Error al eliminar");
      await cargarCitas();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleConfirmar = async (id) => {
    try {
      const res = await authFetch(`${API}/${id}/confirmar`, { method: "PATCH" });
      if (!res.ok) throw new Error("Error al confirmar");
      await cargarCitas();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleGuardar = () => {
    setModal(null);
    cargarCitas();
  };

  return (
    <main className="content">

      <div className="citas-header">
        <div>
          <h2 className="citas-title">Gestión de Citas</h2>
          <p className="citas-subtitle">Administra todas las citas del sistema</p>
        </div>
        <button className="btn-nueva-cita" onClick={() => setModal("nuevo")}>
          <PlusIcon /> Nueva Cita
        </button>
      </div>

      {error && (
        <div className="citas-error">
          ⚠️ {error}
          <button onClick={() => setError(null)}>✕</button>
        </div>
      )}

      <div className="citas-table-wrapper">
        {cargando && citas.length === 0 ? (
          <p className="citas-empty">Cargando citas...</p>
        ) : (
          <table className="citas-table">
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Servicio</th>
                <th>Fecha</th>
                <th>Hora</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {citas.map(cita => (
                <tr key={cita.id}>
                  <td>{cita.paciente}</td>
                  <td>{cita.servicio}</td>
                  <td>{cita.fecha}</td>
                  <td>{cita.hora}</td>
                  <td><EstadoBadge estado={cita.estado} /></td>
                  <td>
                    <div className="citas-acciones">
                      <button className="cita-btn cita-btn--edit" title="Editar"
                        onClick={() => setModal(cita)}><EditIcon /></button>
                      <button className="cita-btn cita-btn--delete" title="Eliminar"
                        onClick={() => handleEliminar(cita.id)}><DeleteIcon /></button>
                      <button className="cita-btn cita-btn--confirm" title="Confirmar"
                        onClick={() => handleConfirmar(cita.id)}><ConfirmIcon /></button>
                    </div>
                  </td>
                </tr>
              ))}
              {citas.length === 0 && (
                <tr><td colSpan={6} className="citas-empty">No hay citas registradas.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      {modal === "nuevo" && (
        <ModalNueva onClose={() => setModal(null)} onGuardar={handleGuardar} />
      )}
      {modal !== null && modal !== "nuevo" && (
        <ModalEditar cita={modal} onClose={() => setModal(null)} onGuardar={handleGuardar} />
      )}

    </main>
  );
}