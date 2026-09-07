import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// Dev/demo build — renders <App> standalone so the sidebar can be built
// and eyeballed on its own, with mock data, before it ever touches the
// real Angular host.
export default defineConfig({
  plugins: [react(), tailwindcss()],
});
