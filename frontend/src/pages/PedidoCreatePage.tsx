import { useNavigate } from 'react-router-dom';
import { pedidosApi } from '@/api/pedidos';
import PageHeader from '@/components/PageHeader';
import PedidoForm from '@/components/PedidoForm';
import { useToast } from '@/context/useToast';

export default function PedidoCreatePage() {
    const navigate = useNavigate();
    const toast = useToast();

    return (
        <div className="mx-auto max-w-3xl">
            <PageHeader title="Nuevo pedido" subtitle="Registre un pedido. Los campos con * son obligatorios." />
            <PedidoForm
                submitLabel="Crear pedido"
                onSubmit={async (input) => {
                    const pedido = await pedidosApi.create(input);
                    toast.success(`Pedido ${pedido.numeroPedido} creado correctamente.`);
                    navigate('/pedidos', { state: { highlight: pedido.id } });
                }}
            />
        </div>
    );
}