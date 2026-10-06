import { http } from './http';
import type { PagedResult, Pedido, PedidoInput, PedidoQuery } from '@/types';

const BASE = '/api/pedidos';

export const pedidosApi = {
    list: (query: PedidoQuery, signal?: AbortSignal) =>
        http
            .get<PagedResult<Pedido>>(BASE, {
                signal,
                params: {
                    ...query,
                    search: query.search || undefined,
                    estado: query.estado || undefined,
                },
            })
            .then((r) => r.data),

    get: (id: number) => http.get<Pedido>(`${BASE}/${id}`).then((r) => r.data),

    create: (input: PedidoInput) => http.post<Pedido>(BASE, input).then((r) => r.data),

    update: (id: number, input: PedidoInput) => http.put<Pedido>(`${BASE}/${id}`, input).then((r) => r.data),

    remove: (id: number) => http.delete<void>(`${BASE}/${id}`),
};