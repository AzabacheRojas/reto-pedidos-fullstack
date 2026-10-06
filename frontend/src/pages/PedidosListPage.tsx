import axios from 'axios';
import { useCallback, useEffect, useState } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { toApiError } from '@/api/errors';
import { pedidosApi } from '@/api/pedidos';
import ConfirmDialog from '@/components/ConfirmDialog';
import EstadoBadge from '@/components/EstadoBadge';
import PageHeader from '@/components/PageHeader';
import Pagination from '@/components/Pagination';
import { useAuth } from '@/context/useAuth';
import { useToast } from '@/context/useToast';
import { useDebounce } from '@/hooks/useDebounce';
import { ESTADOS, ESTADO_LABEL, type EstadoPedido, type PagedResult, type Pedido, type SortField } from '@/types';
import { formatCurrency, formatDate } from '@/utils/format';

const COLUMNS: { key: SortField; label: string; className?: string }[] = [
    { key: 'numeroPedido', label: 'N° Pedido' },
    { key: 'cliente', label: 'Cliente' },
    { key: 'fecha', label: 'Fecha' },
    { key: 'total', label: 'Total', className: 'text-right' },
    { key: 'estado', label: 'Estado' },
];

interface ListState {
    key: string;
    data: PagedResult<Pedido> | null;
    error: string | null;
}

export default function PedidosListPage() {
    const { isAdmin } = useAuth();
    const toast = useToast();
    const navigate = useNavigate();
    const location = useLocation();
    const highlightId = (location.state as { highlight?: number } | null)?.highlight;

    // Filtros sincronizados con la URL: se conservan al recargar o volver atrás.
    const [params, setParams] = useSearchParams();
    const page = Math.max(1, Number(params.get('page')) || 1);
    const pageSize = Number(params.get('pageSize')) || 10;
    const estado = (params.get('estado') ?? '') as EstadoPedido | '';
    const sortBy = (params.get('sortBy') as SortField | null) ?? 'fecha';
    const desc = params.get('desc') !== 'false';
    const urlSearch = params.get('search') ?? '';

    const [searchInput, setSearchInput] = useState(urlSearch);
    const search = useDebounce(searchInput.trim(), 400);

    const [reloadKey, setReloadKey] = useState(0);
    const [toDelete, setToDelete] = useState<Pedido | null>(null);
    const [deleting, setDeleting] = useState(false);

    // Cada combinación de filtros tiene una "clave". El resultado se guarda con la clave
    // a la que pertenece: si no coincide con la actual, estamos cargando (estado derivado).
    const queryKey = JSON.stringify({ page, pageSize, urlSearch, estado, sortBy, desc, reloadKey });
    const [list, setList] = useState<ListState>({ key: '', data: null, error: null });
    const loading = list.key !== queryKey;
    const data = list.data;
    const error = list.key === queryKey ? list.error : null;

    const updateParams = useCallback(
        (changes: Record<string, string | number | boolean | undefined>, resetPage = true) => {
            setParams(
                (prev) => {
                    const next = new URLSearchParams(prev);
                    for (const [k, v] of Object.entries(changes)) {
                        if (v === undefined || v === '') next.delete(k);
                        else next.set(k, String(v));
                    }
                    if (resetPage) next.delete('page');
                    return next;
                },
                { replace: true },
            );
        },
        [setParams],
    );

    // Aplica la búsqueda (con debounce) a la URL.
    useEffect(() => {
        if (urlSearch !== search) updateParams({ search });
    }, [search, urlSearch, updateParams]);

    // Carga el listado cada vez que cambian los filtros.
    useEffect(() => {
        const controller = new AbortController();

        pedidosApi
            .list({ page, pageSize, search: urlSearch, estado, sortBy, desc }, controller.signal)
            .then((result) => {
                // Si se eliminó el último elemento de la última página, retroceder una página.
                if (result.items.length === 0 && result.page > 1 && result.totalPages > 0) {
                    updateParams({ page: result.totalPages }, false);
                    return;
                }
                setList({ key: queryKey, data: result, error: null });
            })
            .catch((err) => {
                if (!axios.isCancel(err)) {
                    setList((prev) => ({ key: queryKey, data: prev.data, error: toApiError(err).message }));
                }
            });

        return () => controller.abort();
    }, [queryKey, page, pageSize, urlSearch, estado, sortBy, desc, updateParams]);

    const toggleSort = (key: SortField) => {
        if (sortBy === key) updateParams({ desc: !desc });
        else updateParams({ sortBy: key, desc: key === 'fecha' || key === 'total' });
    };

    const confirmDelete = async () => {
        if (!toDelete) return;
        setDeleting(true);
        try {
            await pedidosApi.remove(toDelete.id);
            toast.success(`Pedido ${toDelete.numeroPedido} eliminado.`);
            setToDelete(null);
            setReloadKey((k) => k + 1);
        } catch (err) {
            toast.error(toApiError(err).message);
        } finally {
            setDeleting(false);
        }
    };

    const hasFilters = !!urlSearch || !!estado;

    return (
        <>
            <PageHeader
                title="Pedidos"
                subtitle={data ? `${data.totalCount} pedido${data.totalCount === 1 ? '' : 's'} encontrados` : 'Gestione los pedidos registrados'}
                actions={
                    <Link to="/pedidos/nuevo" className="btn-primary">
                        <span className="text-lg leading-none">+</span> Nuevo pedido
                    </Link>
                }
            />

            <div className="card overflow-hidden">
                {/* Filtros */}
                <div className="flex flex-col gap-3 border-b border-slate-200 p-4 sm:flex-row sm:items-center">
                    <div className="relative flex-1">
                        <svg className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2}>
                            <circle cx="11" cy="11" r="7" />
                            <path d="M21 21l-4.3-4.3" strokeLinecap="round" />
                        </svg>
                        <input
                            className="input pl-9"
                            placeholder="Buscar por número o cliente…"
                            value={searchInput}
                            onChange={(e) => setSearchInput(e.target.value)}
                            aria-label="Buscar pedidos"
                        />
                    </div>
                    <select
                        className="input sm:w-48"
                        value={estado}
                        onChange={(e) => updateParams({ estado: e.target.value })}
                        aria-label="Filtrar por estado"
                    >
                        <option value="">Todos los estados</option>
                        {ESTADOS.map((e) => (
                            <option key={e} value={e}>
                                {ESTADO_LABEL[e]}
                            </option>
                        ))}
                    </select>
                    {hasFilters && (
                        <button
                            className="btn-ghost text-sm"
                            onClick={() => {
                                setSearchInput('');
                                updateParams({ search: undefined, estado: undefined });
                            }}
                        >
                            Limpiar filtros
                        </button>
                    )}
                </div>

                {error ? (
                    <div className="p-10 text-center">
                        <p className="text-sm text-red-600">{error}</p>
                        <button className="btn-secondary mt-4" onClick={() => setReloadKey((k) => k + 1)}>
                            Reintentar
                        </button>
                    </div>
                ) : (
                    <>
                        {/* Tabla (escritorio) */}
                        <div className="hidden overflow-x-auto md:block">
                            <table className="min-w-full divide-y divide-slate-200 text-sm">
                                <thead className="bg-slate-50">
                                    <tr>
                                        {COLUMNS.map((c) => (
                                            <th key={c.key} scope="col" className={`px-4 py-3 font-medium text-slate-500 ${c.className ?? 'text-left'}`}>
                                                <button className="inline-flex items-center gap-1 hover:text-slate-900" onClick={() => toggleSort(c.key)}>
                                                    {c.label}
                                                    <span className={`text-xs ${sortBy === c.key ? 'text-brand-600' : 'text-slate-300'}`}>
                                                        {sortBy === c.key ? (desc ? '▼' : '▲') : '↕'}
                                                    </span>
                                                </button>
                                            </th>
                                        ))}
                                        <th className="px-4 py-3 text-right font-medium text-slate-500">Acciones</th>
                                    </tr>
                                </thead>
                                <tbody className={`divide-y divide-slate-100 ${loading && data ? 'opacity-50' : ''}`}>
                                    {loading && !data
                                        ? Array.from({ length: 5 }).map((_, i) => (
                                            <tr key={i}>
                                                {Array.from({ length: 6 }).map((__, j) => (
                                                    <td key={j} className="px-4 py-4">
                                                        <div className="h-4 animate-pulse rounded bg-slate-100" />
                                                    </td>
                                                ))}
                                            </tr>
                                        ))
                                        : data?.items.map((p) => (
                                            <tr
                                                key={p.id}
                                                className={`transition hover:bg-slate-50 ${p.id === highlightId ? 'bg-brand-50/60' : ''}`}
                                                onDoubleClick={() => navigate(`/pedidos/${p.id}/editar`)}
                                            >
                                                <td className="whitespace-nowrap px-4 py-3 font-mono font-medium text-slate-900">{p.numeroPedido}</td>
                                                <td className="px-4 py-3">{p.cliente}</td>
                                                <td className="whitespace-nowrap px-4 py-3 text-slate-600">{formatDate(p.fecha)}</td>
                                                <td className="whitespace-nowrap px-4 py-3 text-right font-medium tabular-nums">{formatCurrency(p.total)}</td>
                                                <td className="px-4 py-3">
                                                    <EstadoBadge estado={p.estado} />
                                                </td>
                                                <td className="whitespace-nowrap px-4 py-3 text-right">
                                                    <RowActions pedido={p} isAdmin={isAdmin} onDelete={setToDelete} />
                                                </td>
                                            </tr>
                                        ))}
                                </tbody>
                            </table>
                        </div>

                        {/* Tarjetas (móvil) */}
                        <ul className="divide-y divide-slate-100 md:hidden">
                            {data?.items.map((p) => (
                                <li key={p.id} className={`p-4 ${p.id === highlightId ? 'bg-brand-50/60' : ''}`}>
                                    <div className="flex items-start justify-between gap-3">
                                        <div>
                                            <p className="font-mono text-sm font-semibold text-slate-900">{p.numeroPedido}</p>
                                            <p className="text-sm text-slate-600">{p.cliente}</p>
                                            <p className="mt-1 text-xs text-slate-500">{formatDate(p.fecha)}</p>
                                        </div>
                                        <div className="text-right">
                                            <p className="font-semibold tabular-nums">{formatCurrency(p.total)}</p>
                                            <div className="mt-1">
                                                <EstadoBadge estado={p.estado} />
                                            </div>
                                        </div>
                                    </div>
                                    <div className="mt-3 flex justify-end">
                                        <RowActions pedido={p} isAdmin={isAdmin} onDelete={setToDelete} />
                                    </div>
                                </li>
                            ))}
                        </ul>

                        {!loading && data?.items.length === 0 && (
                            <div className="px-4 py-16 text-center">
                                <p className="text-sm font-medium text-slate-900">{hasFilters ? 'Sin resultados' : 'Aún no hay pedidos'}</p>
                                <p className="mt-1 text-sm text-slate-500">
                                    {hasFilters ? 'Pruebe con otros filtros de búsqueda.' : 'Cree el primer pedido para comenzar.'}
                                </p>
                                {!hasFilters && (
                                    <Link to="/pedidos/nuevo" className="btn-primary mt-4">
                                        Crear pedido
                                    </Link>
                                )}
                            </div>
                        )}

                        {data && data.totalCount > 0 && (
                            <Pagination
                                page={data.page}
                                totalPages={data.totalPages}
                                totalCount={data.totalCount}
                                pageSize={data.pageSize}
                                onPageChange={(p) => updateParams({ page: p }, false)}
                                onPageSizeChange={(s) => updateParams({ pageSize: s })}
                            />
                        )}
                    </>
                )}
            </div>

            <ConfirmDialog
                open={!!toDelete}
                title="Eliminar pedido"
                message={
                    <>
                        ¿Seguro que desea eliminar el pedido <strong>{toDelete?.numeroPedido}</strong> de <strong>{toDelete?.cliente}</strong>?
                        <br />
                        El registro se conserva en la base de datos (eliminación lógica).
                    </>
                }
                confirmText="Eliminar"
                loading={deleting}
                onConfirm={confirmDelete}
                onCancel={() => setToDelete(null)}
            />
        </>
    );
}

function RowActions({ pedido, isAdmin, onDelete }: { pedido: Pedido; isAdmin: boolean; onDelete: (p: Pedido) => void }) {
    return (
        <div className="inline-flex gap-1">
            <Link to={`/pedidos/${pedido.id}/editar`} className="btn-ghost text-brand-600" title="Editar">
                Editar
            </Link>
            <button
                className="btn-ghost text-red-600 hover:bg-red-50 disabled:text-slate-400 disabled:hover:bg-transparent"
                onClick={() => onDelete(pedido)}
                disabled={!isAdmin}
                title={isAdmin ? 'Eliminar' : 'Solo un administrador puede eliminar pedidos'}
            >
                Eliminar
            </button>
        </div>
    );
}