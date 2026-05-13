import { useState, useEffect } from 'react'
import type { CapacityConfig } from '../types/models'
import {
    getCapacityConfig,
    updateCapacityConfig,
    resetCapacityConfig
} from '../api/capacityConfigApi'

export const useCapacityConfig = () => {
    const [config, setConfig] = useState<CapacityConfig | null>(null)
    const [loading, setLoading] = useState(true)  // ← set true HERE as initial state
    const [error, setError] = useState<string | null>(null)

    // READ on mount
    useEffect(() => {
        let cancelled = false  // ← prevents state update if component unmounts

        getCapacityConfig()
            .then(data => {
                if (!cancelled) {
                    setConfig(data)
                    setLoading(false)  // ← only called after async completes
                }
            })
            .catch(e => {
                if (!cancelled) {
                    setError(e.message)
                    setLoading(false)
                }
            })

        return () => {
            cancelled = true  // ← cleanup on unmount
        }
    }, [])

    // UPDATE
    const save = async (updated: CapacityConfig) => {
        setLoading(true)
        try {
            const result = await updateCapacityConfig(updated)
            setConfig(result)
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'An unexpected error occured')
        } finally {
            setLoading(false)
        }
    }

    // RESET
    const reset = async () => {
        setLoading(true)
        try {
            const result = await resetCapacityConfig()
            setConfig(result)
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : 'An une')
        } finally {
            setLoading(false)
        }
    }

    return { config, loading, error, save, reset }
}