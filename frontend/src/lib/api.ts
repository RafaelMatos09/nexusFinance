const TOKEN_KEY = 'nexus.token'
const USER_KEY = 'nexus.user'

export type SessionUser = { name: string; email: string }
export type Paged<T> = { items: T[]; page: number; pageSize: number; total: number }

export type Client = {
  id: number
  name: string
  document: string
  email: string
  phone: string | null
  status: string
  createdAt: string
}

export type Contract = {
  id: number
  clientId: number
  clientName: string
  description: string
  amount: number
  frequency: string
  startDate: string
  endDate: string | null
  status: string
}

export type Charge = {
  id: number
  contractId: number
  clientId: number
  clientName: string
  contractDescription: string
  originalAmount: number
  dueDate: string
  status: string
  notes: string | null
  daysLate: number
  fine: number
  interest: number
  updatedAmount: number
  amountPaid: number
  balance: number
}

export type Payment = {
  id: number
  chargeId: number
  amount: number
  paidOn: string
  method: string
}

export type Dashboard = {
  totalReceivable: number
  receivedThisMonth: number
  overdue: number
  delinquencyRate: number
  openCharges: number
  overdueCharges: number
  averageTicket: number
  receiptsByMonth: { year: number; month: number; amount: number }[]
  aging: { label: string; amount: number }[]
  statusBreakdown: { status: string; count: number; amount: number }[]
  topDebtors: { clientId: number; clientName: string; amount: number; chargeCount: number }[]
}

export type ChargeReport = {
  count: number
  originalAmount: number
  chargesAmount: number
  updatedAmount: number
  amountPaid: number
  balance: number
  rows: Charge[]
}

export class ApiError extends Error {
  status: number
  errors?: Record<string, string[]>

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

export function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function getUser(): SessionUser | null {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) return null
  return JSON.parse(raw) as SessionUser
}

export function saveSession(token: string, user: SessionUser) {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(USER_KEY, JSON.stringify(user))
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}

function camel(key: string) {
  return key ? key.charAt(0).toLowerCase() + key.slice(1) : key
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (options.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const token = getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(path, { ...options, headers })
  if (response.status === 401 && !path.endsWith('/auth/login')) {
    clearSession()
    if (window.location.pathname !== '/login') window.location.assign('/login')
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      detail?: string
      title?: string
      errors?: Record<string, string[]>
    } | null
    const errors = problem?.errors
      ? Object.fromEntries(Object.entries(problem.errors).map(([key, messages]) => [camel(key), messages]))
      : undefined
    throw new ApiError(response.status, problem?.detail || problem?.title || 'Não foi possível concluir a operação.', errors)
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('spreadsheet') || contentType.includes('octet-stream')) {
    return (await response.blob()) as T
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export function queryString(params: Record<string, string | number | undefined | null>) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}
