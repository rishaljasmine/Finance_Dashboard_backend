import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The "microfrontend" build — bundles the sidebar + React + ReactDOM into
// ONE plain <script> file that defines a <finova-sidebar> custom element.
// Any host page (Angular, plain HTML, whatever) can drop this file in with
// a single <script> tag; it needs nothing else from this project.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // React's own bundled code branches on process.env.NODE_ENV at load time
  // (dev build vs. prod build). There's no Node "process" global in a
  // plain browser <script>, so without this define it throws immediately
  // and the whole bundle dies before customElements.define() ever runs.
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },
  build: {
    outDir: 'dist-element',
    emptyOutDir: true,
    lib: {
      entry: 'src/web-component.tsx',
      name: 'FinovaSidebarElement',
      formats: ['iife'],
      fileName: () => 'finova-sidebar.js',
    },
    cssCodeSplit: false,
  },
});
