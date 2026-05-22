import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../../lib/api'
import { useAuthStore } from '../../store/authStore'

interface Stats {
  pendingLabel: number
  inTransit: number
  delivered: number
  pendingVoucher: number
  pendingRefund: number
  closedTotal: number
}

const STAT_CARDS = [
  { key: 'pendingLabel' as const, label: 'Aguardando etiqueta', color: 'bg-yellow-50 border-yellow-200 text-yellow-700' },
  { key: 'inTransit' as const, label: 'Em trânsito', color: 'bg-blue-50 border-blue-200 text-blue-700' },
  { key: 'delivered' as const, label: 'Aguardando avaliação', color: 'bg-orange-50 border-orange-200 text-orange-700' },
  { key: 'pendingVoucher' as const, label: 'Gerando voucher', color: 'bg-purple-50 border-purple-200 text-purple-700' },
  { key: 'pendingRefund' as const, label: 'Processando reembolso', color: 'bg-red-50 border-red-200 text-red-700' },
  { key: 'closedTotal' as const, label: 'Concluídas', color: 'bg-green-50 border-green-200 text-green-700' },
]

export default function DashboardPage() {
  const name = useAuthStore((s) => s.name)
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()

  const { data: stats } = useQuery<Stats>({
    queryKey: ['admin-stats'],
    queryFn: () => api.get('/admin/returns/stats').then((r) => r.data),
    refetchInterval: 60_000,
  })

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between">
        <h1 className="text-lg font-semibold text-gray-800">La Moda — Gestão de Devoluções</h1>
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/admin/returns')}
            className="text-sm font-medium text-gray-600 hover:text-gray-900"
          >
            Ver todas as devoluções
          </button>
          <span className="text-sm text-gray-500">{name}</span>
          <button onClick={() => { logout(); navigate('/admin/login') }} className="text-sm text-gray-400 hover:text-gray-600">
            Sair
          </button>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-8">
        <h2 className="text-xl font-semibold text-gray-800 mb-6">Dashboard</h2>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
          {STAT_CARDS.map(({ key, label, color }) => (
            <div key={key} className={`rounded-xl border p-5 ${color}`}>
              <p className="text-3xl font-bold">{stats?.[key] ?? '—'}</p>
              <p className="text-sm mt-1 font-medium">{label}</p>
            </div>
          ))}
        </div>
      </main>
    </div>
  )
}
