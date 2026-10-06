import { useEffect, useRef, type ReactNode } from 'react';

interface Props {
    open: boolean;
    title: string;
    message: ReactNode;
    confirmText?: string;
    loading?: boolean;
    onConfirm: () => void;
    onCancel: () => void;
}

export default function ConfirmDialog({ open, title, message, confirmText = 'Confirmar', loading, onConfirm, onCancel }: Props) {
    const cancelRef = useRef<HTMLButtonElement>(null);

    // Al abrir: foco en "Cancelar" (opción segura) y cierre con la tecla Esc.
    useEffect(() => {
        if (!open) return;
        cancelRef.current?.focus();
        const onKey = (e: KeyboardEvent) => {
            if (e.key === 'Escape' && !loading) onCancel();
        };
        window.addEventListener('keydown', onKey);
        return () => window.removeEventListener('keydown', onKey);
    }, [open, loading, onCancel]);

    if (!open) return null;

    return (
        <div className="fixed inset-0 z-40 flex items-center justify-center p-4" role="dialog" aria-modal="true" aria-labelledby="confirm-title">
            <div className="absolute inset-0 bg-slate-900/40" onClick={() => !loading && onCancel()} />
            <div className="card relative w-full max-w-md p-6">
                <div className="flex gap-4">
                    <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-red-100 text-red-600">
                        <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth={2}>
                            <path d="M12 9v4m0 4h.01M10.3 3.9L1.8 18a2 2 0 001.7 3h17a2 2 0 001.7-3L13.7 3.9a2 2 0 00-3.4 0z" strokeLinecap="round" />
                        </svg>
                    </span>
                    <div>
                        <h2 id="confirm-title" className="text-base font-semibold text-slate-900">
                            {title}
                        </h2>
                        <div className="mt-2 text-sm text-slate-600">{message}</div>
                    </div>
                </div>
                <div className="mt-6 flex justify-end gap-3">
                    <button ref={cancelRef} className="btn-secondary" onClick={onCancel} disabled={loading}>
                        Cancelar
                    </button>
                    <button className="btn-danger" onClick={onConfirm} disabled={loading}>
                        {loading ? 'Eliminando…' : confirmText}
                    </button>
                </div>
            </div>
        </div>
    );
}