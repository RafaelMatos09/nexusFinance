import { useFormik } from 'formik'
import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import * as yup from 'yup'
import { ApiError, api, getToken, saveSession } from '../lib/api.ts'
import { Field, inputClass } from '../components/ui.tsx'

const schema = yup.object({
  email: yup.string().email('E-mail inválido.').required('Informe o e-mail.'),
  password: yup.string().required('Informe a senha.'),
})

export function LoginPage() {
  const navigate = useNavigate()
  const [formError, setFormError] = useState('')
  const form = useFormik({
    initialValues: { email: 'demo@nexusfinance.dev', password: 'Demo@123' },
    validationSchema: schema,
    onSubmit: async (values, helpers) => {
      setFormError('')
      try {
        const result = await api<{ token: string; name: string; email: string }>('/api/auth/login', {
          method: 'POST',
          body: JSON.stringify(values),
        })
        saveSession(result.token, { name: result.name, email: result.email })
        navigate('/')
      } catch (error) {
        if (error instanceof ApiError && error.errors) {
          for (const [field, messages] of Object.entries(error.errors)) {
            helpers.setFieldError(field, messages[0])
          }
        }
        setFormError(error instanceof Error ? error.message : 'Falha no login.')
      }
    },
  })

  if (getToken()) return <Navigate to="/" replace />

  return (
    <main className="grid min-h-screen lg:grid-cols-[1.1fr_0.9fr]">
      <section className="relative hidden flex-col justify-between overflow-hidden bg-ink px-12 py-10 text-paper lg:flex">
        <div className="pointer-events-none absolute -right-32 -top-32 h-96 w-96 rounded-full bg-copper/30 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-40 left-10 h-96 w-96 rounded-full bg-moss/40 blur-3xl" />
        <div className="relative flex items-center gap-3">
          <span className="grid h-10 w-10 place-items-center rounded-xl bg-copper font-display text-xl">N</span>
          <p className="font-display text-2xl">Nexus Finance</p>
        </div>
        <div className="relative">
          <h1 className="max-w-md font-display text-5xl leading-tight">Cobrança clara para quem vive de contrato.</h1>
          <p className="mt-4 max-w-sm text-paper/70">Clientes, contratos, recebimentos e inadimplência no mesmo painel.</p>
          <ul className="mt-8 grid max-w-md grid-cols-3 gap-3 text-sm">
            {[
              ['Multa e juros', 'calculados por dia de atraso'],
              ['Aging', 'em quatro faixas de atraso'],
              ['Excel', 'com resumo e dados brutos'],
            ].map(([title, text]) => (
              <li key={title} className="rounded-xl border border-white/10 bg-white/5 p-3">
                <p className="font-medium">{title}</p>
                <p className="mt-1 text-xs text-paper/60">{text}</p>
              </li>
            ))}
          </ul>
        </div>
        <p className="relative text-xs text-paper/50">Projeto de portfólio · .NET 8, Dapper, PostgreSQL, React e Tailwind · dados fictícios</p>
      </section>
      <section className="flex items-center justify-center bg-paper px-6 py-12">
        <form className="w-full max-w-sm" onSubmit={form.handleSubmit}>
          <p className="text-sm font-medium uppercase tracking-[0.16em] text-copper">Entrar</p>
          <h2 className="mt-2 font-display text-4xl">Bem-vinda de volta</h2>
          <div className="mt-8 space-y-4">
            <Field label="E-mail" error={form.touched.email ? form.errors.email : undefined}>
              <input className={inputClass} name="email" type="email" value={form.values.email} onChange={form.handleChange} onBlur={form.handleBlur} />
            </Field>
            <Field label="Senha" error={form.touched.password ? form.errors.password : undefined}>
              <input className={inputClass} name="password" type="password" value={form.values.password} onChange={form.handleChange} onBlur={form.handleBlur} />
            </Field>
          </div>
          {formError ? <p className="mt-4 text-sm text-copper" role="alert">{formError}</p> : null}
          <button type="submit" disabled={form.isSubmitting} className="mt-6 w-full rounded-xl bg-ink px-4 py-3 text-sm font-medium text-paper shadow-sm transition hover:bg-ink/90 disabled:opacity-60">
            {form.isSubmitting ? 'Entrando…' : 'Acessar o painel'}
          </button>
          <div className="mt-6 rounded-xl border border-dashed border-line bg-card p-3 text-xs text-ink/60">
            <p className="font-medium text-ink/80">Acesso de demonstração</p>
            <p className="mt-1 font-mono">demo@nexusfinance.dev · Demo@123</p>
          </div>
        </form>
      </section>
    </main>
  )
}
