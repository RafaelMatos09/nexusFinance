import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useFormik } from 'formik'
import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import * as yup from 'yup'
import { Badge, Button, DataTable, ErrorText, Field, inputClass, LoadingBlock, Modal, Panel, StatCard, type Column } from '../components/ui.tsx'
import { ApiError, api, queryString, type Charge, type Client, type Contract, type Paged } from '../lib/api.ts'
import { dateLabel, formatDocument, initials, money } from '../lib/format.ts'
import { chargeStatusLabel, clientStatusLabel, contractStatusLabel, frequencyLabel } from '../lib/labels.ts'

const schema = yup.object({
  description: yup.string().required('Informe a descrição.'),
  amount: yup.number().moreThan(0, 'Informe um valor maior que zero.').required(),
  frequency: yup.string().required(),
  startDate: yup.string().required('Informe a data inicial.'),
  endDate: yup.string().test('end', 'A data final não pode ser anterior à inicial.', function validate(value) {
    return !value || value >= this.parent.startDate
  }),
})

export function ClienteDetalhePage() {
  const { id } = useParams()
  const queryClient = useQueryClient()
  const [message, setMessage] = useState('')
  const [open, setOpen] = useState(false)
  const client = useQuery({ queryKey: ['client', id], queryFn: () => api<Client>(`/api/clients/${id}`) })
  const contracts = useQuery({ queryKey: ['client-contracts', id], queryFn: () => api<Contract[]>(`/api/clients/${id}/contracts`) })
  const charges = useQuery({ queryKey: ['client-charges', id], queryFn: () => api<Paged<Charge>>(`/api/charges${queryString({ clientId: id, pageSize: 50 })}`) })

  const form = useFormik({
    initialValues: { description: '', amount: 1000, frequency: 'Monthly', startDate: new Date().toISOString().slice(0, 10), endDate: '' },
    validationSchema: schema,
    onSubmit: async (values, helpers) => {
      setMessage('')
      try {
        await api('/api/contracts', {
          method: 'POST',
          body: JSON.stringify({ clientId: Number(id), description: values.description, amount: Number(values.amount), frequency: values.frequency, startDate: values.startDate, endDate: values.endDate || null }),
        })
        helpers.resetForm()
        setOpen(false)
        await queryClient.invalidateQueries({ queryKey: ['client-contracts', id] })
      } catch (error) {
        setMessage(error instanceof ApiError ? error.message : 'Falha ao criar contrato.')
      }
    },
  })

  async function changeStatus(contract: Contract, status: string) {
    if (status === 'Cancelled' && !window.confirm(`Cancelar o contrato "${contract.description}"?`)) return
    try {
      await api(`/api/contracts/${contract.id}/status`, { method: 'POST', body: JSON.stringify({ status }) })
      await Promise.all([queryClient.invalidateQueries({ queryKey: ['client-contracts', id] }), queryClient.invalidateQueries({ queryKey: ['client-charges', id] })])
    } catch (error) {
      window.alert(error instanceof Error ? error.message : 'Não foi possível alterar o status.')
    }
  }

  if (client.isPending) return <LoadingBlock className="h-64" />
  if (client.isError) return <ErrorText message={client.error.message} />

  const open_ = charges.data?.items.filter((charge) => charge.status === 'Pending' || charge.status === 'Overdue') ?? []
  const balance = open_.reduce((sum, charge) => sum + charge.balance, 0)
  const overdue = open_.filter((charge) => charge.status === 'Overdue').reduce((sum, charge) => sum + charge.balance, 0)
  const received = charges.data?.items.reduce((sum, charge) => sum + charge.amountPaid, 0) ?? 0

  const contractColumns: Column<Contract>[] = [
    { key: 'description', header: 'Contrato', cell: (contract) => <span><span className="block font-medium">{contract.description}</span><span className="block text-xs text-ink/50">{frequencyLabel[contract.frequency]} · #{contract.id}</span></span> },
    { key: 'amount', header: 'Valor', align: 'right', cell: (contract) => <span className="tabular-nums">{money(contract.amount)}</span> },
    { key: 'period', header: 'Vigência', cell: (contract) => <span className="tabular-nums">{dateLabel(contract.startDate)} → {contract.endDate ? dateLabel(contract.endDate) : 'indeterminado'}</span> },
    { key: 'status', header: 'Status', cell: (contract) => <Badge status={contract.status} label={contractStatusLabel[contract.status] ?? contract.status} /> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (contract) => (
        <div className="flex justify-end gap-1">
          {contract.status === 'Draft' ? <Button variant="ghost" size="sm" className="text-moss" onClick={() => changeStatus(contract, 'Active')}>Ativar</Button> : null}
          {contract.status === 'Active' ? <Button variant="ghost" size="sm" onClick={() => changeStatus(contract, 'Closed')}>Encerrar</Button> : null}
          {contract.status === 'Draft' || contract.status === 'Active' ? <Button variant="danger" size="sm" onClick={() => changeStatus(contract, 'Cancelled')}>Cancelar</Button> : null}
        </div>
      ),
    },
  ]

  const chargeColumns: Column<Charge>[] = [
    { key: 'due', header: 'Vencimento', cell: (charge) => <span className="tabular-nums">{dateLabel(charge.dueDate)}</span> },
    { key: 'contract', header: 'Contrato', cell: (charge) => <span className="text-ink/70">{charge.contractDescription}</span> },
    { key: 'updated', header: 'Atualizado', align: 'right', cell: (charge) => <span className="tabular-nums">{money(charge.updatedAmount)}</span> },
    { key: 'balance', header: 'Saldo', align: 'right', cell: (charge) => <span className="font-semibold tabular-nums">{money(charge.balance)}</span> },
    { key: 'status', header: 'Status', cell: (charge) => <Badge status={charge.status} label={chargeStatusLabel[charge.status] ?? charge.status} /> },
  ]

  return (
    <div className="space-y-6">
      <Link className="inline-flex items-center gap-1 text-sm text-ink/60 hover:text-ink" to="/clientes">← Clientes</Link>
      <header className="rise flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <span className="grid h-14 w-14 place-items-center rounded-2xl bg-moss/10 font-display text-xl text-moss">{initials(client.data.name)}</span>
          <div>
            <div className="flex items-center gap-3">
              <h1 className="font-display text-3xl leading-none">{client.data.name}</h1>
              <Badge status={client.data.status} label={clientStatusLabel[client.data.status] ?? client.data.status} />
            </div>
            <p className="mt-2 text-sm text-ink/60">
              {formatDocument(client.data.document)} · {client.data.email}{client.data.phone ? ` · ${client.data.phone}` : ''}
            </p>
          </div>
        </div>
        <Button onClick={() => setOpen(true)} disabled={client.data.status !== 'Active'}>Novo contrato</Button>
      </header>

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Saldo em aberto" value={money(balance)} hint={`${open_.length} cobrança(s)`} />
        <StatCard label="Vencido" value={money(overdue)} tone="bad" />
        <StatCard label="Recebido" value={money(received)} tone="good" />
      </div>

      <Panel title="Contratos" subtitle="Somente rascunhos podem ser alterados; ativar gera as cobranças.">
        <DataTable columns={contractColumns} rows={contracts.data} rowKey={(contract) => contract.id} isLoading={contracts.isPending} error={contracts.isError ? contracts.error.message : undefined} emptyMessage="Este cliente ainda não tem contratos." />
      </Panel>

      <Panel title="Cobranças" subtitle="Histórico completo do cliente" actions={<Link to={`/cobrancas`} className="text-sm text-ink/60 hover:text-ink">Ver todas →</Link>}>
        <DataTable columns={chargeColumns} rows={charges.data?.items} rowKey={(charge) => charge.id} isLoading={charges.isPending} error={charges.isError ? charges.error.message : undefined} emptyMessage="Nenhuma cobrança gerada." />
      </Panel>

      <Modal open={open} title="Novo contrato" description="O contrato nasce como rascunho." onOpenChange={setOpen}>
        <form className="grid gap-3 sm:grid-cols-2" onSubmit={form.handleSubmit}>
          <div className="sm:col-span-2">
            <Field label="Descrição" error={form.touched.description ? form.errors.description : undefined}>
              <input className={inputClass} name="description" value={form.values.description} onChange={form.handleChange} onBlur={form.handleBlur} />
            </Field>
          </div>
          <Field label="Valor" error={form.touched.amount ? form.errors.amount : undefined}>
            <input className={inputClass} name="amount" type="number" step="0.01" value={form.values.amount} onChange={form.handleChange} onBlur={form.handleBlur} />
          </Field>
          <Field label="Periodicidade">
            <select className={inputClass} name="frequency" value={form.values.frequency} onChange={form.handleChange}>
              {Object.entries(frequencyLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </Field>
          <Field label="Início" error={form.touched.startDate ? form.errors.startDate : undefined}>
            <input className={inputClass} name="startDate" type="date" value={form.values.startDate} onChange={form.handleChange} onBlur={form.handleBlur} />
          </Field>
          <Field label="Fim" error={form.touched.endDate ? form.errors.endDate : undefined} hint="Opcional">
            <input className={inputClass} name="endDate" type="date" value={form.values.endDate} onChange={form.handleChange} onBlur={form.handleBlur} />
          </Field>
          {message ? <p className="text-sm text-copper sm:col-span-2">{message}</p> : null}
          <div className="flex justify-end gap-2 pt-2 sm:col-span-2">
            <Button variant="secondary" onClick={() => setOpen(false)}>Cancelar</Button>
            <Button type="submit" disabled={form.isSubmitting}>Criar contrato</Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
