import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { api } from '../../lib/api'
import logo from '../../images/logo.png'

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Rascunho',
  PendingLabel: 'Aguardando etiqueta',
  LabelGenerated: 'Etiqueta gerada',
  InTransit: 'Em trânsito',
  Delivered: 'Entregue',
  QualityApproved: 'Qualidade aprovada',
  QualityRejected: 'Qualidade reprovada',
  PendingVoucher: 'Gerando voucher',
  PendingRefund: 'Processando reembolso',
  ClosedExchange: 'Crédito gerado ✓',
  ClosedRefund: 'Reembolso processado ✓',
  ClosedRejected: 'Devolução recusada',
  Cancelled: 'Cancelado',
  Error: 'Erro',
}

const STATUS_COLORS: Record<string, string> = {
  PendingLabel: 'bg-yellow-100 text-yellow-700',
  LabelGenerated: 'bg-blue-100 text-blue-700',
  InTransit: 'bg-purple-100 text-purple-700',
  Delivered: 'bg-orange-100 text-orange-700',
  QualityApproved: 'bg-teal-100 text-teal-700',
  QualityRejected: 'bg-red-100 text-red-700',
  PendingVoucher: 'bg-blue-100 text-blue-700',
  PendingRefund: 'bg-blue-100 text-blue-700',
  ClosedExchange: 'bg-green-100 text-green-700',
  ClosedRefund: 'bg-green-100 text-green-700',
  ClosedRejected: 'bg-red-100 text-red-700',
  Cancelled: 'bg-gray-100 text-gray-500',
  Error: 'bg-red-100 text-red-700',
}

interface ReturnSummary {
  id: string
  requestNumber: string
  status: string
  resolutionType: string
  createdAt: string
}

export default function ReturnsListPage() {
  const navigate = useNavigate()

  const { data: returns, isLoading } = useQuery<ReturnSummary[]>({
    queryKey: ['my-returns'],
    queryFn: () => api.get('/customer/returns').then((r) => r.data),
    refetchInterval: 8_000,
  })

  return (
    <div className="min-h-screen bg-candy-bg">
      <header className="bg-white border-b border-candy-border px-4 py-3 flex items-center gap-3">
        <img src={logo} alt="DevolveFacil" className="h-7 w-auto" />
        <div className="flex-1">
          <h1 className="text-base font-semibold text-candy-on-surface leading-tight">Minhas Devoluções</h1>
        </div>
        <button
          onClick={() => navigate('/orders')}
          className="text-candy-primary text-sm font-medium"
        >
          Pedidos
        </button>
      </header>

      <main className="max-w-lg mx-auto px-4 py-6 space-y-3">
        {isLoading && (
          <div className="text-center text-candy-subtle py-12">Carregando...</div>
        )}

        {!isLoading && (!returns || returns.length === 0) && (
          <div className="text-center py-16">
            <p className="text-5xl mb-4">📦</p>
            <p className="text-candy-on-surface font-medium mb-1">Nenhuma devolução</p>
            <p className="text-candy-subtle text-sm mb-6">Quando você solicitar uma devolução, ela aparecerá aqui.</p>
            <button
              onClick={() => navigate('/orders')}
              className="bg-candy-primary text-white font-semibold px-6 py-2.5 rounded-full text-sm hover:opacity-90 transition-opacity"
            >
              Ver meus pedidos
            </button>
          </div>
        )}

        {returns?.map((r) => (
          <button
            key={r.id}
            onClick={() => navigate(`/returns/${r.id}`)}
            className="w-full bg-white rounded-2xl border border-candy-border p-4 text-left hover:shadow-md transition-shadow"
          >
            <div className="flex items-center justify-between mb-2">
              <span className="font-semibold text-candy-on-surface text-sm">{r.requestNumber}</span>
              <span className={`text-xs font-medium px-2.5 py-0.5 rounded-full ${STATUS_COLORS[r.status] ?? 'bg-gray-100 text-gray-500'}`}>
                {STATUS_LABELS[r.status] ?? r.status}
              </span>
            </div>
            <p className="text-xs text-candy-subtle">
              {r.resolutionType === 'StoreCredit' ? '🎁 Crédito na loja' : '💰 Reembolso'}
              {' · '}
              {new Date(r.createdAt).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' })}
            </p>
          </button>
        ))}
      </main>
    </div>
  )
}
