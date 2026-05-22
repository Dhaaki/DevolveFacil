import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Package, LogOut, Truck, CheckCircle2, Clock, X, AlertTriangle, RotateCcw } from 'lucide-react'
import { api } from '../../lib/api'
import { useAuthStore } from '../../store/authStore'
import { cn } from '../../lib/utils'
import logo from '../../images/logo.png'

interface OrderItem {
  sku: string
  name: string
  quantity: number
  unitPrice: number
  imageUrl: string
}

interface Order {
  externalOrderId: string
  total: number
  currency: string
  orderedAt: string
  eligibleForReturn: boolean
  returnDeadlineDays: number
  awaitingDelivery: boolean
  items: OrderItem[]
}

export default function OrdersPage() {
  const name = useAuthStore((s) => s.name)
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()

  // Track which orders have been confirmed received (local state — persists for session)
  const [confirmedOrders, setConfirmedOrders] = useState<Set<string>>(new Set())

  // Confirm receipt modal state
  const [confirmingOrderId, setConfirmingOrderId] = useState<string | null>(null)
  const [notaFiscal, setNotaFiscal] = useState('')
  const [notaFiscalError, setNotaFiscalError] = useState('')
  const [justConfirmed, setJustConfirmed] = useState<string | null>(null)

  const { data: orders, isLoading, error } = useQuery<Order[]>({
    queryKey: ['customer-orders'],
    queryFn: () => api.get('/customer/orders').then((r) => r.data),
  })

  function openConfirmModal(orderId: string) {
    setConfirmingOrderId(orderId)
    setNotaFiscal('')
    setNotaFiscalError('')
  }

  function handleConfirmReceipt() {
    if (!notaFiscal.trim()) {
      setNotaFiscalError('Informe o número da nota fiscal para continuar.')
      return
    }
    if (!confirmingOrderId) return

    setConfirmedOrders((prev) => new Set([...prev, confirmingOrderId]))
    setJustConfirmed(confirmingOrderId)
    setConfirmingOrderId(null)
    setNotaFiscal('')

    // Clear the success banner after 6s
    setTimeout(() => setJustConfirmed(null), 6000)
  }

  return (
    <div className="min-h-screen bg-candy-bg">
      {/* Header */}
      <header className="sticky top-0 z-40 bg-candy-surface/80 backdrop-blur-xl border-b border-candy-surface-variant px-6 h-[70px] flex items-center justify-between">
        <div className="flex items-center gap-3">
          <img src={logo} alt="DevolveFacil" className="h-8 w-auto object-contain" />
          <span className="font-bold font-display text-candy-on-surface text-lg">DevolveFacil</span>
        </div>
        <div className="flex items-center gap-4">
          <span className="text-sm font-medium text-candy-on-surface-variant hidden sm:block">{name}</span>
          <button
            onClick={() => navigate('/returns')}
            className="flex items-center gap-1.5 text-sm font-bold text-candy-primary hover:opacity-80 transition-opacity"
          >
            <RotateCcw size={15} />
            <span className="hidden sm:block">Devoluções</span>
          </button>
          <button
            onClick={() => { logout(); navigate('/login') }}
            className="flex items-center gap-2 text-sm font-bold text-candy-on-surface-variant hover:text-candy-primary transition-colors"
          >
            <LogOut size={16} />
            <span className="hidden sm:block">Sair</span>
          </button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-4 py-8">
        <div className="mb-8">
          <h2 className="text-2xl font-bold font-display text-candy-on-surface tracking-tight">
            Meus Pedidos
          </h2>
          <p className="text-sm font-medium text-candy-on-surface-variant mt-1">
            Selecione um pedido para solicitar devolução ou troca
          </p>
        </div>

        {/* Success banner */}
        {justConfirmed && (
          <div className="mb-6 bg-green-500/10 border border-green-500/20 rounded-candy-card px-5 py-4 flex items-start gap-3">
            <CheckCircle2 className="text-green-500 shrink-0 mt-0.5" size={18} />
            <div>
              <p className="text-sm font-bold text-green-700">Recebimento confirmado!</p>
              <p className="text-sm text-green-600 mt-0.5">
                Pedido #{justConfirmed} confirmado. Você já pode solicitar uma devolução se necessário.
              </p>
            </div>
          </div>
        )}

        {isLoading && (
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="bg-candy-surface rounded-candy-card p-5 border border-candy-surface-variant animate-pulse h-36" />
            ))}
          </div>
        )}

        {error && (
          <div className="bg-red-500/10 border border-red-500/20 rounded-candy-card px-5 py-4 text-red-500 text-sm font-medium">
            Erro ao carregar pedidos. Tente novamente.
          </div>
        )}

        <div className="space-y-4">
          {orders?.map((order) => {
            const isConfirmed = confirmedOrders.has(order.externalOrderId)
            const canReturn = order.eligibleForReturn || isConfirmed
            const isAwaiting = order.awaitingDelivery && !isConfirmed

            return (
              <div
                key={order.externalOrderId}
                className={cn(
                  'bg-candy-surface rounded-candy-card border p-5 candy-shadow-sm transition-all',
                  isAwaiting
                    ? 'border-orange-200'
                    : canReturn
                    ? 'border-candy-primary/20'
                    : 'border-candy-surface-variant'
                )}
              >
                <div className="flex items-start justify-between mb-3 gap-3">
                  <div className="flex items-center gap-3">
                    <div className={cn(
                      'w-10 h-10 rounded-2xl flex items-center justify-center shrink-0',
                      isAwaiting
                        ? 'bg-orange-100 text-orange-500'
                        : canReturn
                        ? 'bg-candy-primary-container text-candy-primary'
                        : 'bg-candy-surface-variant text-candy-on-surface-variant'
                    )}>
                      {isAwaiting ? <Truck size={18} /> : canReturn ? <RotateCcw size={18} /> : <Package size={18} />}
                    </div>
                    <div>
                      <p className="font-bold text-candy-on-surface text-sm">
                        Pedido #{order.externalOrderId}
                      </p>
                      <p className="text-xs font-medium text-candy-on-surface-variant">
                        {new Date(order.orderedAt).toLocaleDateString('pt-BR', {
                          day: '2-digit', month: 'long', year: 'numeric'
                        })}
                      </p>
                    </div>
                  </div>

                  <div className="text-right shrink-0">
                    <p className="font-bold text-candy-on-surface text-sm">
                      {order.total.toLocaleString('pt-BR', { style: 'currency', currency: order.currency })}
                    </p>
                    {isAwaiting && (
                      <span className="inline-flex items-center gap-1 text-[11px] font-bold text-orange-500 bg-orange-500/10 px-2.5 py-0.5 rounded-full mt-1">
                        <Truck size={10} /> Aguardando entrega
                      </span>
                    )}
                    {isConfirmed && (
                      <span className="inline-flex items-center gap-1 text-[11px] font-bold text-green-600 bg-green-500/10 px-2.5 py-0.5 rounded-full mt-1">
                        <CheckCircle2 size={10} /> Recebido
                      </span>
                    )}
                    {!isAwaiting && !isConfirmed && canReturn && (
                      <span className="inline-flex items-center gap-1 text-[11px] font-bold text-candy-primary bg-candy-primary/10 px-2.5 py-0.5 rounded-full mt-1">
                        <Clock size={10} /> {order.returnDeadlineDays}d para devolver
                      </span>
                    )}
                    {!isAwaiting && !isConfirmed && !canReturn && (
                      <span className="text-[11px] font-bold text-candy-on-surface-variant bg-candy-surface-variant px-2.5 py-0.5 rounded-full mt-1 inline-block">
                        Fora do prazo
                      </span>
                    )}
                  </div>
                </div>

                <ul className="space-y-1 mb-4 pl-[52px]">
                  {order.items.slice(0, 2).map((item) => (
                    <li key={item.sku} className="text-xs font-medium text-candy-on-surface-variant">
                      {item.name} × {item.quantity}
                    </li>
                  ))}
                  {order.items.length > 2 && (
                    <li className="text-xs text-candy-on-surface-variant/60">
                      +{order.items.length - 2} item(s)
                    </li>
                  )}
                </ul>

                <div className="pl-[52px] flex gap-2">
                  {isAwaiting && (
                    <button
                      onClick={() => openConfirmModal(order.externalOrderId)}
                      className="flex-1 h-11 bg-orange-500 text-white font-bold rounded-full text-sm hover-bounce active-shrink shadow-lg shadow-orange-500/20"
                    >
                      Já recebi — confirmar entrega
                    </button>
                  )}
                  {canReturn && (
                    <button
                      onClick={() => navigate(`/returns/new?orderId=${order.externalOrderId}`)}
                      className="flex-1 h-11 bg-candy-primary text-white font-bold rounded-full text-sm hover-bounce active-shrink candy-glow"
                    >
                      Solicitar devolução
                    </button>
                  )}
                </div>
              </div>
            )
          })}
        </div>

        {orders?.length === 0 && (
          <div className="text-center py-20">
            <Package size={40} className="text-candy-on-surface-variant/40 mx-auto mb-3" />
            <p className="text-candy-on-surface-variant font-medium">Nenhum pedido encontrado.</p>
          </div>
        )}
      </main>

      {/* Confirm Receipt Modal */}
      {confirmingOrderId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={() => setConfirmingOrderId(null)} />
          <div className="relative bg-candy-surface rounded-candy-card p-6 w-full max-w-md candy-shadow border border-candy-surface-variant">
            <button
              onClick={() => setConfirmingOrderId(null)}
              className="absolute top-4 right-4 w-8 h-8 flex items-center justify-center rounded-2xl text-candy-on-surface-variant hover:bg-candy-surface-variant transition-colors"
            >
              <X size={16} />
            </button>

            <div className="flex items-center gap-3 mb-5">
              <div className="w-11 h-11 rounded-2xl bg-orange-100 flex items-center justify-center shrink-0">
                <CheckCircle2 className="text-orange-500" size={20} />
              </div>
              <div>
                <h3 className="font-bold text-candy-on-surface text-lg font-display">
                  Confirmar recebimento
                </h3>
                <p className="text-xs font-medium text-candy-on-surface-variant">
                  Pedido #{confirmingOrderId}
                </p>
              </div>
            </div>

            {/* Warning */}
            <div className="flex items-start gap-3 bg-orange-500/8 border border-orange-500/20 rounded-2xl px-4 py-3 mb-5">
              <AlertTriangle className="text-orange-500 shrink-0 mt-0.5" size={16} />
              <p className="text-sm font-medium text-orange-700 leading-snug">
                <strong>Atenção:</strong> Esta ação não pode ser desfeita. Confirme o recebimento somente após ter recebido o pedido fisicamente.
              </p>
            </div>

            <div className="mb-5">
              <label className="block text-[11px] font-bold uppercase tracking-widest text-candy-on-surface-variant mb-2">
                Número da Nota Fiscal
              </label>
              <input
                type="text"
                value={notaFiscal}
                onChange={(e) => { setNotaFiscal(e.target.value); setNotaFiscalError('') }}
                placeholder="Ex: 123456"
                autoFocus
                className={cn(
                  'w-full h-14 px-5 rounded-[24px] border-transparent',
                  'text-sm font-medium text-candy-on-surface placeholder:text-candy-on-surface-variant/50',
                  'outline-none transition-all',
                  notaFiscalError
                    ? 'bg-red-500/8 ring-2 ring-red-500/30'
                    : 'bg-candy-surface-variant focus:bg-candy-surface focus:ring-4 focus:ring-candy-primary/10'
                )}
              />
              {notaFiscalError && (
                <p className="text-red-500 text-xs font-medium mt-2">{notaFiscalError}</p>
              )}
            </div>

            <div className="flex gap-3">
              <button
                onClick={() => setConfirmingOrderId(null)}
                className="flex-1 h-12 font-bold text-sm text-candy-on-surface-variant bg-candy-surface-variant rounded-full hover:bg-candy-surface-variant/80 transition-colors active-shrink"
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirmReceipt}
                className="flex-1 h-12 bg-green-500 text-white font-bold text-sm rounded-full hover-bounce active-shrink shadow-lg shadow-green-500/25"
              >
                Confirmar recebimento
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
