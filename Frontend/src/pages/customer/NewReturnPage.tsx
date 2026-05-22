import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import { api } from '../../lib/api'

const REASON_CODES = [
  { value: 'defective', label: 'Produto com defeito' },
  { value: 'wrong_item', label: 'Item errado' },
  { value: 'not_as_described', label: 'Diferente da descrição' },
  { value: 'changed_mind', label: 'Mudança de decisão' },
  { value: 'size_issue', label: 'Problema de tamanho' },
]

interface OrderItem {
  id: string
  sku: string
  name: string
  quantity: number
  unitPrice: number
}

interface Order {
  externalOrderId: string
  total: number
  currency: string
  items: OrderItem[]
}

export default function NewReturnPage() {
  const [params] = useSearchParams()
  const orderId = params.get('orderId') ?? ''
  const navigate = useNavigate()

  const [selectedItems, setSelectedItems] = useState<Record<string, number>>({})
  const [resolutionType, setResolutionType] = useState<'Refund' | 'StoreCredit'>('StoreCredit')
  const [reasonCode, setReasonCode] = useState('defective')
  const [reasonNotes, setReasonNotes] = useState('')

  const { data: order, isLoading } = useQuery<Order>({
    queryKey: ['order', orderId],
    queryFn: () => api.get(`/customer/orders/${orderId}`).then((r) => r.data),
    enabled: !!orderId,
  })

  const mutation = useMutation({
    mutationFn: (payload: object) => api.post('/customer/returns', payload).then((r) => r.data),
    onSuccess: (data) => navigate(`/returns/${data.id}`),
  })

  function toggleItem(itemId: string, maxQty: number) {
    setSelectedItems((prev) => {
      if (prev[itemId]) {
        const { [itemId]: _, ...rest } = prev
        return rest
      }
      return { ...prev, [itemId]: maxQty }
    })
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    mutation.mutate({
      orderExternalId: orderId,
      items: Object.entries(selectedItems).map(([orderItemId, quantityReturned]) => ({
        orderItemId,
        quantityReturned,
      })),
      resolutionType,
      reasonCode,
      reasonNotes,
    })
  }

  if (isLoading) return <div className="p-8 text-gray-500">Carregando pedido...</div>
  if (!order) return <div className="p-8 text-red-500">Pedido não encontrado.</div>

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200 px-4 py-3">
        <button onClick={() => navigate('/orders')} className="text-brand-600 text-sm font-medium">
          ← Voltar
        </button>
        <h1 className="text-lg font-semibold text-gray-800 mt-1">Solicitar Devolução</h1>
      </header>

      <main className="max-w-lg mx-auto px-4 py-8">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <h2 className="font-medium text-gray-700 mb-3">Selecione os itens</h2>
            <div className="space-y-3">
              {order.items.map((item) => (
                <label key={item.id} className="flex items-center gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={!!selectedItems[item.id]}
                    onChange={() => toggleItem(item.id, item.quantity)}
                    className="accent-brand-600 w-4 h-4"
                  />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-gray-800">{item.name}</p>
                    <p className="text-xs text-gray-400">
                      {item.unitPrice.toLocaleString('pt-BR', { style: 'currency', currency: order.currency })} · Qtd: {item.quantity}
                    </p>
                  </div>
                </label>
              ))}
            </div>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <h2 className="font-medium text-gray-700 mb-3">Tipo de resolução</h2>
            <div className="grid grid-cols-2 gap-3">
              {(['StoreCredit', 'Refund'] as const).map((type) => (
                <button
                  key={type}
                  type="button"
                  onClick={() => setResolutionType(type)}
                  className={`py-3 rounded-xl border-2 text-sm font-medium transition-colors ${
                    resolutionType === type
                      ? 'border-brand-500 bg-brand-50 text-brand-700'
                      : 'border-gray-200 text-gray-600 hover:border-gray-300'
                  }`}
                >
                  {type === 'StoreCredit' ? '🎁 Crédito na Loja' : '💰 Reembolso'}
                </button>
              ))}
            </div>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm space-y-3">
            <h2 className="font-medium text-gray-700">Motivo</h2>
            <select
              value={reasonCode}
              onChange={(e) => setReasonCode(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
            >
              {REASON_CODES.map((r) => (
                <option key={r.value} value={r.value}>{r.label}</option>
              ))}
            </select>
            <textarea
              value={reasonNotes}
              onChange={(e) => setReasonNotes(e.target.value)}
              placeholder="Detalhes adicionais (opcional)"
              rows={3}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 resize-none"
            />
          </div>

          {mutation.isError && (
            <p className="text-red-500 text-sm text-center">Erro ao enviar solicitação. Tente novamente.</p>
          )}

          <button
            type="submit"
            disabled={Object.keys(selectedItems).length === 0 || mutation.isPending}
            className="w-full bg-brand-600 text-white font-semibold py-3 rounded-xl hover:bg-brand-700 disabled:opacity-50 transition-colors"
          >
            {mutation.isPending ? 'Enviando...' : 'Confirmar Devolução'}
          </button>
        </form>
      </main>
    </div>
  )
}
