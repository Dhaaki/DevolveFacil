import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../lib/api'

const STATUS_LABELS: Record<string, { label: string; step: number }> = {
  Draft: { label: 'Rascunho', step: 0 },
  PendingLabel: { label: 'Aguardando etiqueta', step: 1 },
  LabelGenerated: { label: 'Etiqueta gerada', step: 2 },
  InTransit: { label: 'Em trânsito', step: 3 },
  Delivered: { label: 'Entregue — aguardando avaliação', step: 4 },
  QualityApproved: { label: 'Qualidade aprovada', step: 5 },
  QualityRejected: { label: 'Qualidade reprovada', step: 5 },
  PendingVoucher: { label: 'Gerando voucher', step: 5 },
  PendingRefund: { label: 'Processando reembolso', step: 5 },
  ClosedExchange: { label: 'Crédito gerado ✓', step: 6 },
  ClosedRefund: { label: 'Reembolso processado ✓', step: 6 },
  ClosedRejected: { label: 'Devolução recusada', step: 6 },
  Cancelled: { label: 'Cancelado', step: 0 },
  Error: { label: 'Erro — contate o suporte', step: 0 },
}

interface ReturnDetail {
  id: string
  requestNumber: string
  status: string
  resolutionType: string
  reasonCode: string
  voucherCode?: string
  createdAt: string
  shipment?: { trackingCode: string; carrierCode: string; status: string }
  events: { eventType: string; actor: string; occurredAt: string }[]
}

const STEPS = ['Solicitado', 'Etiqueta', 'Postado', 'Em trânsito', 'Entregue', 'Avaliação', 'Concluído']

export default function ReturnDetailPage() {
  const { returnId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [posted, setPosted] = useState(false)

  const { data, isLoading, error } = useQuery<ReturnDetail>({
    queryKey: ['return', returnId],
    queryFn: () => api.get(`/customer/returns/${returnId}`).then((r) => r.data),
    refetchInterval: 5_000,
  })

  const confirmPosted = useMutation({
    mutationFn: () => api.post(`/customer/returns/${returnId}/confirm-posted`).then((r) => r.data),
    onSuccess: () => {
      setPosted(true)
      queryClient.invalidateQueries({ queryKey: ['return', returnId] })
    },
  })

  if (isLoading) return <div className="p-8 text-candy-subtle">Carregando...</div>
  if (error || !data) return <div className="p-8 text-red-500">Erro ao carregar solicitação.</div>

  const statusInfo = STATUS_LABELS[data.status] ?? { label: data.status, step: 0 }

  return (
    <div className="min-h-screen bg-candy-bg">
      <header className="bg-white border-b border-candy-border px-4 py-3">
        <div className="flex items-center justify-between">
          <button onClick={() => navigate('/orders')} className="text-candy-primary text-sm font-medium">
            ← Meus Pedidos
          </button>
          <button onClick={() => navigate('/returns')} className="text-candy-subtle text-sm font-medium">
            Minhas Devoluções
          </button>
        </div>
        <h1 className="text-lg font-semibold text-candy-on-surface mt-1">Devolução {data.requestNumber}</h1>
      </header>

      <main className="max-w-lg mx-auto px-4 py-6 space-y-4">
        {/* Status timeline */}
        <div className="bg-white rounded-2xl border border-candy-border p-5 shadow-sm">
          <p className="font-medium text-candy-on-surface mb-1">
            Status:{' '}
            <span className="text-candy-primary">{statusInfo.label}</span>
          </p>
          {data.status === 'PendingLabel' && (
            <p className="text-xs text-candy-subtle mb-3">Estamos gerando sua etiqueta, aguarde alguns instantes...</p>
          )}
          <div className="flex items-center gap-1 mt-3">
            {STEPS.map((step, i) => (
              <div key={step} className="flex items-center flex-1 last:flex-none">
                <div className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold flex-shrink-0 ${
                  i <= statusInfo.step ? 'bg-candy-primary text-white' : 'bg-gray-100 text-gray-400'
                }`}>
                  {i + 1}
                </div>
                {i < STEPS.length - 1 && (
                  <div className={`h-0.5 flex-1 mx-1 ${i < statusInfo.step ? 'bg-candy-primary' : 'bg-gray-100'}`} />
                )}
              </div>
            ))}
          </div>
          <div className="flex justify-between mt-1">
            {STEPS.map((step) => (
              <span key={step} className="text-xs text-candy-subtle text-center" style={{ width: `${100 / STEPS.length}%` }}>
                {step}
              </span>
            ))}
          </div>
        </div>

        {/* Tracking code + confirm posted */}
        {data.shipment && (
          <div className="bg-white rounded-2xl border border-candy-border p-5 shadow-sm space-y-3">
            <h2 className="font-medium text-candy-on-surface">Rastreamento</h2>
            <div className="flex items-center gap-2">
              <span className="font-mono text-sm bg-gray-50 border border-gray-200 px-3 py-1.5 rounded-lg flex-1">
                {data.shipment.trackingCode}
              </span>
              <span className="text-xs text-candy-subtle">{data.shipment.carrierCode.toUpperCase()}</span>
            </div>
            {data.status === 'LabelGenerated' && (
              <div className="pt-1">
                {posted ? (
                  <p className="text-sm text-green-600 font-medium text-center">✓ Postagem confirmada! Aguardando rastreamento.</p>
                ) : (
                  <>
                    <p className="text-xs text-candy-subtle mb-2">
                      Após postar o pacote nos Correios com a etiqueta acima, confirme abaixo:
                    </p>
                    <button
                      onClick={() => confirmPosted.mutate()}
                      disabled={confirmPosted.isPending}
                      className="w-full bg-candy-primary text-white font-semibold py-2.5 rounded-xl hover:opacity-90 disabled:opacity-50 transition-opacity text-sm"
                    >
                      {confirmPosted.isPending ? 'Confirmando...' : 'Confirmei que postei o pacote'}
                    </button>
                  </>
                )}
              </div>
            )}
          </div>
        )}

        {/* Label download */}
        {['LabelGenerated', 'InTransit', 'Delivered'].includes(data.status) && (
          <a
            href={`/api/customer/returns/${data.id}/label`}
            target="_blank"
            rel="noreferrer"
            className="block w-full text-center text-sm font-medium text-candy-primary border border-candy-border rounded-xl py-2.5 bg-white hover:bg-candy-bg transition-colors"
          >
            Baixar etiqueta de postagem
          </a>
        )}

        {/* Voucher */}
        {data.voucherCode && (
          <div className="bg-green-50 border border-green-200 rounded-2xl p-5">
            <h2 className="font-medium text-green-800 mb-1">🎁 Seu crédito está pronto!</h2>
            <p className="text-sm text-green-700">Código do voucher:</p>
            <p className="text-xl font-mono font-bold text-green-800 mt-1">{data.voucherCode}</p>
          </div>
        )}

        {/* Event log */}
        <div className="bg-white rounded-2xl border border-candy-border p-5 shadow-sm">
          <h2 className="font-medium text-candy-on-surface mb-3">Histórico</h2>
          <ol className="space-y-3">
            {data.events.map((evt, i) => (
              <li key={i} className="flex gap-3 text-sm">
                <div className="w-1.5 h-1.5 rounded-full bg-candy-primary mt-1.5 flex-shrink-0" />
                <div>
                  <p className="text-candy-on-surface">{evt.eventType}</p>
                  <p className="text-xs text-candy-subtle">
                    {new Date(evt.occurredAt).toLocaleString('pt-BR')}
                  </p>
                </div>
              </li>
            ))}
          </ol>
        </div>
      </main>
    </div>
  )
}
