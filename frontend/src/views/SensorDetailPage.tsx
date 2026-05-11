import { useParams, useSearchParams, useNavigate, Navigate } from 'react-router-dom'
import SensorDetail from '../components/SensorDetail'
import { SensorType } from '../types'
import type { SensorResponse } from '../types'

export default function SensorDetailPage() {
  const { sensorId } = useParams<{ sensorId: string }>()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  const id = Number(sensorId)
  const type = searchParams.get('type') as SensorType | null

  if (!id || isNaN(id) || !type) {
    return <Navigate to="/dashboard" replace />
  }

  const sensor: SensorResponse = {
    id,
    type,
    sensorNumber: 0,
    description: null,
    isActive: true,
    createdAt: '',
  }

  return <SensorDetail sensor={sensor} onClose={() => navigate('/dashboard')} />
}
