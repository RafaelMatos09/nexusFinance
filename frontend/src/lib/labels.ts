export const clientStatusLabel: Record<string, string> = {
  Active: 'Ativo',
  Inactive: 'Inativo',
}

export const contractStatusLabel: Record<string, string> = {
  Draft: 'Rascunho',
  Active: 'Ativo',
  Closed: 'Encerrado',
  Cancelled: 'Cancelado',
}

export const frequencyLabel: Record<string, string> = {
  Monthly: 'Mensal',
  Quarterly: 'Trimestral',
  Yearly: 'Anual',
}

export const chargeStatusLabel: Record<string, string> = {
  Pending: 'Pendente',
  Paid: 'Paga',
  Overdue: 'Vencida',
  Cancelled: 'Cancelada',
}

export const paymentMethodLabel: Record<string, string> = {
  Pix: 'Pix',
  Boleto: 'Boleto',
  Transfer: 'Transferência',
  Cash: 'Dinheiro',
  Card: 'Cartão',
}

export function statusClass(status: string) {
  if (status === 'Active' || status === 'Paid') return 'bg-moss/10 text-moss'
  if (status === 'Overdue') return 'bg-copper/10 text-copper'
  if (status === 'Cancelled' || status === 'Inactive') return 'bg-ink/8 text-ink/60'
  if (status === 'Pending') return 'bg-gold/15 text-gold'
  if (status === 'Closed') return 'bg-sky/10 text-sky'
  return 'bg-line text-ink/80'
}

export function statusDotClass(status: string) {
  if (status === 'Active' || status === 'Paid') return 'bg-moss'
  if (status === 'Overdue') return 'bg-copper'
  if (status === 'Pending') return 'bg-gold'
  if (status === 'Closed') return 'bg-sky'
  return 'bg-ink/40'
}

export const statusColor: Record<string, string> = {
  Pending: '#b8891c',
  Paid: '#1f6b4a',
  Overdue: '#c4622d',
  Cancelled: '#8a8f8b',
}
