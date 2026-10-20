import { useQuery } from '@tanstack/react-query'
import { fetchHealth } from '../lib/api.ts'

export function HomePage() {
  const health = useQuery({
    queryKey: ['health'],
    queryFn: fetchHealth,
  })

  return (
    <section className="mx-auto max-w-3xl">
      <p className="text-sm font-medium uppercase tracking-[0.16em] text-copper">Início</p>
      <h1 className="mt-2 font-display text-4xl text-ink">Gestão financeira fictícia</h1>
      <p className="mt-3 max-w-xl text-base leading-relaxed text-ink/75">
        Base do Nexus Finance. O restante dos módulos entra nos próximos épicos. A conexão abaixo
        consulta /api/health neste mesmo endereço.
      </p>

      <article className="mt-8 rounded-2xl border border-line bg-card p-6">
        <h2 className="font-display text-2xl">Saúde da API</h2>
        {health.isPending ? (
          <p className="mt-4 h-16 animate-pulse rounded-xl bg-line/70" aria-busy="true">
            <span className="sr-only">Consultando a API…</span>
          </p>
        ) : null}
        {health.isError ? (
          <p className="mt-4 rounded-xl bg-copper/10 px-4 py-3 text-sm text-copper" role="alert">
            {health.error.message}
          </p>
        ) : null}
        {health.isSuccess && !health.data.status ? (
          <p className="mt-4 text-sm text-ink/70">A API respondeu sem dados de status.</p>
        ) : null}
        {health.isSuccess && health.data.status ? (
          <dl className="mt-4 grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl bg-paper px-4 py-3">
              <dt className="text-xs uppercase tracking-wide text-ink/60">API</dt>
              <dd className="mt-1 text-lg font-medium text-moss">{health.data.status}</dd>
            </div>
            <div className="rounded-xl bg-paper px-4 py-3">
              <dt className="text-xs uppercase tracking-wide text-ink/60">Banco</dt>
              <dd className="mt-1 text-lg font-medium text-moss">{health.data.database}</dd>
            </div>
          </dl>
        ) : null}
      </article>
    </section>
  )
}
