export type HealthStatus = {
  status: string
  database: string
}

export async function fetchHealth(): Promise<HealthStatus> {
  const response = await fetch('/api/health')

  if (response.status === 503) {
    throw new Error('A API está no ar, mas o banco não respondeu.')
  }

  if (!response.ok) {
    throw new Error('Não foi possível consultar a API.')
  }

  return (await response.json()) as HealthStatus
}
