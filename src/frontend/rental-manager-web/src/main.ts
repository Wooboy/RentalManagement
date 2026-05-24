import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import './style.css'
import { applyAuthGuard, routes } from './router'

const router = createRouter({ history: createWebHistory(), routes })
applyAuthGuard(router)
createApp(App).use(router).mount('#app')
