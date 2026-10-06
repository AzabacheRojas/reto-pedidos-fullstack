import type { Usuario } from '@/types';

const KEY = 'pedidos.session';

export interface Session {
    token: string;
    usuario: Usuario;
    expiresAt: number; // epoch ms
}

export const authStorage = {
    get(): Session | null {
        try {
            const raw = localStorage.getItem(KEY);
            if (!raw) return null;
            const session = JSON.parse(raw) as Session;
            if (!session.token || Date.now() >= session.expiresAt) {
                localStorage.removeItem(KEY);
                return null;
            }
            return session;
        } catch {
            return null;
        }
    },
    set(session: Session) {
        try {
            localStorage.setItem(KEY, JSON.stringify(session));
        } catch {
            /* almacenamiento no disponible (modo privado): la sesión vive solo en memoria */
        }
    },
    clear() {
        try {
            localStorage.removeItem(KEY);
        } catch {
            /* ignorar */
        }
    },
};

/** Evento global para cerrar sesión cuando la API responde 401 (token expirado/inválido). */
export const SESSION_EXPIRED_EVENT = 'auth:session-expired';