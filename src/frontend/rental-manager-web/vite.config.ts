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
    // 專案位於網路磁碟機（Z:）時，Node 原生 fs.watch 不支援變更通知會拋 UNKNOWN watch 錯誤，
    // 改用輪詢監看才能正常啟動與熱更新。
    watch: {
      usePolling: true,
      interval: 300
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5091',
        changeOrigin: true
      }
    }
  }
})
