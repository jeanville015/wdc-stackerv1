import { API_BASE } from '../config/apiConfig';
import type { CapacityConfig } from '../types/models';

const BASE_URL = `${API_BASE}/api/capacity-config`;

// READ
export const getCapacityConfig = async (): Promise<CapacityConfig> => {
    const res = await fetch(BASE_URL);
    if (!res.ok) throw new Error('Failed to fetch config');
    return res.json();
};

// UPDATE (full replace)
export const updateCapacityConfig = async (config: CapacityConfig): Promise<CapacityConfig> => {
    const res = await fetch(BASE_URL, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
    });
    if (!res.ok) throw new Error('Failed to update config');
    return res.json();
};

// UPDATE (partial)
export const patchCapacityConfig = async (partial: Partial<CapacityConfig>): Promise<CapacityConfig> => {
    const res = await fetch(BASE_URL, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(partial)
    });
    if (!res.ok) throw new Error('Failed to patch config');
    return res.json();
};

// RESET
export const resetCapacityConfig = async (): Promise<CapacityConfig> => {
    const res = await fetch(`${BASE_URL}/reset`, { method: 'DELETE' });
    if (!res.ok) throw new Error('Failed to reset config');
    return res.json();
};