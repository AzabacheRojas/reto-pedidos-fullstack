import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '@/api/auth';
import { authStorage, SESSION_EXPIRED_EVENT, type Session } from '@/api/authStorage';
import { AuthContext, type AuthContextValue, type LogoutReason } from './authContext';

export default function AuthProvider({ children }: { children: ReactNode }) {
    // Al cargar la app, recupera la sesión guardada (authStorage descarta las vencidas).
    const [session, setSession] = useState<Session | null>(() => authStorage.get());
    const [logoutReason, setLogoutReason] = useState<LogoutReason | null>(null);

    const logout = useCallback((reason: LogoutReason = 'manual') => {
        authStorage.clear();
        setSession(null);
        setLogoutReason(reason);
    }, []);

    const login = useCallback(async (email: string, password: string) => {
        const res = await authApi.login(email, password);
        const newSession: Session = {
            token: res.token,
            usuario: res.usuario,
            expiresAt: Date.now() + res.expiresIn * 1000,
        };
        authStorage.set(newSession);
        setSession(newSession);
        setLogoutReason(null);
    }, []);

    // Cierre automático al expirar el token. Siempre se programa con setTimeout
    // (si ya venció, el retardo es 0) para no cambiar estado dentro del efecto.
    useEffect(() => {
        if (!session) return;
        const ms = Math.max(session.expiresAt - Date.now(), 0);
        const id = window.setTimeout(() => logout('expired'), Math.min(ms, 2_147_483_647));
        return () => window.clearTimeout(id);
    }, [session, logout]);

    // La API respondió 401 (lo avisa el interceptor de axios).
    useEffect(() => {
        const onExpired = () => logout('expired');
        window.addEventListener(SESSION_EXPIRED_EVENT, onExpired);
        return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired);
    }, [logout]);

    const value = useMemo<AuthContextValue>(
        () => ({
            usuario: session?.usuario ?? null,
            isAuthenticated: !!session,
            isAdmin: session?.usuario.rol === 'Admin',
            expiresAt: session?.expiresAt ?? null,
            login,
            logout,
            logoutReason,
        }),
        [session, login, logout, logoutReason],
    );

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}