export const ESTADOS = ['Registrado', 'EnProceso', 'Despachado', 'Entregado', 'Cancelado'] as const;
export type EstadoPedido = (typeof ESTADOS)[number];

export const ESTADO_LABEL: Record<EstadoPedido, string> = {
    Registrado: 'Registrado',
    EnProceso: 'En proceso',
    Despachado: 'Despachado',
    Entregado: 'Entregado',
    Cancelado: 'Cancelado',
};

export interface Pedido {
    id: number;
    numeroPedido: string;
    cliente: string;
    fecha: string; // ISO (yyyy-MM-ddTHH:mm:ss)
    total: number;
    estado: EstadoPedido;
    createdAt: string;
    updatedAt?: string;
}

export interface PedidoInput {
    numeroPedido: string;
    cliente: string;
    fecha: string; // yyyy-MM-dd
    total: number;
    estado: EstadoPedido;
}

export interface PagedResult<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
}

export type SortField = 'numeroPedido' | 'cliente' | 'fecha' | 'total' | 'estado';

export interface PedidoQuery {
    page: number;
    pageSize: number;
    search?: string;
    estado?: EstadoPedido | '';
    sortBy?: SortField;
    desc?: boolean;
}

export type Rol = 'Admin' | 'User';

export interface Usuario {
    email: string;
    nombre: string;
    rol: Rol;
}

export interface LoginResponse {
    token: string;
    expiresIn: number;
    usuario: Usuario;
}

/** RFC 7807 ProblemDetails devuelto por la API. */
export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    errors?: Record<string, string[]>;
    traceId?: string;
}