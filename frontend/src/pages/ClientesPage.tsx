import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useFormik } from 'formik'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import * as yup from 'yup'
import { Badge, Button, DataTable, Field, inputClass, Modal, PageHeader, Pagination, Toolbar, type Column } from '../components/ui.tsx'
import { ApiError, api, queryString, type Client, type Paged } from '../lib/api.ts'
import { isValidDocument, onlyDigits } from '../lib/document.ts'
import { dateLabel, formatDocument, initials } from '../lib/format.ts'
import { clientStatusLabel } from '../lib/labels.ts'

const schema = yup.object({
  name: yup.string().required('Informe o nome.'),
  document: yup.string().required('Informe o documento.').test('doc', 'CPF ou CNPJ inválido.', (value) => isValidDocument(value ?? '')),
  email: yup.string().email('E-mail inválido.').required('Informe o e-mail.'),
  phone: yup.string().max(20),
})

export function ClientesPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<Client | null>(null)
  const [message, setMessage] = useState('')

  const clients = useQuery({
    queryKey: ['clients', page, search, status],
    queryFn: () => api<Paged<Client>>(`/api/clients${queryString({ page, pageSize: 10, search, status })}`),
  })

  const form = useFormik({
    enableReinitialize: true,
    initialValues: { name: editing?.name ?? '', document: editing?.document ?? '', email: editing?.email ?? '', phone: editing?.phone ?? '' },
    validationSchema: schema,
    onSubmit: async (values, helpers) => {
      setMessage('')
      const body = JSON.stringify({ ...values, document: onlyDigits(values.document), phone: values.phone || null })
      try {
        if (editing) await api(`/api/clients/${editing.id}`, { method: 'PUT', body })
        else await api('/api/clients', { method: 'POST', body })
        setOpen(false)
        await queryClient.invalidateQueries({ queryKey: ['clients'] })
      } catch (error) {
        if (error instanceof ApiError && error.errors) {
          for (const [field, messages] of Object.entries(error.errors)) helpers.setFieldError(field, messages[0])
        }
        setMessage(error instanceof Error ? error.message : 'Falha ao salvar.')
      }
    },
  })

  async function inactivate(client: Client) {
    if (!window.confirm(`Inativar ${client.name}? Contratos novos não poderão ser criados.`)) return
    await api(`/api/clients/${client.id}/inactivate`, { method: 'POST' })
    await queryClient.invalidateQueries({ queryKey: ['clients'] })
  }

  const columns: Column<Client>[] = [
    {
      key: 'name',
      header: 'Cliente',
      cell: (client) => (
        <Link to={`/clientes/${client.id}`} className="flex items-center gap-3">
          <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-moss/10 text-xs font-semibold text-moss">{initials(client.name)}</span>
          <span>
            <span className="block font-medium hover:text-copper">{client.name}</span>
            <span className="block text-xs text-ink/50">{client.email}</span>
          </span>
        </Link>
      ),
    },
    { key: 'document', header: 'Documento', cell: (client) => <span className="tabular-nums">{formatDocument(client.document)}</span> },
    { key: 'phone', header: 'Telefone', cell: (client) => client.phone || <span className="text-ink/40">—</span> },
    { key: 'since', header: 'Cliente desde', cell: (client) => dateLabel(client.createdAt.slice(0, 10)) },
    { key: 'status', header: 'Status', cell: (client) => <Badge status={client.status} label={clientStatusLabel[client.status] ?? client.status} /> },
    {
      key: 'actions',
      header: '',
      align: 'right',
      cell: (client) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" size="sm" onClick={() => { setEditing(client); setOpen(true) }}>Editar</Button>
          {client.status === 'Active' ? <Button variant="danger" size="sm" onClick={() => inactivate(client)}>Inativar</Button> : null}
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader
        eyebrow="Clientes"
        title="Carteira"
        description="Cadastro de pessoas físicas e jurídicas. Clientes inativos permanecem no histórico."
        actions={<Button onClick={() => { setEditing(null); setOpen(true) }}>Novo cliente</Button>}
      />
      <Toolbar>
        <input className={`${inputClass} max-w-xs`} placeholder="Buscar por nome ou documento" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} />
        <select className={`${inputClass} max-w-[10rem]`} value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">Todos</option>
          <option value="Active">Ativos</option>
          <option value="Inactive">Inativos</option>
        </select>
        {clients.data ? <span className="ml-auto text-xs text-ink/50">{clients.data.total} cliente(s)</span> : null}
      </Toolbar>
      <div className="mt-4">
        <DataTable
          columns={columns}
          rows={clients.data?.items}
          rowKey={(client) => client.id}
          isLoading={clients.isPending}
          error={clients.isError ? clients.error.message : undefined}
          emptyMessage="Nenhum cliente encontrado com esses filtros."
          footer={clients.data ? <Pagination page={clients.data.page} pageSize={clients.data.pageSize} total={clients.data.total} onPage={setPage} /> : null}
        />
      </div>
      <Modal open={open} title={editing ? 'Editar cliente' : 'Novo cliente'} description="CPF ou CNPJ válido, único por cliente." onOpenChange={setOpen}>
        <form className="space-y-3" onSubmit={form.handleSubmit}>
          <Field label="Nome ou razão social" error={form.touched.name ? form.errors.name : undefined}>
            <input className={inputClass} name="name" value={form.values.name} onChange={form.handleChange} onBlur={form.handleBlur} />
          </Field>
          <Field label="CPF ou CNPJ" error={form.touched.document ? form.errors.document : undefined}>
            <input className={inputClass} name="document" value={form.values.document} onChange={form.handleChange} onBlur={form.handleBlur} />
          </Field>
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="E-mail" error={form.touched.email ? form.errors.email : undefined}>
              <input className={inputClass} name="email" value={form.values.email} onChange={form.handleChange} onBlur={form.handleBlur} />
            </Field>
            <Field label="Telefone" error={form.touched.phone ? form.errors.phone : undefined}>
              <input className={inputClass} name="phone" value={form.values.phone} onChange={form.handleChange} onBlur={form.handleBlur} />
            </Field>
          </div>
          {message ? <p className="text-sm text-copper">{message}</p> : null}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setOpen(false)}>Cancelar</Button>
            <Button type="submit" disabled={form.isSubmitting}>{form.isSubmitting ? 'Salvando…' : 'Salvar'}</Button>
          </div>
        </form>
      </Modal>
    </div>
  )
}
