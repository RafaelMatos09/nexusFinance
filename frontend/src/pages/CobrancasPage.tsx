import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useFormik } from 'formik'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import * as yup from 'yup'
import { Badge, Button, DataTable, EmptyText, ErrorText, Field, inputClass, LoadingBlock, Modal, PageHeader, Pagination, Toolbar, type Column } from '../components/ui.tsx'
import { ApiError, api, queryString, type Charge, type Client, type Paged, type Payment } from '../lib/api.ts'
import { dateLabel, money } from '../lib/format.ts'
import { chargeStatusLabel, paymentMethodLabel } from '../lib/labels.ts'

const schema = yup.object({
  amount: yup.number().moreThan(0, 'Informe um valor maior que zero.').required(),
  paidOn: yup.string().required('Informe a data.'),
  method: yup.string().required(),
})

export function CobrancasPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [clientId, setClientId] = useState('')
  const [status, setStatus] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [paymentCharge, setPaymentCharge] = useState<Charge | null>(null)
  const [historyCharge, setHistoryCharge] = useState<Charge | null>(null)
  const [message, setMessage] = useState('')
  const options = useQuery({ queryKey: ['client-options'], queryFn: () => api<Client[]>('/api/clients/options') })
  const charges = useQuery({
    queryKey: ['charges', page, clientId, status, from, to],
    queryFn: () => api<Paged<Charge>>(`/api/charges${queryString({ page, pageSize: 10, clientId, status, from, to })}`),
  })
  const history = useQuery({
    queryKey: ['payments', historyCharge?.id],
    enabled: historyCharge !== null,
    queryFn: () => api<Payment[]>(`/api/charges/${historyCharge?.id}/payments`),
  })
  const form = useFormik({
    enableReinitialize: true,
    initialValues: { amount: paymentCharge?.balance ?? 0, paidOn: new Date().toISOString().slice(0, 10), method: 'Pix' },
    validationSchema: schema,
    onSubmit: async (values) => {
      if (!paymentCharge) return
      setMessage('')
      try {
        await api(`/api/charges/${paymentCharge.id}/payments`, {
          method: 'POST',
          body: JSON.stringify({ amount: Number(values.amount), paidOn: values.paidOn, method: values.method }),
        })
        setPaymentCharge(null)
        await queryClient.invalidateQueries({ queryKey: ['charges'] })
        await queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      } catch (error) {
        setMessage(error instanceof ApiError ? error.message : 'Falha na baixa.')
      }
    },
  })

  async function cancel(charge: Charge) {
    if (!window.confirm(`Cancelar a cobrança de ${charge.clientName} com vencimento em ${dateLabel(charge.dueDate)}?`)) return
    try {
      await api(`/api/charges/${charge.id}/cancel`, { method: 'POST' })
      await queryClient.invalidateQueries({ queryKey: ['charges'] })
    } catch (error) {
      window.alert(error instanceof Error ? error.message : 'Não foi possível cancelar.')
    }
  }

  const isOpen = (charge: Charge) => charge.status !== 'Paid' && charge.status !== 'Cancelled'
  const partial = Number(form.values.amount) > 0 && paymentCharge !== null && Number(form.values.amount) < paymentCharge.balance

  const columns: Column<Charge>[] = [
    {
      key: 'client',
      header: 'Cliente',
      cell: (charge) => (
        <span>
          <Link className="block font-medium hover:text-copper" to={`/clientes/${charge.clientId}`}>{charge.clientName}</Link>
          <span className="block text-xs text-ink/50">{charge.contractDescription}</span>
        </span>
      ),
    },
    {
      key: 'due',
      header: 'Vencimento',
      cell: (charge) => (
        <span className="tabular-nums">
          {dateLabel(charge.dueDate)}
          {charge.daysLate > 0 && isOpen(charge) ? <span className="block text-xs text-copper">{charge.daysLate} dia(s) de atraso</span> : null}
        </span>
      ),
    },
    { key: 'original', header: 'Original', align: 'right', cell: (charge) => <span className="tabular-nums">{money(charge.originalAmount)}</span> },
    {
      key: 'fees',
      header: 'Encargos',
      align: 'right',
      cell: (charge) => (charge.fine + charge.interest > 0 ? <span className="tabular-nums text-copper">+{money(charge.fine + charge.interest)}</span> : <span className="text-ink/40">—</span>),
    },
    { key: 'paid', header: 'Pago', align: 'right', cell: (charge) => <span className="tabular-nums text-moss">{charge.amountPaid > 0 ? money(charge.amountPaid) : '—'}</span> },
    { key: 'balance', header: 'Saldo', align: 'right', cell: (charge) => <span className="font-semibold tabular-nums">{money(charge.balance)}</span> },
    { key: 'status', header: 'Status', cell: (charge) => <Badge status={charge.status} label={chargeStatusLabel[charge.status] ?? charge.status} /> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (charge) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" size="sm" onClick={() => setHistoryCharge(charge)}>Histórico</Button>
          {isOpen(charge) ? <Button variant="ghost" size="sm" className="text-moss" onClick={() => setPaymentCharge(charge)}>Baixar</Button> : null}
          {isOpen(charge) ? <Button variant="danger" size="sm" onClick={() => cancel(charge)}>Cancelar</Button> : null}
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader eyebrow="Cobranças" title="Contas a receber" description="Multa de 2% e juros de 1% ao mês são aplicados automaticamente sobre cobranças vencidas." />
      <Toolbar>
        <select className={`${inputClass} max-w-xs`} value={clientId} onChange={(event) => { setClientId(event.target.value); setPage(1) }}>
          <option value="">Todos os clientes</option>
          {options.data?.map((client) => <option key={client.id} value={client.id}>{client.name}</option>)}
        </select>
        <select className={`${inputClass} max-w-[10rem]`} value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">Todos</option>
          {Object.entries(chargeStatusLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <label className="flex items-center gap-2 text-xs text-ink/60">
          De
          <input className={inputClass} type="date" value={from} onChange={(event) => { setFrom(event.target.value); setPage(1) }} />
        </label>
        <label className="flex items-center gap-2 text-xs text-ink/60">
          Até
          <input className={inputClass} type="date" value={to} onChange={(event) => { setTo(event.target.value); setPage(1) }} />
        </label>
        {clientId || status || from || to ? <Button variant="ghost" size="sm" onClick={() => { setClientId(''); setStatus(''); setFrom(''); setTo(''); setPage(1) }}>Limpar</Button> : null}
      </Toolbar>
      <div className="mt-4">
        <DataTable
          columns={columns}
          rows={charges.data?.items}
          rowKey={(charge) => charge.id}
          isLoading={charges.isPending}
          error={charges.isError ? charges.error.message : undefined}
          emptyMessage="Nenhuma cobrança no filtro."
          footer={charges.data ? <Pagination page={charges.data.page} pageSize={charges.data.pageSize} total={charges.data.total} onPage={setPage} /> : null}
        />
      </div>

      <Modal open={paymentCharge !== null} title="Registrar baixa" description={paymentCharge ? `${paymentCharge.clientName} · vencimento ${dateLabel(paymentCharge.dueDate)}` : undefined} onOpenChange={(open) => { if (!open) setPaymentCharge(null) }}>
        {paymentCharge ? (
          <dl className="mb-4 grid grid-cols-3 gap-2 rounded-xl bg-ink/[0.04] p-3 text-xs">
            <div><dt className="text-ink/50">Atualizado</dt><dd className="mt-0.5 font-medium tabular-nums">{money(paymentCharge.updatedAmount)}</dd></div>
            <div><dt className="text-ink/50">Já pago</dt><dd className="mt-0.5 font-medium tabular-nums text-moss">{money(paymentCharge.amountPaid)}</dd></div>
            <div><dt className="text-ink/50">Saldo devido</dt><dd className="mt-0.5 font-semibold tabular-nums">{money(paymentCharge.balance)}</dd></div>
          </dl>
        ) : null}
        <form className="space-y-3" onSubmit={form.handleSubmit}>
          <Field label="Valor pago" error={form.touched.amount ? form.errors.amount : undefined} hint={partial ? 'Baixa parcial: a cobrança continua em aberto.' : 'Baixa total: a cobrança será marcada como paga.'}>
            <input className={inputClass} name="amount" type="number" step="0.01" value={form.values.amount} onChange={form.handleChange} onBlur={form.handleBlur} />
          </Field>
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="Data do pagamento" error={form.touched.paidOn ? form.errors.paidOn : undefined}>
              <input className={inputClass} name="paidOn" type="date" value={form.values.paidOn} onChange={form.handleChange} onBlur={form.handleBlur} />
            </Field>
            <Field label="Forma">
              <select className={inputClass} name="method" value={form.values.method} onChange={form.handleChange}>
                {Object.entries(paymentMethodLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </Field>
          </div>
          {message ? <p className="text-sm text-copper">{message}</p> : null}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setPaymentCharge(null)}>Cancelar</Button>
            <Button type="submit" disabled={form.isSubmitting}>Confirmar baixa</Button>
          </div>
        </form>
      </Modal>

      <Modal open={historyCharge !== null} title="Recebimentos" description={historyCharge ? `${historyCharge.clientName} · ${money(historyCharge.amountPaid)} recebido de ${money(historyCharge.updatedAmount)}` : undefined} onOpenChange={(open) => { if (!open) setHistoryCharge(null) }}>
        {history.isPending ? <LoadingBlock className="h-20" /> : null}
        {history.isError ? <ErrorText message={history.error.message} /> : null}
        {history.data && history.data.length === 0 ? <EmptyText message="Nenhum recebimento nesta cobrança." /> : null}
        {history.data && history.data.length > 0 ? (
          <ol className="relative ml-2 space-y-4 border-l border-line pl-5 text-sm">
            {history.data.map((payment) => (
              <li key={payment.id} className="relative">
                <span className="absolute -left-[1.6rem] top-1.5 h-2.5 w-2.5 rounded-full bg-moss ring-4 ring-card" />
                <div className="flex items-center justify-between gap-3">
                  <span>
                    <span className="block font-medium">{paymentMethodLabel[payment.method] ?? payment.method}</span>
                    <span className="block text-xs text-ink/50">{dateLabel(payment.paidOn)}</span>
                  </span>
                  <strong className="tabular-nums text-moss">{money(payment.amount)}</strong>
                </div>
              </li>
            ))}
          </ol>
        ) : null}
      </Modal>
    </div>
  )
}
