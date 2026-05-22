import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../lib/api'

interface ReturnDetail {
  id: string
  requestNumber: string
  status: string
  resolutionType: string
  reasonCode: string
  reasonNotes?: string
  voucherCode?: string
  erpReturnOrderId?: string
  createdAt: string
  customer: { name: string; email: string; cpf: string }
  order: { externalOrderId: string; total: number; currency: string }
  shipment?: { trackingCode: string; carrierCode: string; status: string; deliveredAt?: string }
  items: { sku: string; name: string; quantity: number; unitPrice: number }[]
  events: { eventType: string; actor: string; occurredAt: string }[]
}

const STATUS_PT: Record<string, string> = {
  PendingLabel: 'Aguardando etiqueta',
  LabelGenerated: 'Etiqueta gerada',
  InTransit: 'Em trânsito',
  Delivered: 'Entregue',
  QualityApproved: 'Qualidade aprovada',
  QualityRejected: 'Qualidade reprovada',
  PendingVoucher: 'Gerando voucher',
  PendingRefund: 'Processando reembolso',
  ClosedExchange: 'Crédito gerado',
  ClosedRefund: 'Reembolso processado',
  ClosedRejected: 'Devolução recusada',
  Cancelled: 'Cancelado',
}

export default function AdminReturnDetailPage() {
  const { returnId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data, isLoading } = useQuery<ReturnDetail>({
    queryKey: ['admin-return', returnId],
    queryFn: () => api.get(`/admin/returns/${returnId}`).then((r) => r.data),
    refetchInterval: 5_000,
  })

  const simulateLabel = useMutation({
    mutationFn: () => api.post(`/admin/returns/${returnId}/simulate-label`).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-return', returnId] }),
  })

  const simulateDelivery = useMutation({
    mutationFn: () => api.post(`/admin/returns/${returnId}/simulate-delivery`).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-return', returnId] }),
  })

  if (isLoading) return <div className="p-8 text-gray-500">Carregando...</div>
  if (!data) return <div className="p-8 text-red-500">Não encontrado.</div>

  const statusLabel = STATUS_PT[data.status] ?? data.status

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between">
        <button onClick={() => navigate('/admin/returns')} className="text-gray-600 text-sm font-medium">
          ← Devoluções
        </button>
        <div className="text-center">
          <h1 className="text-lg font-semibold text-gray-800">{data.requestNumber}</h1>
          <span className="text-xs text-gray-500">{statusLabel}</span>
        </div>
        <div className="flex gap-2">
          {data.status === 'PendingLabel' && (
            <button
              onClick={() => simulateLabel.mutate()}
              disabled={simulateLabel.isPending}
              className="bg-blue-600 text-white text-sm font-semibold px-4 py-1.5 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
              {simulateLabel.isPending ? 'Gerando...' : 'Simular etiqueta'}
            </button>
          )}
          {data.status === 'InTransit' && (
            <button
              onClick={() => simulateDelivery.mutate()}
              disabled={simulateDelivery.isPending}
              className="bg-purple-600 text-white text-sm font-semibold px-4 py-1.5 rounded-lg hover:bg-purple-700 disabled:opacity-50 transition-colors"
            >
              {simulateDelivery.isPending ? 'Simulando...' : 'Simular entrega'}
            </button>
          )}
          {data.status === 'Delivered' && (
            <button
              onClick={() => navigate(`/admin/returns/${returnId}/quality`)}
              className="bg-orange-500 text-white text-sm font-semibold px-4 py-1.5 rounded-lg hover:bg-orange-600 transition-colors"
            >
              Avaliar qualidade
            </button>
          )}
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-6 py-8 grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <h2 className="font-medium text-gray-700 mb-3">Cliente</h2>
            <p className="text-sm text-gray-800 font-medium">{data.customer.name}</p>
            <p className="text-sm text-gray-500">{data.customer.email}</p>
            <p className="text-sm text-gray-400">CPF: {data.customer.cpf}</p>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <h2 className="font-medium text-gray-700 mb-3">Pedido</h2>
            <p className="text-sm text-gray-800">#{data.order.externalOrderId}</p>
            <p className="text-sm text-gray-600">
              {data.order.total.toLocaleString('pt-BR', { style: 'currency', currency: data.order.currency })}
            </p>
            <div className="mt-2 space-y-1">
              {data.items.map((item) => (
                <p key={item.sku} className="text-xs text-gray-500">{item.name} × {item.quantity}</p>
              ))}
            </div>
          </div>

          {data.shipment && (
            <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
              <h2 className="font-medium text-gray-700 mb-2">Rastreamento</h2>
              <p className="text-sm font-mono text-gray-800">{data.shipment.trackingCode}</p>
              <p className="text-sm text-gray-500">{data.shipment.carrierCode.toUpperCase()} · {data.shipment.status}</p>
              {data.shipment.deliveredAt && (
                <p className="text-xs text-green-600 mt-1">
                  Entregue em {new Date(data.shipment.deliveredAt).toLocaleDateString('pt-BR')}
                </p>
              )}
            </div>
          )}

          {data.voucherCode && (
            <div className="bg-green-50 border border-green-200 rounded-xl p-5">
              <p className="font-medium text-green-800">Voucher gerado</p>
              <p className="font-mono text-lg text-green-700 mt-1">{data.voucherCode}</p>
            </div>
          )}

          {simulateLabel.isSuccess && simulateLabel.data && (
            <div className="bg-blue-50 border border-blue-200 rounded-xl p-4">
              <p className="text-sm text-blue-700 font-medium">
                Etiqueta gerada — código:{' '}
                <span className="font-mono">
                  {(simulateLabel.data as { trackingCode?: string }).trackingCode}
                </span>
              </p>
            </div>
          )}
          {simulateDelivery.isSuccess && (
            <div className="bg-blue-50 border border-blue-200 rounded-xl p-4">
              <p className="text-sm text-blue-700 font-medium">
                Entrega simulada — status atualizado para Entregue.
              </p>
            </div>
          )}
        </div>

        <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm h-fit">
          <h2 className="font-medium text-gray-700 mb-3">Histórico de eventos</h2>
          <ol className="space-y-3">
            {data.events.map((evt, i) => (
              <li key={i} className="flex gap-3 text-sm">
                <div className="w-2 h-2 rounded-full bg-gray-300 mt-1.5 flex-shrink-0" />
                <div>
                  <p className="text-gray-700 font-medium">{evt.eventType}</p>
                  <p className="text-xs text-gray-400">{evt.actor} · {new Date(evt.occurredAt).toLocaleString('pt-BR')}</p>
                </div>
              </li>
            ))}
          </ol>
        </div>
      </main>
    </div>
  )
}
