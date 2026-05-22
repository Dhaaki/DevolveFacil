import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../../lib/api'
import { useAuthStore } from '../../store/authStore'
import { cn } from '../../lib/utils'
import logo from '../../images/logo.png'

export default function LoginPage() {
  const [cpf, setCpf] = useState('')

  function maskCpf(value: string) {
    return value
      .replace(/\D/g, '')
      .slice(0, 11)
      .replace(/(\d{3})(\d)/, '$1.$2')
      .replace(/(\d{3})(\d)/, '$1.$2')
      .replace(/(\d{3})(\d{1,2})$/, '$1-$2')
  }
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const setAuth = useAuthStore((s) => s.setAuth)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await api.post('/auth/customer/login', { cpf, password })
      setAuth(res.data.accessToken, 'customer', res.data.name)
      navigate('/orders')
    } catch {
      setError('CPF ou senha inválidos. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-candy-bg flex items-center justify-center px-4">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 rounded-full bg-candy-primary/8 blur-3xl" />
        <div className="absolute -bottom-40 -left-40 w-96 h-96 rounded-full bg-candy-secondary/8 blur-3xl" />
      </div>

      <div className="relative w-full max-w-sm">
        {/* Logo + brand */}
        <div className="flex flex-col items-center mb-8 gap-3">
          <img
            src={logo}
            alt="DevolveFacil"
            className="h-28 w-auto object-contain animate-bounce-subtle"
          />
          <div className="text-center">
            <h1 className="text-2xl font-bold font-display text-candy-on-surface tracking-tight">
              DevolveFacil
            </h1>
            <p className="text-sm font-medium text-candy-on-surface-variant mt-0.5">
              Portal de Devoluções La Moda
            </p>
          </div>
        </div>

        {/* Card */}
        <div className="bg-candy-surface rounded-candy-card p-8 candy-shadow border border-candy-surface-variant">
          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-[11px] font-bold uppercase tracking-widest text-candy-on-surface-variant mb-2">
                CPF
              </label>
              <input
                type="text"
                value={cpf}
                onChange={(e) => setCpf(maskCpf(e.target.value))}
                placeholder="000.000.000-00"
                required
                className={cn(
                  'w-full h-14 px-5 bg-candy-surface-variant rounded-[24px] border-transparent',
                  'text-sm font-medium text-candy-on-surface placeholder:text-candy-on-surface-variant/50',
                  'outline-none focus:bg-candy-surface focus:ring-4 focus:ring-candy-primary/10 transition-all'
                )}
              />
            </div>

            <div>
              <label className="block text-[11px] font-bold uppercase tracking-widest text-candy-on-surface-variant mb-2">
                Senha
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className={cn(
                  'w-full h-14 px-5 bg-candy-surface-variant rounded-[24px] border-transparent',
                  'text-sm font-medium text-candy-on-surface placeholder:text-candy-on-surface-variant/50',
                  'outline-none focus:bg-candy-surface focus:ring-4 focus:ring-candy-primary/10 transition-all'
                )}
              />
            </div>

            {error && (
              <p className="text-red-500 text-sm font-medium bg-red-500/8 rounded-2xl px-4 py-3">
                {error}
              </p>
            )}

            <button
              type="submit"
              disabled={loading}
              className={cn(
                'w-full h-14 bg-candy-primary text-white font-bold rounded-full',
                'hover-bounce active-shrink candy-glow',
                'disabled:opacity-50 disabled:cursor-not-allowed disabled:shadow-none',
                'transition-all text-sm'
              )}
            >
              {loading ? 'Entrando...' : 'Entrar'}
            </button>
          </form>
        </div>

        <p className="text-center text-xs text-candy-on-surface-variant mt-6">
          Dúvidas? Fale com nosso{' '}
          <span className="text-candy-primary font-semibold cursor-pointer">suporte</span>
        </p>
      </div>
    </div>
  )
}
