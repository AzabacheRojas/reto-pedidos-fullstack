import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { toApiError } from '@/api/errors';
import Spinner from '@/components/Spinner';
import { useAuth } from '@/context/useAuth';

const DEMO_USERS = [
    { label: 'Admin', email: 'admin@pedidos.com', password: 'Admin123*' },
    { label: 'Usuario', email: 'user@pedidos.com', password: 'User123*' },
];

export default function LoginPage() {
    const { login, isAuthenticated, logoutReason } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const from = (location.state as { from?: string } | null)?.from ?? '/pedidos';

    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    if (isAuthenticated) return <Navigate to={from} replace />;

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (!email.trim() || !password) {
            setError('Ingrese su email y contraseña.');
            return;
        }
        setLoading(true);
        setError(null);
        try {
            await login(email.trim(), password);
            navigate(from, { replace: true });
        } catch (err) {
            const apiError = toApiError(err);
            setError(apiError.status === 429 ? 'Demasiados intentos. Espere un minuto e intente de nuevo.' : apiError.message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-brand-50 via-white to-slate-100 px-4">
            <div className="w-full max-w-md">
                <div className="mb-8 text-center">
                    <span className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-brand-600 text-white shadow-lg shadow-brand-600/30">
                        <svg viewBox="0 0 24 24" className="h-7 w-7" fill="none" stroke="currentColor" strokeWidth={2}>
                            <path d="M21 8l-9-5-9 5 9 5 9-5zM3 8v8l9 5 9-5V8M12 13v8" strokeLinecap="round" strokeLinejoin="round" />
                        </svg>
                    </span>
                    <h1 className="text-2xl font-bold text-slate-900">Gestión de Pedidos</h1>
                    <p className="mt-1 text-sm text-slate-500">Inicie sesión para continuar</p>
                </div>

                <form onSubmit={handleSubmit} noValidate className="card space-y-5 p-8">
                    {logoutReason === 'expired' && !error && (
                        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
                            Su sesión expiró. Inicie sesión nuevamente.
                        </div>
                    )}
                    {error && (
                        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
                            {error}
                        </div>
                    )}

                    <div>
                        <label htmlFor="email" className="label">
                            Email
                        </label>
                        <input
                            id="email"
                            type="email"
                            autoComplete="username"
                            className="input"
                            placeholder="usuario@email.com"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            autoFocus
                        />
                    </div>

                    <div>
                        <label htmlFor="password" className="label">
                            Contraseña
                        </label>
                        <div className="relative">
                            <input
                                id="password"
                                type={showPassword ? 'text' : 'password'}
                                autoComplete="current-password"
                                className="input pr-20"
                                placeholder="••••••••"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                            />
                            <button
                                type="button"
                                onClick={() => setShowPassword((s) => !s)}
                                className="absolute inset-y-0 right-2 my-auto h-7 rounded px-2 text-xs font-medium text-slate-500 hover:bg-slate-100"
                            >
                                {showPassword ? 'Ocultar' : 'Mostrar'}
                            </button>
                        </div>
                    </div>

                    <button type="submit" className="btn-primary w-full py-2.5" disabled={loading}>
                        {loading && <Spinner className="h-4 w-4" />}
                        {loading ? 'Ingresando…' : 'Ingresar'}
                    </button>

                    <div className="border-t border-slate-100 pt-4">
                        <p className="mb-2 text-center text-xs text-slate-500">Accesos de demostración</p>
                        <div className="grid grid-cols-2 gap-2">
                            {DEMO_USERS.map((u) => (
                                <button
                                    key={u.email}
                                    type="button"
                                    className="btn-secondary text-xs"
                                    onClick={() => {
                                        setEmail(u.email);
                                        setPassword(u.password);
                                        setError(null);
                                    }}
                                >
                                    {u.label}
                                </button>
                            ))}
                        </div>
                    </div>
                </form>
            </div>
        </div>
    );
}