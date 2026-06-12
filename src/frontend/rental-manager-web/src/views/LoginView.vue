<template>
  <div class="min-h-screen bg-base-200 flex items-center justify-center p-6">
    <div class="w-full max-w-md card bg-base-100 shadow-xl border border-base-300">
      <form class="card-body" @submit.prevent="login">
        <p class="text-xs uppercase tracking-wider text-base-content/60">Rental Manager</p>
        <h2 class="text-2xl font-bold mb-2">使用者登入</h2>
        <label class="form-control mb-3">
          <span class="label-text mb-1">帳號</span>
          <input v-model="username" class="input input-bordered" placeholder="請輸入帳號" />
        </label>
        <label class="form-control mb-4">
          <span class="label-text mb-1">密碼</span>
          <input v-model="password" type="password" class="input input-bordered" placeholder="請輸入密碼" />
        </label>
        <button type="submit" class="btn btn-primary">登入</button>
        <p class="text-sm mt-3" :class="isError ? 'text-error' : 'text-success'">{{ message }}</p>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../services/api'
import { setAuthState } from '../services/auth'

const route = useRoute()
const router = useRouter()
const username = ref('')
const password = ref('')
const message = ref('')
const isError = ref(false)

const login = async () => {
  try {
    const { data } = await api.post('/auth/login', { username: username.value, password: password.value })
    setAuthState({
      token: data.token,
      userId: String(data.userId),
      username: data.username,
      role: String(data.role)
    })
    isError.value = false
    message.value = `登入成功：${data.username}`
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.push(redirect)
  } catch (e: any) {
    isError.value = true
    message.value = e?.response?.data || '登入失敗'
  }
}
</script>
