import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import {
  EncabezadoPagina,
  GrupoVista,
  MenuInferior,
  MenuLateral,
  Paginacion,
  Pestanas,
  SinConexion,
  Tabla,
  type GrupoMenu,
} from './index';
import { sinViolacionesAxe } from './prueba/axe';

const grupos: GrupoMenu[] = [
  {
    titulo: 'Principal',
    elementos: [
      { id: 'inicio', etiqueta: 'Inicio', icono: 'inicio', href: '/' },
      { id: 'agenda', etiqueta: 'Agenda', icono: 'agenda', href: '/agenda' },
    ],
  },
  { titulo: 'Clínica', elementos: [] },
  {
    titulo: 'Administración',
    elementos: [{ id: 'equipo', etiqueta: 'Equipo y usuarios', icono: 'equipo', href: '/equipo' }],
  },
];

describe('MenuLateral', () => {
  it('el contador del menú lateral se lee separado del nombre', () => {
    render(<MenuLateral grupos={[{ titulo: 'Principal', elementos: [{ id: 's', etiqueta: 'Solicitudes', icono: 'solicitudes', href: '/s', contador: { valor: 5, descripcion: '5 por revisar' } }] }]} usuario={{ nombre: 'Ana', rol: 'Asistente' }} alCerrarSesion={() => {}} />);
    expect(screen.getByRole('link', { name: 'Solicitudes, 5 por revisar' })).toBeInTheDocument();
  });

  it('marca la sección actual, oculta grupos vacíos y cierra sesión', async () => {
    const alCerrarSesion = vi.fn();
    render(
      <MenuLateral
        grupos={grupos}
        activo="agenda"
        usuario={{ nombre: 'Laura Mendoza', rol: 'Administradora' }}
        alCerrarSesion={alCerrarSesion}
      />,
    );
    const nav = screen.getByRole('navigation', { name: 'Menú principal' });
    expect(within(nav).getByRole('link', { name: 'Agenda' })).toHaveAttribute('aria-current', 'page');
    expect(within(nav).getByRole('link', { name: 'Inicio' })).not.toHaveAttribute('aria-current');
    expect(within(nav).queryByRole('list', { name: 'Clínica' })).toBeNull();
    expect(screen.getByText('Laura Mendoza')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }));
    expect(alCerrarSesion).toHaveBeenCalledOnce();
  });

  it('usa el componente de enlace de la aplicación', () => {
    const Enlace = ({ href, children, ...resto }: { href: string; children: React.ReactNode }) => (
      <a href={href} data-enrutador="si" {...resto}>
        {children}
      </a>
    );
    render(
      <MenuLateral grupos={grupos} usuario={{ nombre: 'Ana', rol: 'Asistente' }} alCerrarSesion={() => {}} enlace={Enlace} />,
    );
    expect(screen.getByRole('link', { name: 'Inicio' })).toHaveAttribute('data-enrutador', 'si');
  });
});

describe('MenuInferior', () => {
  it('el contador se lee con su descripción y la sección actual queda marcada', () => {
    render(
      <MenuInferior
        activo="inicio"
        elementos={[
          { id: 'inicio', etiqueta: 'Inicio', icono: 'inicio', href: '/' },
          {
            id: 'solicitudes',
            etiqueta: 'Solicitudes',
            icono: 'solicitudes',
            href: '/solicitudes',
            contador: { valor: 3, descripcion: '3 por revisar' },
          },
          { id: 'mas', etiqueta: 'Más', icono: 'menu', href: '/mas' },
        ]}
      />,
    );
    const nav = screen.getByRole('navigation', { name: 'Menú móvil' });
    expect(within(nav).getByRole('link', { name: 'Inicio' })).toHaveAttribute('aria-current', 'page');
    expect(within(nav).getByRole('link', { name: /Solicitudes/ })).toHaveAccessibleName('Solicitudes, 3 por revisar');
  });
});

describe('Encabezado y tabla', () => {
  it('el encabezado es el h1 de la página', () => {
    render(<EncabezadoPagina titulo="Agenda" subtitulo="Semana del 16 al 21 de noviembre de 2026" />);
    expect(screen.getByRole('heading', { level: 1, name: 'Agenda' })).toBeInTheDocument();
  });

  it('la tabla tiene título, encabezados con alcance y región desplazable con foco', () => {
    render(
      <Tabla etiqueta="Pacientes" columnas={['Paciente', 'Teléfono', '']}>
        <tr>
          <td>Rosa Aguilar</td>
          <td>55 5555 0101</td>
          <td>
            <a href="/pacientes/1">Ver ficha</a>
          </td>
        </tr>
      </Tabla>,
    );
    expect(screen.getByRole('table', { name: 'Pacientes' })).toBeInTheDocument();
    expect(screen.getAllByRole('columnheader')).toHaveLength(3);
    expect(screen.getByRole('columnheader', { name: 'Acciones' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Pacientes' })).toHaveAttribute('tabindex', '0');
  });

  it('la paginación deshabilita lo que no aplica', async () => {
    const alSiguiente = vi.fn();
    render(
      <Paginacion desde={1} hasta={8} hayAnterior={false} haySiguiente alAnterior={() => {}} alSiguiente={alSiguiente} />,
    );
    expect(screen.getByText('Mostrando 1 a 8')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Anterior' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Siguiente' }));
    expect(alSiguiente).toHaveBeenCalledOnce();
  });
});

describe('Pestañas y grupo de vista', () => {
  it('las pestañas se recorren con las flechas', async () => {
    render(
      <Pestanas
        etiqueta="Tipo de solicitud"
        pestanas={[
          { id: 'pendientes', etiqueta: 'Pendientes', contador: 5, descripcionContador: 'solicitudes', contenido: <p>Lista de pendientes</p> },
          { id: 'historial', etiqueta: 'Historial', contenido: <p>Lista del historial</p> },
        ]}
      />,
    );
    expect(screen.getByRole('tab', { name: 'Pendientes, 5 solicitudes' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByText('Lista de pendientes')).toBeInTheDocument();
    await userEvent.tab();
    await userEvent.keyboard('{ArrowRight}');
    expect(screen.getByRole('tab', { name: 'Historial' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByText('Lista del historial')).toBeInTheDocument();
  });

  it('el grupo Día/Semana usa aria-pressed', async () => {
    function Ejemplo() {
      const [vista, setVista] = useState<'dia' | 'semana'>('semana');
      return (
        <GrupoVista
          etiqueta="Vista"
          valor={vista}
          alCambiar={setVista}
          opciones={[
            { valor: 'dia', texto: 'Día' },
            { valor: 'semana', texto: 'Semana' },
          ]}
        />
      );
    }
    render(<Ejemplo />);
    expect(screen.getByRole('button', { name: 'Semana' })).toHaveAttribute('aria-pressed', 'true');
    await userEvent.click(screen.getByRole('button', { name: 'Día' }));
    expect(screen.getByRole('button', { name: 'Día' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Semana' })).toHaveAttribute('aria-pressed', 'false');
  });
});

describe('SinConexion', () => {
  it('muestra el texto del diseño y reintenta', async () => {
    const alReintentar = vi.fn();
    render(<SinConexion alReintentar={alReintentar} />);
    expect(screen.getByRole('heading', { level: 1, name: 'Sin conexión con la clínica' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }));
    expect(alReintentar).toHaveBeenCalledOnce();
  });
});

describe('accesibilidad (axe)', () => {
  it('navegación y página no tienen violaciones', async () => {
    const { container } = render(
      <div>
        <MenuLateral
          grupos={grupos}
          activo="inicio"
          usuario={{ nombre: 'Laura Mendoza', rol: 'Administradora' }}
          alCerrarSesion={() => {}}
        />
        <main>
          <EncabezadoPagina titulo="Pacientes" subtitulo="Registro de pacientes de la clínica" />
          <Pestanas
            etiqueta="Filtro"
            pestanas={[{ id: 'todos', etiqueta: 'Todos', contador: 8, contenido: <p>Todos</p> }]}
          />
          <Tabla
            etiqueta="Pacientes"
            columnas={['Paciente', '']}
            pie={<Paginacion desde={1} hasta={1} hayAnterior={false} haySiguiente={false} alAnterior={() => {}} alSiguiente={() => {}} />}
          >
            <tr>
              <td>Rosa Aguilar</td>
              <td>
                <a href="/pacientes/1">Ver ficha</a>
              </td>
            </tr>
          </Tabla>
        </main>
        <MenuInferior activo="inicio" elementos={grupos[0]!.elementos} />
      </div>,
    );
    await sinViolacionesAxe(container);
  });
});
