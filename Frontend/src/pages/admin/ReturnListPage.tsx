import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { api } from '../../lib/api'

interface ReturnSummary {
  id: string
  requestNumber: string
  status: string
  resolutionType: string
  customerName: string
  carrierCode?: string
  createdAt: string
}

const STATUS_BADGE: Record<string, string> = {
  PendingLabel: 'bg-yellow-100 text-yellow-700',
  LabelGenerated: 'bg-blue-100 text-blue-700',
  InTransit: 'bg-blue-200 text-blue-800',
  Delivered: 'bg-orange-100 text-orange-700',
  QualityApproved: 'bg-green-100 text-green-700',
  QualityRejected: 'bg-red-100 text-red-700',
  ClosedExchange: 'bg-green-200 text-green-800',
  ClosedRefund: 'bg-green-200 text-green-800',
  ClosedRejected: 'bg-gray-100 text-gray-600',
  Cancelled: 'bg-gray-100 text-gray-400',
}

export default function ReturnListPage() {
  const navigate = useNavigate()
  const [status, setStatus] = useState('')

  const { data: returns, isLoading } = useQuery<ReturnSummary[]>({
    queryKey: ['admin-returns', status],
    queryFn: () =>
      api.get('/admin/returns', { params: status ? { status } : {} }).then((r) => r.data),
  })

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between">
        <button onClick={() => navigate('/admin')} className="text-gray-600 text-sm font-medium">← Dashboard</button>
        <h1 className="text-lg font-semibold text-gray-800">Todas as Devoluções</h1>
        <div />
      </header>

      <main className="max-w-5xl mx-auto px-6 py-8">
        <div className="flex gap-3 mb-6 flex-wrap">
          {['', 'Delivered', 'InTransit', 'PendingLabel', 'ClosedExchange', 'ClosedRefund'].map((s) => (
            <button
              key={s}
              onClick={() => setStatus(s)}
              className={`text-sm px-3 py-1 rounded-full border transition-colors ${
                status === s ? 'bg-gray-800 text-white border-gray-800' : 'bg-white text-gray-600 border-gray-300 hover:border-gray-500'
              }`}
            >
              {s || 'Todas'}
            </button>
          ))}
        </div>

        {isLoading && <p className="text-gray-500">Carregando...</p>}

        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Número</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Cliente</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Status</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Resolução</th>
                <th className="text-left px-4 py-3 font-medium text-gray-500">Data</th>
                <th />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {returns?.map((r) => (
                <tr key={r.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 font-mono text-gray-800">{r.requestNumber}</td>
                  <td className="px-4 py-3 text-gray-700">{r.customerName}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_BADGE[r.status] ?? 'bg-gray-100 text-gray-500'}`}>
                      {r.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-600">
                    {r.resolutionType === 'StoreCredit' ? 'Crédito' : 'Reembolso'}
                  </td>
                  <td className="px-4 py-3 text-gray-400">
                    {new Date(r.createdAt).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => navigate(`/admin/returns/${r.id}`)}
                      className="text-brand-600 hover:underline text-xs font-medium"
                    >
                      Ver →
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {returns?.length === 0 && (
            <p className="text-center text-gray-400 py-8">Nenhuma devolução encontrada.</p>
          )}
        </div>
      </main>
    </div>
  )
}
