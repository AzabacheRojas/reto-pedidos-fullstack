import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '@/context/useAuth';

const linkClass = ({ isActive }: { isActive: boolean }) =>
    `rounded-md px-3 py-2 text-sm font-medium transition ${isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
    }`;

export default function Layout() {
    const { usuario, logout } = useAuth();
    const navigate = useNavigate();
    const [open, setOpen] = useState(false);

    const handleLogout = () => {
        logout('manual');
        navigate('/login', { replace: true });
    };

    const initials = usuario?.nombre
        .split(' ')
        .map((p) => p[0])
        .slice(0, 2)
        .join('')
        .toUpperCase();

    return (
        <div className="min-h-screen">
            <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/90 backdrop-blur">
                <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
                    <div className="flex items-center gap-8">
                        <NavLink to="/pedidos" className="flex items-center gap-2 font-semibold text-slate-900">
                            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-white">
                                <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth={2}>
                                    <path d="M21 8l-9-5-9 5 9 5 9-5zM3 8v8l9 5 9-5V8M12 13v8" strokeLinecap="round" strokeLinejoin="round" />
                                </svg>
                            </span>
                            Pedidos
                        </NavLink>
                        <nav className="hidden gap-1 sm:flex">
                            <NavLink to="/pedidos" end className={linkClass}>
                                Listado
                            </NavLink>
                            <NavLink to="/pedidos/nuevo" className={linkClass}>
                                Nuevo pedido
                            </NavLink>
                        </nav>
                    </div>

                    <div className="hidden items-center gap-3 sm:flex">
                        <div className="text-right leading-tight">
                            <p className="text-sm font-medium text-slate-900">{usuario?.nombre}</p>
                            <p className="text-xs text-slate-500">
                                {usuario?.email} ·{' '}
                                <span className={usuario?.rol === 'Admin' ? 'text-brand-600' : ''}>{usuario?.rol}</span>
                            </p>
                        </div>
                        <span className="flex h-9 w-9 items-center justify-center rounded-full bg-slate-200 text-sm font-semibold text-slate-700">
                            {initials}
                        </span>
                        <button onClick={handleLogout} className="btn-secondary">
                            Salir
                        </button>
                    </div>

                    <button
                        className="btn-ghost sm:hidden"
                        onClick={() => setOpen((o) => !o)}
                        aria-label="Abrir menú"
                        aria-expanded={open}
                    >
                        <svg viewBox="0 0 24 24" className="h-6 w-6" fill="none" stroke="currentColor" strokeWidth={2}>
                            <path d={open ? 'M6 6l12 12M6 18L18 6' : 'M4 6h16M4 12h16M4 18h16'} strokeLinecap="round" />
                        </svg>
                    </button>
                </div>

                {open && (
                    <div className="border-t border-slate-200 px-4 py-3 sm:hidden">
                        <nav className="flex flex-col gap-1" onClick={() => setOpen(false)}>
                            <NavLink to="/pedidos" end className={linkClass}>
                                Listado
                            </NavLink>
                            <NavLink to="/pedidos/nuevo" className={linkClass}>
                                Nuevo pedido
                            </NavLink>
                        </nav>
                        <div className="mt-3 flex items-center justify-between border-t border-slate-100 pt-3">
                            <span className="text-sm text-slate-600">
                                {usuario?.nombre} · {usuario?.rol}
                            </span>
                            <button onClick={handleLogout} className="btn-secondary">
                                Salir
                            </button>
                        </div>
                    </div>
                )}
            </header>

            <main className="mx-auto max-w-6xl px-4 py-8">
                <Outlet />
            </main>
        </div>
    );
}