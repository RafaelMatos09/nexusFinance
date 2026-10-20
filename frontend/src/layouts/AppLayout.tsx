import type { ReactNode } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { clearSession, getUser } from '../lib/api.ts'
import { initials } from '../lib/format.ts'

const icon = (path: ReactNode) => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
    {path}
  </svg>
)

const navigation = [
  { label: 'Painel', to: '/', icon: icon(<><rect x="3" y="3" width="7" height="9" rx="1.5" /><rect x="14" y="3" width="7" height="5" rx="1.5" /><rect x="14" y="12" width="7" height="9" rx="1.5" /><rect x="3" y="16" width="7" height="5" rx="1.5" /></>) },
  { label: 'Clientes', to: '/clientes', icon: icon(<><circle cx="9" cy="8" r="3.5" /><path d="M2.5 20a6.5 6.5 0 0 1 13 0" /><path d="M16 4a3.5 3.5 0 0 1 0 7M21.5 20a6.5 6.5 0 0 0-4.5-6.2" /></>) },
  { label: 'Contratos', to: '/contratos', icon: icon(<><path d="M7 3h7l5 5v13H7z" /><path d="M14 3v5h5M10 13h6M10 17h6" /></>) },
  { label: 'Cobranças', to: '/cobrancas', icon: icon(<><rect x="2.5" y="6" width="19" height="12" rx="2" /><path d="M2.5 10h19M7 14h3" /></>) },
  { label: 'Relatórios', to: '/relatorios', icon: icon(<><path d="M4 20V10M10 20V4M16 20v-7M22 20H2" /></>) },
]

export function AppLayout() {
  const navigate = useNavigate()
  const user = getUser()
  const today = new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })

  return (
    <div className="flex min-h-screen text-ink">
      <aside className="hidden w-64 shrink-0 flex-col bg-ink text-paper lg:flex">
        <div className="flex items-center gap-3 px-6 pb-6 pt-7">
          <span className="grid h-9 w-9 place-items-center rounded-xl bg-copper font-display text-lg text-paper">N</span>
          <div>
            <p className="font-display text-xl leading-none tracking-tight">Nexus Finance</p>
            <p className="mt-1 text-xs text-paper/60">Cobrança para PMEs</p>
          </div>
        </div>
        <nav className="flex flex-1 flex-col gap-1 px-3" aria-label="Principal">
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm transition ${isActive ? 'bg-white/12 font-medium text-white shadow-inner' : 'text-paper/70 hover:bg-white/6 hover:text-white'}`
              }
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="mx-3 mb-4 rounded-xl border border-white/10 bg-white/5 p-4 text-xs text-paper/70">
          <p className="font-medium text-paper">Ambiente de demonstração</p>
          <p className="mt-1">Todos os dados são fictícios e podem ser alterados livremente.</p>
        </div>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-16 items-center justify-between border-b border-line bg-paper/85 px-5 backdrop-blur lg:px-8">
          <div className="flex items-center gap-3">
            <span className="grid h-8 w-8 place-items-center rounded-lg bg-ink font-display text-paper lg:hidden">N</span>
            <p className="hidden text-sm capitalize text-ink/60 sm:block">{today}</p>
          </div>
          <div className="flex items-center gap-3">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-medium leading-none">{user?.name ?? 'Demonstração'}</p>
              <p className="mt-1 text-xs text-ink/50">{user?.email}</p>
            </div>
            <span className="grid h-9 w-9 place-items-center rounded-full bg-moss/15 text-xs font-semibold text-moss">{initials(user?.name ?? 'NF')}</span>
            <button
              type="button"
              className="rounded-xl border border-line bg-card px-3 py-1.5 text-xs font-medium text-ink/70 transition hover:border-copper hover:text-copper"
              onClick={() => {
                clearSession()
                navigate('/login')
              }}
            >
              Sair
            </button>
          </div>
        </header>
        <nav className="flex gap-1 overflow-x-auto border-b border-line bg-card px-3 py-2 lg:hidden" aria-label="Principal (compacta)">
          {navigation.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.to === '/'} className={({ isActive }) => `whitespace-nowrap rounded-lg px-3 py-1.5 text-sm ${isActive ? 'bg-ink text-paper' : 'text-ink/70'}`}>
              {item.label}
            </NavLink>
          ))}
        </nav>
        <main className="flex-1 px-5 py-8 lg:px-8">
          <div className="mx-auto max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
