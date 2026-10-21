import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// En desarrollo, Vite reenvía /api y /health a la API local (perfil "http" de Visual Studio),
// así el navegador ve un solo origen y la API no necesita CORS (ADR 0006).
// En docker compose el build se sirve desde wwwroot de la propia API.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      '/health': 'http://localhost:5080',
    },
  },
});
