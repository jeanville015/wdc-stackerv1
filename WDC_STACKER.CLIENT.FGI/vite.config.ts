import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
    base: '/WDC_STACKER_FGI/',
    plugins: [react()],
    resolve: {
        alias: {
            react: path.resolve('./node_modules/react'),
            'react-dom': path.resolve('./node_modules/react-dom'),
        },
    },
    server: {
        port: 5174,
        open: '/config',
        proxy: {
            '/api': {
                target: 'http://localhost:5002',
                changeOrigin: true,
                secure: false,
                rewrite: (path) => path
            },
            '/FGI_Service': {
                target: 'http://pbt-md-app03',
                changeOrigin: true,
                secure: false
            }
        }
    }
}) 

