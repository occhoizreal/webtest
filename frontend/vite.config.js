import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    // vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  server: {
    host: true,         // ← this allows connections from your local network
    port: 5173,         // ← optional, but makes it explicit
  },
  // Set base path for GitHub Pages deployment
  base: process.env.NODE_ENV === 'production' ? '/webtest/' : '/',
})
