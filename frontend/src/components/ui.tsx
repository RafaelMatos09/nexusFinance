import * as Dialog from '@radix-ui/react-dialog'
import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { statusClass, statusDotClass } from '../lib/labels.ts'

export function Field({ label, error, hint, children }: { label: string; error?: string; hint?: string; children: ReactNode }) {
  return (
    <label className="block text-sm">
      <span className="font-medium text-ink/80">{label}</span>
      <div className="mt-1">{children}</div>
      {error ? <span className="mt-1 block text-xs text-copper">{error}</span> : hint ? <span className="mt-1 block text-xs text-ink/50">{hint}</span> : null}
    </label>
  )
}

export const inputClass =
  'w-full rounded-xl border border-line bg-white px-3 py-2 text-sm text-ink outline-none transition focus:border-moss focus:ring-2 focus:ring-moss/15 disabled:bg-paper'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger'

const buttonVariants: Record<ButtonVariant, string> = {
  primary: 'bg-ink text-paper hover:bg-ink/90 shadow-sm',
  secondary: 'border border-line bg-white text-ink hover:border-ink/30',
  ghost: 'text-ink/70 hover:bg-ink/5 hover:text-ink',
  danger: 'text-copper hover:bg-copper/10',
}

export function Button({ variant = 'primary', size = 'md', className = '', ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: ButtonVariant; size?: 'sm' | 'md' }) {
  const sizing = size === 'sm' ? 'px-2.5 py-1 text-xs' : 'px-4 py-2 text-sm'
  return (
    <button
      type="button"
      className={`inline-flex items-center justify-center gap-2 rounded-xl font-medium transition disabled:opacity-50 ${sizing} ${buttonVariants[variant]} ${className}`}
      {...props}
    />
  )
}

export function Badge({ status, label }: { status: string; label: string }) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ${statusClass(status)}`}>
      <span className={`h-1.5 w-1.5 rounded-full ${statusDotClass(status)}`} />
      {label}
    </span>
  )
}

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow: string; title: string; description?: string; actions?: ReactNode }) {
  return (
    <div className="rise flex flex-wrap items-end justify-between gap-4">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-copper">{eyebrow}</p>
        <h1 className="mt-1 font-display text-4xl leading-tight">{title}</h1>
        {description ? <p className="mt-2 max-w-xl text-sm text-ink/60">{description}</p> : null}
      </div>
      {actions ? <div className="flex flex-wrap gap-2">{actions}</div> : null}
    </div>
  )
}

export function Toolbar({ children }: { children: ReactNode }) {
  return <div className="mt-6 flex flex-wrap items-center gap-3 rounded-2xl border border-line bg-card/70 p-3">{children}</div>
}

export function StatCard({ label, value, hint, tone = 'default', icon }: { label: string; value: string; hint?: string; tone?: 'default' | 'good' | 'bad' | 'accent'; icon?: ReactNode }) {
  const accent = tone === 'good' ? 'text-moss' : tone === 'bad' ? 'text-copper' : tone === 'accent' ? 'text-gold' : 'text-ink'
  return (
    <article className="rise relative overflow-hidden rounded-2xl border border-line bg-card p-5 shadow-card">
      <div className="flex items-start justify-between gap-3">
        <p className="text-xs font-medium uppercase tracking-wide text-ink/50">{label}</p>
        {icon ? <span className={`rounded-lg bg-ink/5 p-1.5 ${accent}`}>{icon}</span> : null}
      </div>
      <p className={`mt-3 font-display text-3xl tabular-nums ${accent}`}>{value}</p>
      {hint ? <p className="mt-1 text-xs text-ink/50">{hint}</p> : null}
    </article>
  )
}

export function Panel({ title, subtitle, actions, children, className = '' }: { title?: string; subtitle?: string; actions?: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={`rise rounded-2xl border border-line bg-card p-5 shadow-card ${className}`}>
      {title ? (
        <header className="flex flex-wrap items-start justify-between gap-2">
          <div>
            <h2 className="font-display text-xl">{title}</h2>
            {subtitle ? <p className="mt-0.5 text-xs text-ink/50">{subtitle}</p> : null}
          </div>
          {actions}
        </header>
      ) : null}
      <div className={title ? 'mt-4' : ''}>{children}</div>
    </section>
  )
}

export type Column<T> = {
  key: string
  header: ReactNode
  cell: (row: T) => ReactNode
  align?: 'left' | 'right' | 'center'
  width?: string
}

export function DataTable<T>({
  columns,
  rows,
  rowKey,
  isLoading,
  error,
  emptyMessage = 'Nenhum registro encontrado.',
  footer,
}: {
  columns: Column<T>[]
  rows: T[] | undefined
  rowKey: (row: T) => string | number
  isLoading?: boolean
  error?: string
  emptyMessage?: string
  footer?: ReactNode
}) {
  const alignClass = (align?: Column<T>['align']) => (align === 'right' ? 'text-right' : align === 'center' ? 'text-center' : 'text-left')
  return (
    <div className="rise overflow-hidden rounded-2xl border border-line bg-card shadow-card">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-ink/[0.03] text-xs uppercase tracking-wide text-ink/50">
            <tr>
              {columns.map((column) => (
                <th key={column.key} scope="col" className={`px-4 py-3 font-semibold ${alignClass(column.align)}`} style={column.width ? { width: column.width } : undefined}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading
              ? Array.from({ length: 5 }).map((_, index) => (
                  <tr key={`skeleton-${index}`} className="border-t border-line">
                    {columns.map((column) => (
                      <td key={column.key} className="px-4 py-3.5">
                        <div className="h-3.5 animate-pulse rounded bg-line/80" style={{ width: `${55 + ((index * 17) % 40)}%` }} />
                      </td>
                    ))}
                  </tr>
                ))
              : null}
            {!isLoading && error ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-8">
                  <ErrorText message={error} />
                </td>
              </tr>
            ) : null}
            {!isLoading && !error && rows && rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-12 text-center">
                  <EmptyText message={emptyMessage} />
                </td>
              </tr>
            ) : null}
            {!isLoading && !error
              ? rows?.map((row) => (
                  <tr key={rowKey(row)} className="border-t border-line transition hover:bg-ink/[0.025]">
                    {columns.map((column) => (
                      <td key={column.key} className={`px-4 py-3 align-middle ${alignClass(column.align)}`}>
                        {column.cell(row)}
                      </td>
                    ))}
                  </tr>
                ))
              : null}
          </tbody>
        </table>
      </div>
      {footer ? <div className="border-t border-line px-4 py-3">{footer}</div> : null}
    </div>
  )
}

export function Pagination({ page, pageSize, total, onPage }: { page: number; pageSize: number; total: number; onPage: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(total / pageSize))
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1
  const end = Math.min(total, page * pageSize)
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-ink/60">
      <span>
        {start}–{end} de {total}
      </span>
      <div className="flex items-center gap-2">
        <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => onPage(page - 1)}>
          Anterior
        </Button>
        <span className="tabular-nums">
          {page} / {pages}
        </span>
        <Button variant="secondary" size="sm" disabled={page >= pages} onClick={() => onPage(page + 1)}>
          Próxima
        </Button>
      </div>
    </div>
  )
}

export function Modal({ open, title, description, onOpenChange, children }: { open: boolean; title: string; description?: string; onOpenChange: (open: boolean) => void; children: ReactNode }) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-ink/50 backdrop-blur-[2px]" />
        <Dialog.Content className="rise fixed left-1/2 top-1/2 max-h-[90vh] w-[min(36rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 overflow-auto rounded-2xl border border-line bg-card p-6 shadow-2xl">
          <div className="flex items-start justify-between gap-4">
            <div>
              <Dialog.Title className="font-display text-2xl">{title}</Dialog.Title>
              {description ? <Dialog.Description className="mt-1 text-sm text-ink/60">{description}</Dialog.Description> : null}
            </div>
            <Dialog.Close className="rounded-lg p-1 text-ink/50 hover:bg-ink/5 hover:text-ink" aria-label="Fechar">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M6 6l12 12M18 6L6 18" /></svg>
            </Dialog.Close>
          </div>
          <div className="mt-5">{children}</div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

export function LoadingBlock({ className = 'h-28' }: { className?: string }) {
  return <div className={`${className} animate-pulse rounded-2xl bg-line/70`} aria-busy="true" />
}

export function ErrorText({ message }: { message: string }) {
  return (
    <p className="rounded-xl border border-copper/20 bg-copper/10 px-4 py-3 text-sm text-copper" role="alert">
      {message}
    </p>
  )
}

export function EmptyText({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center gap-2 text-sm text-ink/55">
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" className="text-ink/30"><rect x="3" y="5" width="18" height="14" rx="2" /><path d="M3 10h18M8 15h4" /></svg>
      {message}
    </div>
  )
}

export function Feedback({ message, tone = 'good' }: { message: string; tone?: 'good' | 'bad' }) {
  if (!message) return null
  return <p className={`mt-3 text-sm ${tone === 'good' ? 'text-moss' : 'text-copper'}`}>{message}</p>
}
