import { Link } from 'react-router-dom';

export default function NotFoundPage() {
    return (
        <div className="flex min-h-[60vh] flex-col items-center justify-center text-center">
            <p className="text-6xl font-bold text-slate-200">404</p>
            <h1 className="mt-2 text-xl font-semibold text-slate-800">Página no encontrada</h1>
            <p className="mt-1 text-sm text-slate-500">La ruta que busca no existe.</p>
            <Link to="/pedidos" className="btn-primary mt-6">
                Ir a pedidos
            </Link>
        </div>
    );
}