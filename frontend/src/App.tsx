import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from '@/components/Layout';
import ProtectedRoute from '@/components/ProtectedRoute';
import LoginPage from '@/pages/LoginPage';
import NotFoundPage from '@/pages/NotFoundPage';
import PedidoCreatePage from '@/pages/PedidoCreatePage';
import PedidoEditPage from '@/pages/PedidoEditPage';
import PedidosListPage from '@/pages/PedidosListPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      {/* Todo lo de aquí dentro exige sesión y comparte el menú (Layout). */}
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route index element={<Navigate to="/pedidos" replace />} />
          <Route path="/pedidos" element={<PedidosListPage />} />
          <Route path="/pedidos/nuevo" element={<PedidoCreatePage />} />
          <Route path="/pedidos/:id/editar" element={<PedidoEditPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}