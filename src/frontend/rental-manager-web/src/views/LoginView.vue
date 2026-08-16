<template>
  <div class="min-h-screen bg-slate-950 flex items-center justify-center p-6">
    <div class="w-full max-w-sm">
      <div class="flex flex-col items-center mb-6">
        <div class="grid place-items-center w-12 h-12 rounded-xl bg-emerald-500 text-slate-950 shadow-lg shadow-emerald-500/25 mb-3">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" viewBox="0 0 24 24" fill="currentColor">
            <path d="M11.25 3v8.25H4.5L12.75 21v-8.25H19.5L11.25 3Z" />
          </svg>
        </div>
        <p class="text-[11px] font-medium uppercase tracking-widest text-emerald-400/80">Rental Manager</p>
        <h1 class="text-lg font-semibold text-white">租屋管理系統</h1>
      </div>

      <div class="card bg-base-100 shadow-2xl border border-base-300">
        <form class="card-body gap-4" @submit.prevent="login">
          <h2 class="text-xl font-semibold tracking-tight">使用者登入</h2>
          <label class="form-control">
            <span class="label-text mb-1.5">帳號</span>
            <input v-model="username" class="input input-bordered w-full" placeholder="請輸入帳號" autocomplete="username" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1.5">密碼</span>
            <input v-model="password" type="password" class="input input-bordered w-full" placeholder="請輸入密碼" autocomplete="current-password" />
          </label>
          <button type="submit" class="btn btn-primary w-full" :disabled="loading">
            <span v-if="loading" class="loading loading-spinner loading-sm"></span>
            登入
          </button>
          <p v-if="message" class="text-sm text-center" :class="isError ? 'text-error' : 'text-success'">{{ message }}</p>
        </form>
      </div>

      <p class="text-center text-xs text-slate-500 mt-6">v{{ appVersion }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const username = ref('')
const password = ref('')
const message = ref('')
const isError = ref(false)
const loading = ref(false)
const appVersion = import.meta.env.APP_VERSION || 'dev'

const login = async () => {
  loading.value = true
  try {
    const { data } = await api.post('/auth/login', { username: username.value, password: password.value })
    auth.setAuth({
      token: data.token,
      userId: data.userId,
      username: data.username,
      role: data.role,
      tenantId: data.tenantId ?? null,
      displayName: data.displayName ?? null
    })
    isError.value = false
    message.value = `登入成功：${data.displayName || data.username}`
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : auth.homePath
    await router.push(redirect)
  } catch (e: any) {
    isError.value = true
    message.value = e?.response?.data || '登入失敗'
  } finally {
    loading.value = false
  }
}
</script>
