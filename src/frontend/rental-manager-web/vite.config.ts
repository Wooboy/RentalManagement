import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import pkg from './package.json'

const buildTime = new Date().toLocaleString('sv-SE', { timeZone: 'Asia/Taipei' }).slice(0, 16)

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  define: {
    'import.meta.env.APP_VERSION': JSON.stringify(pkg.version),
    'import.meta.env.BUILD_TIME': JSON.stringify(buildTime)
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5091',
        changeOrigin: true
      }
    }
  }
})
