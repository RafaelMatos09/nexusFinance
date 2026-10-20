import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Area, AreaChart, Bar, BarChart, CartesianGrid, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { Badge, DataTable, EmptyText, ErrorText, LoadingBlock, PageHeader, Panel, StatCard, type Column } from '../components/ui.tsx'
import { api, type Dashboard } from '../lib/api.ts'
import { compactMoney, money, monthLabel } from '../lib/format.ts'
import { chargeStatusLabel, statusColor } from '../lib/labels.ts'

type Debtor = Dashboard['topDebtors'][number]

const tooltipStyle = { borderRadius: 12, border: '1px solid #e4dccf', background: '#fbf8f2', fontSize: 12 }

export function DashboardPage() {
  const dashboard = useQuery({ queryKey: ['dashboard'], queryFn: () => api<Dashboard>('/api/dashboard') })

  if (dashboard.isPending) {
    return (
      <div className="space-y-6">
        <PageHeader eyebrow="Painel" title="Como está a carteira" />
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">{Array.from({ length: 4 }).map((_, index) => <LoadingBlock key={index} className="h-32" />)}</div>
        <LoadingBlock className="h-72" />
      </div>
    )
  }
  if (dashboard.isError) return <ErrorText message={dashboard.error.message} />

  const data = dashboard.data
  const chart = data.receiptsByMonth.map((item) => ({ name: monthLabel(item.year, item.month), amount: item.amount }))
  const previous = chart.at(-2)?.amount ?? 0
  const variation = previous ? ((data.receivedThisMonth - previous) / previous) * 100 : null
  const breakdown = data.statusBreakdown ?? []
  const slices = breakdown.filter((slice) => slice.count > 0)
  const aging = data.aging.map((bucket) => ({ ...bucket, label: `${bucket.label} dias` }))
  const empty = data.totalReceivable === 0 && data.receivedThisMonth === 0 && breakdown.every((slice) => slice.count === 0)

  const debtorColumns: Column<Debtor>[] = [
    { key: 'client', header: 'Cliente', cell: (row) => <Link className="font-medium hover:text-copper" to={`/clientes/${row.clientId}`}>{row.clientName}</Link> },
    { key: 'count', header: 'Cobranças', align: 'center', cell: (row) => row.chargeCount },
    { key: 'amount', header: 'Saldo em atraso', align: 'right', cell: (row) => <span className="font-medium tabular-nums text-copper">{money(row.amount)}</span> },
    { key: 'share', header: 'Participação', align: 'right', cell: (row) => <ShareBar value={data.overdue ? (row.amount / data.overdue) * 100 : 0} /> },
  ]

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Painel"
        title="Como está a carteira"
        description="Visão consolidada de recebíveis, atrasos e recebimentos dos últimos seis meses."
        actions={<Link to="/cobrancas" className="rounded-xl bg-ink px-4 py-2 text-sm font-medium text-paper hover:bg-ink/90">Ver cobranças</Link>}
      />

      {empty ? <EmptyText message="Ainda não há cobranças para resumir. Ative um contrato para gerar as primeiras." /> : null}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard label="A receber" value={money(data.totalReceivable)} hint={`${data.openCharges ?? 0} cobrança(s) em aberto`} icon={<Icon d="M12 3v18M7 8h7a3 3 0 0 1 0 6H9a3 3 0 0 0 0 6h8" />} />
        <StatCard
          label="Recebido no mês"
          value={money(data.receivedThisMonth)}
          tone="good"
          hint={variation === null ? 'Sem base de comparação' : `${variation >= 0 ? '+' : ''}${variation.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}% vs. mês anterior`}
          icon={<Icon d="M4 17l6-6 4 4 6-7M14 8h6v6" />}
        />
        <StatCard label="Vencido" value={money(data.overdue)} tone="bad" hint={`${data.overdueCharges ?? 0} cobrança(s) vencida(s)`} icon={<Icon d="M12 8v5M12 16h.01M10.3 3.9L2.6 17.3A2 2 0 0 0 4.3 20h15.4a2 2 0 0 0 1.7-2.7L13.7 3.9a2 2 0 0 0-3.4 0z" />} />
        <StatCard
          label="Inadimplência"
          value={`${data.delinquencyRate.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`}
          tone="accent"
          hint={`Ticket médio ${money(data.averageTicket ?? 0)}`}
          icon={<Icon d="M21 12a9 9 0 1 1-9-9M21 3l-9 9M15 3h6v6" />}
        />
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.6fr_1fr]">
        <Panel title="Recebimentos" subtitle="Últimos seis meses, por data de pagamento">
          {chart.every((item) => item.amount === 0) ? (
            <EmptyText message="Nenhum recebimento no período." />
          ) : (
            <div className="h-72">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={chart} margin={{ top: 10, right: 8, left: 0, bottom: 0 }}>
                  <defs>
                    <linearGradient id="receipts" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#1f6b4a" stopOpacity={0.35} />
                      <stop offset="100%" stopColor="#1f6b4a" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid vertical={false} stroke="#e4dccf" strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fill: '#14281f', fontSize: 12 }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fill: '#14281f', fontSize: 12 }} axisLine={false} tickLine={false} tickFormatter={(value: number) => compactMoney(value)} width={72} />
                  <Tooltip contentStyle={tooltipStyle} formatter={(value) => [money(Number(value ?? 0)), 'Recebido']} />
                  <Area type="monotone" dataKey="amount" stroke="#1f6b4a" strokeWidth={2.5} fill="url(#receipts)" dot={{ r: 3, fill: '#1f6b4a' }} activeDot={{ r: 5 }} />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          )}
        </Panel>

        <Panel title="Cobranças por status" subtitle="Quantidade e valor atualizado">
          {slices.length === 0 ? (
            <EmptyText message="Sem cobranças." />
          ) : (
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center xl:flex-col">
              <div className="h-44 w-full sm:w-1/2 xl:w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={slices} dataKey="count" nameKey="status" innerRadius={48} outerRadius={70} paddingAngle={3} stroke="none">
                      {slices.map((slice) => <Cell key={slice.status} fill={statusColor[slice.status] ?? '#8a8f8b'} />)}
                    </Pie>
                    <Tooltip contentStyle={tooltipStyle} formatter={(value, name) => [`${value} cobrança(s)`, chargeStatusLabel[String(name)] ?? name]} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
              <ul className="flex-1 space-y-2 text-sm">
                {slices.map((slice) => (
                  <li key={slice.status} className="flex items-center justify-between gap-3">
                    <Badge status={slice.status} label={chargeStatusLabel[slice.status] ?? slice.status} />
                    <span className="text-ink/60">{slice.count}</span>
                    <span className="font-medium tabular-nums">{money(slice.amount)}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </Panel>
      </div>

      <div className="grid gap-4 xl:grid-cols-[1fr_1.6fr]">
        <Panel title="Aging" subtitle="Saldo em aberto por faixa de atraso">
          <div className="h-56">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={aging} layout="vertical" margin={{ top: 0, right: 16, left: 0, bottom: 0 }}>
                <CartesianGrid horizontal={false} stroke="#e4dccf" strokeDasharray="3 3" />
                <XAxis type="number" hide />
                <YAxis type="category" dataKey="label" width={84} tick={{ fill: '#14281f', fontSize: 12 }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={tooltipStyle} cursor={{ fill: 'rgba(20,40,31,0.04)' }} formatter={(value) => [money(Number(value ?? 0)), 'Saldo']} />
                <Bar dataKey="amount" radius={[0, 8, 8, 0]} barSize={18}>
                  {aging.map((bucket, index) => <Cell key={bucket.label} fill={['#1f6b4a', '#b8891c', '#c4622d', '#8a3a1c'][index]} />)}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </Panel>

        <div>
          <h2 className="mb-3 font-display text-xl">Top 5 clientes em atraso</h2>
          <DataTable columns={debtorColumns} rows={data.topDebtors} rowKey={(row) => row.clientId} emptyMessage="Nenhum cliente em atraso. Carteira saudável." />
        </div>
      </div>
    </div>
  )
}

function ShareBar({ value }: { value: number }) {
  return (
    <div className="ml-auto flex w-32 items-center gap-2">
      <div className="h-1.5 flex-1 rounded-full bg-line">
        <div className="h-1.5 rounded-full bg-copper" style={{ width: `${Math.min(100, value)}%` }} />
      </div>
      <span className="w-10 text-right text-xs tabular-nums text-ink/60">{value.toLocaleString('pt-BR', { maximumFractionDigits: 0 })}%</span>
    </div>
  )
}

function Icon({ d }: { d: string }) {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d={d} />
    </svg>
  )
}
