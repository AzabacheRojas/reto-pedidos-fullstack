import { ESTADO_LABEL, type EstadoPedido } from '@/types';

const STYLES: Record<EstadoPedido, string> = {
    Registrado: 'bg-slate-100 text-slate-700 ring-slate-200',
    EnProceso: 'bg-amber-50 text-amber-700 ring-amber-200',
    Despachado: 'bg-sky-50 text-sky-700 ring-sky-200',
    Entregado: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
    Cancelado: 'bg-red-50 text-red-700 ring-red-200',
};

export default function EstadoBadge({ estado }: { estado: EstadoPedido }) {
    return (
        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${STYLES[estado]}`}>
            {ESTADO_LABEL[estado]}
        </span>
    );
}