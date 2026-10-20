import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Badge, Button, DataTable, Feedback, inputClass, PageHeader, Pagination, Toolbar, type Column } from '../components/ui.tsx'
import { ApiError, api, queryString, type Client, type Contract, type Paged } from '../lib/api.ts'
import { dateLabel, money } from '../lib/format.ts'
import { contractStatusLabel, frequencyLabel } from '../lib/labels.ts'

export function ContratosPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const [clientId, setClientId] = useState('')
  const [status, setStatus] = useState('')
  const [message, setMessage] = useState('')
  const options = useQuery({ queryKey: ['client-options'], queryFn: () => api<Client[]>('/api/clients/options') })
  const contracts = useQuery({
    queryKey: ['contracts', page, clientId, status],
    queryFn: () => api<Paged<Contract>>(`/api/contracts${queryString({ page, pageSize: 10, clientId, status })}`),
  })

  async function changeStatus(contract: Contract, next: string) {
    const label = contractStatusLabel[next]?.toLowerCase()
    if (next === 'Cancelled' && !window.confirm(`Cancelar o contrato "${contract.description}"? Cobranças em aberto sem pagamento serão canceladas.`)) return
    setMessage('')
    try {
      await api(`/api/contracts/${contract.id}/status`, { method: 'POST', body: JSON.stringify({ status: next }) })
      await queryClient.invalidateQueries({ queryKey: ['contracts'] })
      setMessage(next === 'Active' ? 'Contrato ativado e cobranças geradas.' : `Contrato ${label}.`)
    } catch (error) {
      setMessage(error instanceof ApiError ? error.message : 'Não foi possível alterar o status.')
    }
  }

  const columns: Column<Contract>[] = [
    {
      key: 'client',
      header: 'Cliente',
      cell: (contract) => <Link className="font-medium hover:text-copper" to={`/clientes/${contract.clientId}`}>{contract.clientName}</Link>,
    },
    {
      key: 'description',
      header: 'Contrato',
      cell: (contract) => (
        <span>
          <span className="block">{contract.description}</span>
          <span className="block text-xs text-ink/50">{frequencyLabel[contract.frequency]} · #{contract.id}</span>
        </span>
      ),
    },
    { key: 'amount', header: 'Valor', align: 'right', cell: (contract) => <span className="font-medium tabular-nums">{money(contract.amount)}</span> },
    {
      key: 'period',
      header: 'Vigência',
      cell: (contract) => (
        <span className="tabular-nums">
          {dateLabel(contract.startDate)} <span className="text-ink/40">→</span> {contract.endDate ? dateLabel(contract.endDate) : <span className="text-ink/50">indeterminado</span>}
        </span>
      ),
    },
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

  return (
    <div>
      <PageHeader eyebrow="Contratos" title="Acordos" description="Ativar um contrato gera as cobranças do período. Rascunhos podem ser editados no detalhe do cliente." />
      <Toolbar>
        <select className={`${inputClass} max-w-xs`} value={clientId} onChange={(event) => { setClientId(event.target.value); setPage(1) }}>
          <option value="">Todos os clientes</option>
          {options.data?.map((client) => <option key={client.id} value={client.id}>{client.name}</option>)}
        </select>
        <select className={`${inputClass} max-w-[12rem]`} value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">Todos os status</option>
          {Object.entries(contractStatusLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        {contracts.data ? <span className="ml-auto text-xs text-ink/50">{contracts.data.total} contrato(s)</span> : null}
      </Toolbar>
      <Feedback message={message} tone={message.startsWith('Não') ? 'bad' : 'good'} />
      <div className="mt-4">
        <DataTable
          columns={columns}
          rows={contracts.data?.items}
          rowKey={(contract) => contract.id}
          isLoading={contracts.isPending}
          error={contracts.isError ? contracts.error.message : undefined}
          emptyMessage="Nenhum contrato com esses filtros."
          footer={contracts.data ? <Pagination page={contracts.data.page} pageSize={contracts.data.pageSize} total={contracts.data.total} onPage={setPage} /> : null}
        />
      </div>
    </div>
  )
}
