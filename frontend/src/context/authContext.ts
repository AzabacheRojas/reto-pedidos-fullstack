import { createContext } from 'react';
import type { Usuario } from '@/types';

export type LogoutReason = 'manual' | 'expired';

export interface AuthContextValue {
    usuario: Usuario | null;
    isAuthenticated: boolean;
    isAdmin: boolean;
    expiresAt: number | null;
    login: (email: string, password: string) => Promise<void>;
    logout: (reason?: LogoutReason) => void;
    logoutReason: LogoutReason | null;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);