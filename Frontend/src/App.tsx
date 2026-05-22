import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/authStore'
import LoginPage from './pages/customer/LoginPage'
import OrdersPage from './pages/customer/OrdersPage'
import NewReturnPage from './pages/customer/NewReturnPage'
import ReturnDetailPage from './pages/customer/ReturnDetailPage'
import ReturnsListPage from './pages/customer/ReturnsListPage'
import AdminLoginPage from './pages/admin/AdminLoginPage'
import DashboardPage from './pages/admin/DashboardPage'
import ReturnListPage from './pages/admin/ReturnListPage'
import AdminReturnDetailPage from './pages/admin/AdminReturnDetailPage'
import QualityAssessmentPage from './pages/admin/QualityAssessmentPage'

function RequireAuth({ children, role }: { children: React.ReactNode; role: 'customer' | 'admin' }) {
  const auth = useAuthStore()
  if (!auth.accessToken || auth.role !== role) {
    return <Navigate to={role === 'admin' ? '/admin/login' : '/login'} replace />
  }
  return <>{children}</>
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Customer routes */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/orders" element={<RequireAuth role="customer"><OrdersPage /></RequireAuth>} />
        <Route path="/returns" element={<RequireAuth role="customer"><ReturnsListPage /></RequireAuth>} />
        <Route path="/returns/new" element={<RequireAuth role="customer"><NewReturnPage /></RequireAuth>} />
        <Route path="/returns/:returnId" element={<RequireAuth role="customer"><ReturnDetailPage /></RequireAuth>} />

        {/* Admin routes */}
        <Route path="/admin/login" element={<AdminLoginPage />} />
        <Route path="/admin" element={<RequireAuth role="admin"><DashboardPage /></RequireAuth>} />
        <Route path="/admin/returns" element={<RequireAuth role="admin"><ReturnListPage /></RequireAuth>} />
        <Route path="/admin/returns/:returnId" element={<RequireAuth role="admin"><AdminReturnDetailPage /></RequireAuth>} />
        <Route path="/admin/returns/:returnId/quality" element={<RequireAuth role="admin"><QualityAssessmentPage /></RequireAuth>} />

        {/* Fallback */}
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
