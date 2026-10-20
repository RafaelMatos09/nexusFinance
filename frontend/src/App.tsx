import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import { AppLayout } from './layouts/AppLayout.tsx'
import { HomePage } from './pages/HomePage.tsx'

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [{ index: true, element: <HomePage /> }],
  },
])

export function App() {
  return <RouterProvider router={router} />
}
