import { http } from './http';
import type { LoginResponse } from '@/types';

export const authApi = {
    login: (email: string, password: string) =>
        http.post<LoginResponse>('/auth/login', { email, password }).then((r) => r.data),
};