import { useState, useEffect, useRef } from 'react'
import { useCapacityConfig } from '../hooks/useCapacityConfig';
import type { CapacityConfig } from '../types/models';

const ConfigPage = () => {
    const { config, loading, error, save, reset } = useCapacityConfig();
    const [form, setForm] = useState<CapacityConfig | null>(null);
    const initialized = useRef(false)

    // Sync form when config loads
    useEffect(() => {
        if (config && !initialized.current) {
            initialized.current = true 
            setForm(config);
        } 
    }, [config]);

    const handleChange = (field: keyof CapacityConfig, value: string) => {
        if (!form) return;
        setForm({ ...form, [field]: parseInt(value) || 0 });
    };

    const handleSave = () => { if (form) save(form); };
    const handleReset = () => { if (confirm('Reset to defaults?')) reset(); };

    if (loading) return <p>Loading...</p>;
    if (error) return <p>Error: {error}</p>;
    if (!form) return null;

    return (
        <div>
            <h2>Capacity Configuration</h2>

            {(Object.keys(form) as (keyof CapacityConfig)[]).map(field => (
                <div key={field}>
                    <label>{field}</label>
                    <input
                        type="number"
                        value={form[field]}
                        onChange={e => handleChange(field, e.target.value)}
                    />
                </div>
            ))}

            <button onClick={handleSave}>Save</button>
            <button onClick={handleReset}>Reset to Defaults</button>
        </div>
    );
};

export default ConfigPage;