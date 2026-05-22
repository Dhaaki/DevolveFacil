import { useState, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { api } from '../../lib/api'

export default function QualityAssessmentPage() {
  const { returnId = '' } = useParams()
  const navigate = useNavigate()

  const [result, setResult] = useState<'Approved' | 'Rejected'>('Approved')
  const [notes, setNotes] = useState('')
  const [images, setImages] = useState<File[]>([])
  const fileRef = useRef<HTMLInputElement>(null)

  const uploadMutation = useMutation({
    mutationFn: async () => {
      if (images.length === 0) return { imageKeys: [] }
      const form = new FormData()
      images.forEach((img) => form.append('files', img))
      const res = await api.post('/uploads/damage-images', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return res.data as { imageKeys: string[] }
    },
  })

  const assessMutation = useMutation({
    mutationFn: (payload: object) =>
      api.patch(`/admin/returns/${returnId}/quality`, payload).then((r) => r.data),
    onSuccess: () => navigate(`/admin/returns/${returnId}`),
  })

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const uploadResult = await uploadMutation.mutateAsync()
    assessMutation.mutate({ result, notes, damageImageKeys: uploadResult.imageKeys })
  }

  function handleFiles(e: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? []).slice(0, 5)
    setImages(files)
  }

  const isPending = uploadMutation.isPending || assessMutation.isPending

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white border-b border-gray-200 px-6 py-3 flex items-center">
        <button onClick={() => navigate(`/admin/returns/${returnId}`)} className="text-gray-600 text-sm font-medium">
          ← Voltar
        </button>
        <h1 className="text-lg font-semibold text-gray-800 ml-4">Avaliação de Qualidade</h1>
      </header>

      <main className="max-w-lg mx-auto px-6 py-8">
        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <h2 className="font-medium text-gray-700 mb-3">Resultado da avaliação</h2>
            <div className="grid grid-cols-2 gap-3">
              {(['Approved', 'Rejected'] as const).map((r) => (
                <button
                  key={r}
                  type="button"
                  onClick={() => setResult(r)}
                  className={`py-3 rounded-xl border-2 text-sm font-medium transition-colors ${
                    result === r
                      ? r === 'Approved'
                        ? 'border-green-500 bg-green-50 text-green-700'
                        : 'border-red-500 bg-red-50 text-red-700'
                      : 'border-gray-200 text-gray-600 hover:border-gray-300'
                  }`}
                >
                  {r === 'Approved' ? '✓ Aprovado' : '✗ Reprovado'}
                </button>
              ))}
            </div>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <h2 className="font-medium text-gray-700 mb-2">Observações</h2>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder={result === 'Rejected' ? 'Descreva o motivo da reprovação...' : 'Observações (opcional)'}
              required={result === 'Rejected'}
              rows={4}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gray-800 resize-none"
            />
          </div>

          {result === 'Rejected' && (
            <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
              <h2 className="font-medium text-gray-700 mb-2">Fotos de evidência (até 5)</h2>
              <input
                ref={fileRef}
                type="file"
                accept="image/*"
                multiple
                onChange={handleFiles}
                className="hidden"
              />
              <button
                type="button"
                onClick={() => fileRef.current?.click()}
                className="w-full border-2 border-dashed border-gray-300 rounded-lg py-4 text-sm text-gray-500 hover:border-gray-400 transition-colors"
              >
                {images.length > 0 ? `${images.length} foto(s) selecionada(s)` : 'Clique para adicionar fotos'}
              </button>
              {images.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-2">
                  {images.map((img) => (
                    <span key={img.name} className="text-xs bg-gray-100 px-2 py-1 rounded">
                      {img.name}
                    </span>
                  ))}
                </div>
              )}
            </div>
          )}

          {(uploadMutation.isError || assessMutation.isError) && (
            <p className="text-red-500 text-sm text-center">Erro ao salvar avaliação. Tente novamente.</p>
          )}

          <button
            type="submit"
            disabled={isPending}
            className={`w-full font-semibold py-3 rounded-xl transition-colors text-white disabled:opacity-50 ${
              result === 'Approved' ? 'bg-green-600 hover:bg-green-700' : 'bg-red-600 hover:bg-red-700'
            }`}
          >
            {isPending ? 'Salvando...' : result === 'Approved' ? 'Aprovar devolução' : 'Reprovar devolução'}
          </button>
        </form>
      </main>
    </div>
  )
}
