import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Badge, Button, DataTable, Feedback, inputClass, PageHeader, StatCard, Toolbar, type Column } from '../components/ui.tsx'
import { api, getToken, queryString, type Charge, type ChargeReport, type Client } from '../lib/api.ts'
import { dateLabel, money } from '../lib/format.ts'
import { chargeStatusLabel } from '../lib/labels.ts'

const PREVIEW = 25

export function RelatoriosPage() {
  const [clientId, setClientId] = useState('')
  const [status, setStatus] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [feedback, setFeedback] = useState('')
  const [exporting, setExporting] = useState(false)
  const options = useQuery({ queryKey: ['client-options'], queryFn: () => api<Client[]>('/api/clients/options') })
  const report = useQuery({
    queryKey: ['report', clientId, status, from, to],
    queryFn: () => api<ChargeReport>(`/api/reports/charges${queryString({ clientId, status, from, to })}`),
  })

  async function exportExcel() {
    setExporting(true)
    setFeedback('Preparando o arquivo…')
    try {
      const response = await fetch(`/api/reports/charges/excel${queryString({ clientId, status, from, to })}`, {
        headers: { Authorization: `Bearer ${getToken() ?? ''}` },
      })
      if (!response.ok) throw new Error('Não foi possível exportar.')
      const url = URL.createObjectURL(await response.blob())
      const link = document.createElement('a')
      link.href = url
      link.download = 'cobrancas.xlsx'
      link.click()
      URL.revokeObjectURL(url)
      setFeedback('Download iniciado. O arquivo tem as abas DASHBOARD e DADOS.')
    } catch (error) {
      setFeedback(error instanceof Error ? error.message : 'Não foi possível exportar.')
    } finally {
      setExporting(false)
    }
  }

  const columns: Column<Charge>[] = [
    { key: 'id', header: '#', width: '4rem', cell: (row) => <span className="text-ink/50">{row.id}</span> },
    { key: 'client', header: 'Cliente', cell: (row) => <span><span className="block font-medium">{row.clientName}</span><span className="block text-xs text-ink/50">{row.contractDescription}</span></span> },
    { key: 'due', header: 'Vencimento', cell: (row) => <span className="tabular-nums">{dateLabel(row.dueDate)}</span> },
    { key: 'status', header: 'Status', cell: (row) => <Badge status={row.status} label={chargeStatusLabel[row.status] ?? row.status} /> },
    { key: 'original', header: 'Original', align: 'right', cell: (row) => <span className="tabular-nums">{money(row.originalAmount)}</span> },
    { key: 'fees', header: 'Multa + juros', align: 'right', cell: (row) => <span className="tabular-nums">{money(row.fine + row.interest)}</span> },
    { key: 'updated', header: 'Atualizado', align: 'right', cell: (row) => <span className="tabular-nums">{money(row.updatedAmount)}</span> },
    { key: 'paid', header: 'Pago', align: 'right', cell: (row) => <span className="tabular-nums text-moss">{money(row.amountPaid)}</span> },
    { key: 'balance', header: 'Saldo', align: 'right', cell: (row) => <span className="font-semibold tabular-nums">{money(row.balance)}</span> },
  ]

  const rows = report.data?.rows.slice(0, PREVIEW)
  const hidden = (report.data?.rows.length ?? 0) - (rows?.length ?? 0)

  return (
    <div>
      <PageHeader
        eyebrow="Relatório"
        title="Cobranças"
        description="Filtre por período, cliente e status. A exportação em Excel respeita os filtros aplicados."
        actions={<Button onClick={exportExcel} disabled={exporting || report.isPending}>{exporting ? 'Gerando…' : 'Exportar Excel'}</Button>}
      />
      <Toolbar>
        <select className={`${inputClass} max-w-xs`} value={clientId} onChange={(event) => setClientId(event.target.value)}>
          <option value="">Todos os clientes</option>
          {options.data?.map((client) => <option key={client.id} value={client.id}>{client.name}</option>)}
        </select>
        <select className={`${inputClass} max-w-[10rem]`} value={status} onChange={(event) => setStatus(event.target.value)}>
          <option value="">Todos</option>
          {Object.entries(chargeStatusLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <label className="flex items-center gap-2 text-xs text-ink/60">De<input className={inputClass} type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></label>
        <label className="flex items-center gap-2 text-xs text-ink/60">Até<input className={inputClass} type="date" value={to} onChange={(event) => setTo(event.target.value)} /></label>
      </Toolbar>
      <Feedback message={feedback} tone={feedback.startsWith('Não') ? 'bad' : 'good'} />
      {report.data ? (
        <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
          <StatCard label="Cobranças" value={String(report.data.count)} />
          <StatCard label="Valor original" value={money(report.data.originalAmount)} />
          <StatCard label="Encargos" value={money(report.data.chargesAmount)} tone="bad" />
          <StatCard label="Recebido" value={money(report.data.amountPaid)} tone="good" />
          <StatCard label="Saldo" value={money(report.data.balance)} tone="accent" />
        </div>
      ) : null}
      <div className="mt-6">
        <DataTable
          columns={columns}
          rows={rows}
          rowKey={(row) => row.id}
          isLoading={report.isPending}
          error={report.isError ? report.error.message : undefined}
          emptyMessage="Nenhuma linha para estes filtros."
          footer={hidden > 0 ? <p className="text-xs text-ink/50">Prévia das {PREVIEW} primeiras linhas. Mais {hidden} linha(s) no arquivo Excel.</p> : rows && rows.length > 0 ? <p className="text-xs text-ink/50">{rows.length} linha(s)</p> : null}
        />
      </div>
    </div>
  )
}
