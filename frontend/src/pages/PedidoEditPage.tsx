import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { toApiError } from '@/api/errors';
import { pedidosApi } from '@/api/pedidos';
import PageHeader from '@/components/PageHeader';
import PedidoForm from '@/components/PedidoForm';
import Spinner from '@/components/Spinner';
import { useToast } from '@/context/useToast';
import type { Pedido } from '@/types';
import { formatDate, toDateInput } from '@/utils/format';

interface LoadError {
    status?: number;
    message: string;
}

interface LoadState {
    id: number;
    pedido?: Pedido;
    error?: LoadError;
}

export default function PedidoEditPage() {
    const { id } = useParams();
    const pedidoId = Number(id);
    const idValido = Number.isInteger(pedidoId) && pedidoId > 0;
    const navigate = useNavigate();
    const toast = useToast();

    // Guardamos el resultado junto con el id al que pertenece: así "cargando" se
    // deriva (no hay setState síncrono dentro del efecto).
    const [state, setState] = useState<LoadState>({ id: 0 });

    useEffect(() => {
        if (!idValido) return;
        let active = true;
        pedidosApi
            .get(pedidoId)
            .then((pedido) => {
                if (active) setState({ id: pedidoId, pedido });
            })
            .catch((err) => {
                if (active) setState({ id: pedidoId, error: toApiError(err) });
            });
        return () => {
            active = false;
        };
    }, [pedidoId, idValido]);

    const actual = state.id === pedidoId ? state : null;
    const error: LoadError | undefined = !idValido
        ? { status: 404, message: 'Identificador de pedido inválido.' }
        : actual?.error;
    const pedido = actual?.pedido;

    if (error) {
        return (
            <div className="card mx-auto max-w-lg p-8 text-center">
                <p className="text-4xl font-bold text-slate-300">{error.status ?? '!'}</p>
                <p className="mt-2 text-slate-600">{error.message}</p>
                <Link to="/pedidos" className="btn-primary mt-6">
                    Volver al listado
                </Link>
            </div>
        );
    }

    if (!pedido) {
        return (
            <div className="flex justify-center py-20 text-brand-600">
                <Spinner className="h-8 w-8" />
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-3xl">
            <PageHeader
                title={`Editar pedido ${pedido.numeroPedido}`}
                subtitle={`Registrado el ${formatDate(pedido.createdAt)}${pedido.updatedAt ? ` · última modificación ${formatDate(pedido.updatedAt)}` : ''
                    }`}
            />
            <PedidoForm
                submitLabel="Guardar cambios"
                initial={{
                    numeroPedido: pedido.numeroPedido,
                    cliente: pedido.cliente,
                    fecha: toDateInput(pedido.fecha),
                    total: pedido.total,
                    estado: pedido.estado,
                }}
                onSubmit={async (input) => {
                    const updated = await pedidosApi.update(pedido.id, input);
                    toast.success(`Pedido ${updated.numeroPedido} actualizado.`);
                    navigate('/pedidos', { state: { highlight: updated.id } });
                }}
            />
        </div>
    );
}