import axios, { AxiosError } from 'axios';
import { authStorage, SESSION_EXPIRED_EVENT } from './authStorage';

export const http = axios.create({
    baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5080',
    timeout: 15_000,
    headers: { 'Content-Type': 'application/json' },
});

// Adjunta el JWT en cada request.
http.interceptors.request.use((config) => {
    const session = authStorage.get();
    if (session) config.headers.Authorization = `Bearer ${session.token}`;
    return config;
});

// 401 en un endpoint protegido ⇒ sesión inválida/expirada ⇒ logout global.
http.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
        const isLogin = error.config?.url?.includes('/auth/login');
        if (error.response?.status === 401 && !isLogin) {
            authStorage.clear();
            window.dispatchEvent(new CustomEvent(SESSION_EXPIRED_EVENT));
        }
        return Promise.reject(error);
    },
);