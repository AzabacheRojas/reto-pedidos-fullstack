interface Props {
    page: number;
    totalPages: number;
    totalCount: number;
    pageSize: number;
    onPageChange: (page: number) => void;
    onPageSizeChange: (size: number) => void;
}

export default function Pagination({ page, totalPages, totalCount, pageSize, onPageChange, onPageSizeChange }: Props) {
    const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
    const to = Math.min(page * pageSize, totalCount);

    return (
        <div className="flex flex-col items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 text-sm text-slate-600 sm:flex-row">
            <p>
                Mostrando <span className="font-medium text-slate-900">{from}</span>–<span className="font-medium text-slate-900">{to}</span> de{' '}
                <span className="font-medium text-slate-900">{totalCount}</span> pedidos
            </p>
            <div className="flex items-center gap-3">
                <label className="flex items-center gap-2">
                    <span className="hidden sm:inline">Por página</span>
                    <select className="input w-auto py-1" value={pageSize} onChange={(e) => onPageSizeChange(Number(e.target.value))}>
                        {[5, 10, 20, 50].map((n) => (
                            <option key={n} value={n}>
                                {n}
                            </option>
                        ))}
                    </select>
                </label>
                <div className="flex items-center gap-1">
                    <button className="btn-secondary px-3 py-1" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
                        ‹ Anterior
                    </button>
                    <span className="px-2">
                        {page} / {Math.max(totalPages, 1)}
                    </span>
                    <button className="btn-secondary px-3 py-1" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
                        Siguiente ›
                    </button>
                </div>
            </div>
        </div>
    );
}