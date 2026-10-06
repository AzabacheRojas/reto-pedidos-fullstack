import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { toApiError } from '@/api/errors';
import { ESTADOS, ESTADO_LABEL, type EstadoPedido, type PedidoInput } from '@/types';
import { todayInput } from '@/utils/format';
import Spinner from './Spinner';

interface Props {
    initial?: PedidoInput;
    submitLabel: string;
    onSubmit: (input: PedidoInput) => Promise<void>;
}

interface FormState {
    numeroPedido: string;
    cliente: string;
    fecha: string;
    total: string;
    estado: EstadoPedido;
}

type Errors = Partial<Record<keyof FormState, string>>;

const NUMERO_REGEX = /^[A-Za-z0-9-]+$/;

/** Validación en cliente: mismas reglas que el backend, para dar feedback inmediato. */
function validate(f: FormState): Errors {
    const e: Errors = {};
    const numero = f.numeroPedido.trim();
    if (!numero) e.numeroPedido = 'El número de pedido es obligatorio.';
    else if (numero.length > 50) e.numeroPedido = 'Máximo 50 caracteres.';
    else if (!NUMERO_REGEX.test(numero)) e.numeroPedido = 'Solo letras, números y guiones (ej: PED-001).';

    const cliente = f.cliente.trim();
    if (!cliente) e.cliente = 'El cliente es obligatorio.';
    else if (cliente.length > 150) e.cliente = 'Máximo 150 caracteres.';

    if (!f.fecha) e.fecha = 'La fecha es obligatoria.';

    const total = Number(f.total);
    if (f.total.trim() === '' || Number.isNaN(total)) e.total = 'Ingrese un monto válido.';
    else if (total <= 0) e.total = 'El total debe ser mayor a 0.';
    else if (!/^\d{1,8}(\.\d{1,2})?$/.test(f.total.trim())) e.total = 'Máximo 8 enteros y 2 decimales.';

    return e;
}

export default function PedidoForm({ initial, submitLabel, onSubmit }: Props) {
    const [form, setForm] = useState<FormState>({
        numeroPedido: initial?.numeroPedido ?? '',
        cliente: initial?.cliente ?? '',
        fecha: initial?.fecha ?? todayInput(),
        total: initial ? String(initial.total) : '',
        estado: initial?.estado ?? 'Registrado',
    });
    const [errors, setErrors] = useState<Errors>({});
    const [touched, setTouched] = useState<Partial<Record<keyof FormState, boolean>>>({});
    const [serverError, setServerError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
        const next = { ...form, [key]: value };
        setForm(next);
        if (touched[key]) setErrors((prev) => ({ ...prev, [key]: validate(next)[key] }));
    };

    const blur = (key: keyof FormState) => {
        setTouched((t) => ({ ...t, [key]: true }));
        setErrors((prev) => ({ ...prev, [key]: validate(form)[key] }));
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        const v = validate(form);
        setErrors(v);
        setTouched({ numeroPedido: true, cliente: true, fecha: true, total: true, estado: true });
        if (Object.keys(v).length > 0) return;

        setSubmitting(true);
        setServerError(null);
        try {
            await onSubmit({
                numeroPedido: form.numeroPedido.trim().toUpperCase(),
                cliente: form.cliente.trim(),
                fecha: form.fecha,
                total: Number(form.total),
                estado: form.estado,
            });
        } catch (err) {
            // Errores del backend: por campo (400) o número duplicado (409).
            const apiError = toApiError(err);
            setErrors((prev) => ({ ...prev, ...(apiError.fieldErrors as Errors) }));
            if (apiError.status === 409) setErrors((prev) => ({ ...prev, numeroPedido: apiError.message }));
            setServerError(apiError.message);
        } finally {
            setSubmitting(false);
        }
    };

    const fieldClass = (key: keyof FormState) => `input ${errors[key] ? 'input-error' : ''}`;

    return (
        <form onSubmit={handleSubmit} noValidate className="card p-6">
            {serverError && (
                <div className="mb-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" role="alert">
                    {serverError}
                </div>
            )}

            <div className="grid gap-5 sm:grid-cols-2">
                <div>
                    <label htmlFor="numeroPedido" className="label">
                        Número de pedido *
                    </label>
                    <input
                        id="numeroPedido"
                        className={`${fieldClass('numeroPedido')} uppercase`}
                        placeholder="PED-001"
                        value={form.numeroPedido}
                        maxLength={50}
                        autoFocus={!initial}
                        onChange={(e) => set('numeroPedido', e.target.value)}
                        onBlur={() => blur('numeroPedido')}
                    />
                    {errors.numeroPedido && <p className="mt-1 text-xs text-red-600">{errors.numeroPedido}</p>}
                </div>

                <div>
                    <label htmlFor="cliente" className="label">
                        Cliente *
                    </label>
                    <input
                        id="cliente"
                        className={fieldClass('cliente')}
                        placeholder="Juan Pérez"
                        value={form.cliente}
                        maxLength={150}
                        onChange={(e) => set('cliente', e.target.value)}
                        onBlur={() => blur('cliente')}
                    />
                    {errors.cliente && <p className="mt-1 text-xs text-red-600">{errors.cliente}</p>}
                </div>

                <div>
                    <label htmlFor="fecha" className="label">
                        Fecha *
                    </label>
                    <input
                        id="fecha"
                        type="date"
                        className={fieldClass('fecha')}
                        value={form.fecha}
                        onChange={(e) => set('fecha', e.target.value)}
                        onBlur={() => blur('fecha')}
                    />
                    {errors.fecha && <p className="mt-1 text-xs text-red-600">{errors.fecha}</p>}
                </div>

                <div>
                    <label htmlFor="total" className="label">
                        Total *
                    </label>
                    <div className="relative">
                        <span className="pointer-events-none absolute inset-y-0 left-3 flex items-center text-sm text-slate-400">$</span>
                        <input
                            id="total"
                            type="number"
                            inputMode="decimal"
                            step="0.01"
                            min="0.01"
                            className={`${fieldClass('total')} pl-7`}
                            placeholder="0.00"
                            value={form.total}
                            onChange={(e) => set('total', e.target.value)}
                            onBlur={() => blur('total')}
                        />
                    </div>
                    {errors.total && <p className="mt-1 text-xs text-red-600">{errors.total}</p>}
                </div>

                <div className="sm:col-span-2">
                    <span className="label">Estado</span>
                    <div className="flex flex-wrap gap-2">
                        {ESTADOS.map((estado) => (
                            <button
                                type="button"
                                key={estado}
                                onClick={() => set('estado', estado)}
                                className={`rounded-full border px-3 py-1.5 text-sm transition ${form.estado === estado
                                        ? 'border-brand-600 bg-brand-600 text-white'
                                        : 'border-slate-300 bg-white text-slate-600 hover:border-slate-400'
                                    }`}
                                aria-pressed={form.estado === estado}
                            >
                                {ESTADO_LABEL[estado]}
                            </button>
                        ))}
                    </div>
                    {errors.estado && <p className="mt-1 text-xs text-red-600">{errors.estado}</p>}
                </div>
            </div>

            <div className="mt-8 flex justify-end gap-3 border-t border-slate-100 pt-5">
                <Link to="/pedidos" className="btn-secondary">
                    Cancelar
                </Link>
                <button type="submit" className="btn-primary" disabled={submitting}>
                    {submitting && <Spinner className="h-4 w-4" />}
                    {submitLabel}
                </button>
            </div>
        </form>
    );
}