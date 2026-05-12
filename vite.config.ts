import { defineConfig } from 'vite'; 
import react from '@vitejs/plugin-react'; 

export default defineConfig({
    plugins: [react()],
    server: {
        port: 5173,
        open: '/config',
        proxy: {
            '/api': {
                target: 'http://localhost:5002',  // ← your API port
                changeOrigin: true,
                secure: false,
                rewrite: (path) => path  // ← explicitly keep the path as-is
            }
        }
    }
})
