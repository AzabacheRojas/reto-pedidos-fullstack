import axios from 'axios';
import type { ProblemDetails } from '@/types';

export interface ApiError {
    status?: number;
    message: string;
    fieldErrors: Record<string, string>;
}

/** Normaliza cualquier error (red, timeout, ProblemDetails) a un formato único para la UI. */
export function toApiError(error: unknown): ApiError {
    if (axios.isAxiosError(error)) {
        if (!error.response) {
            return {
                message: error.code === 'ECONNABORTED'
                    ? 'La solicitud tardó demasiado. Intente nuevamente.'
                    : 'No se pudo conectar con el servidor. Verifique que la API esté en ejecución.',
                fieldErrors: {},
            };
        }

        const data = error.response.data as ProblemDetails | undefined;
        const fieldErrors: Record<string, string> = {};
        for (const [key, messages] of Object.entries(data?.errors ?? {})) {
            const field = key.charAt(0).toLowerCase() + key.slice(1);
            if (messages.length) fieldErrors[field] = messages[0];
        }

        return {
            status: error.response.status,
            message: data?.detail ?? data?.title ?? `Error ${error.response.status}`,
            fieldErrors,
        };
    }

    return { message: error instanceof Error ? error.message : 'Error inesperado', fieldErrors: {} };
}