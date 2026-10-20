import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom'
import { AppLayout } from './layouts/AppLayout.tsx'
import { getToken } from './lib/api.ts'
import { ClienteDetalhePage } from './pages/ClienteDetalhePage.tsx'
import { ClientesPage } from './pages/ClientesPage.tsx'
import { CobrancasPage } from './pages/CobrancasPage.tsx'
import { ContratosPage } from './pages/ContratosPage.tsx'
import { DashboardPage } from './pages/DashboardPage.tsx'
import { LoginPage } from './pages/LoginPage.tsx'
import { RelatoriosPage } from './pages/RelatoriosPage.tsx'

function RequireAuth() {
  if (!getToken()) return <Navigate to="/login" replace />
  return <AppLayout />
}

const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    path: '/',
    element: <RequireAuth />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'clientes', element: <ClientesPage /> },
      { path: 'clientes/:id', element: <ClienteDetalhePage /> },
      { path: 'contratos', element: <ContratosPage /> },
      { path: 'cobrancas', element: <CobrancasPage /> },
      { path: 'relatorios', element: <RelatoriosPage /> },
    ],
  },
])

export function App() {
  return <RouterProvider router={router} />
}
