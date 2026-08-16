import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import './style.css'
import { applyAuthGuard, routes } from './router'

const router = createRouter({ history: createWebHistory(), routes })
const app = createApp(App)

app.use(createPinia())
applyAuthGuard(router)
app.use(router)
app.mount('#app')
