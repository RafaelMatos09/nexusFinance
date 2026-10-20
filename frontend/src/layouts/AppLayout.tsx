import { NavLink, Outlet } from 'react-router-dom'

const navigation = [
  { label: 'Início', to: '/' },
  { label: 'Clientes' },
  { label: 'Contratos' },
  { label: 'Cobranças' },
  { label: 'Recebimentos' },
  { label: 'Relatórios' },
]

export function AppLayout() {
  return (
    <div className="flex min-h-screen bg-paper text-ink">
      <aside className="flex w-64 shrink-0 flex-col bg-ink text-paper">
        <div className="px-6 pb-4 pt-7">
          <p className="font-display text-2xl tracking-tight">Nexus Finance</p>
          <p className="mt-1 text-sm text-paper/70">Cobrança para PMEs</p>
        </div>
        <nav className="flex flex-1 flex-col gap-1 px-3" aria-label="Principal">
          {navigation.map((item) =>
            item.to ? (
              <NavLink
                key={item.label}
                to={item.to}
                className={({ isActive }) =>
                  `rounded-lg px-3 py-2 text-sm ${
                    isActive ? 'bg-white/10 text-white' : 'text-paper/80 hover:bg-white/5'
                  }`
                }
              >
                {item.label}
              </NavLink>
            ) : (
              <span
                key={item.label}
                className="flex items-center justify-between rounded-lg px-3 py-2 text-sm text-paper/40"
              >
                {item.label}
                <span className="text-[10px] uppercase tracking-wide">em breve</span>
              </span>
            ),
          )}
        </nav>
        <p className="px-6 py-5 text-xs text-paper/50">Dados fictícios</p>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-16 items-center justify-between border-b border-line bg-card px-8">
          <p className="text-sm text-ink/70">Ambiente de demonstração</p>
          <span className="rounded-full bg-copper/10 px-3 py-1 text-xs font-medium text-copper">
            MVP
          </span>
        </header>
        <main className="flex-1 px-8 py-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
