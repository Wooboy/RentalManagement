<template>
  <div class="card bg-base-100 shadow p-6 max-w-md">
    <h2 class="text-xl font-bold mb-4">管理者登入</h2>
    <input v-model="username" class="input input-bordered mb-3" placeholder="帳號" />
    <input v-model="password" type="password" class="input input-bordered mb-3" placeholder="密碼" />
    <button class="btn btn-primary" @click="login">登入</button>
    <p class="text-sm mt-3">{{ message }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import api from '../services/api'

const username = ref('admin')
const password = ref('admin123')
const message = ref('')

const login = async () => {
  const { data } = await api.post('/auth/login', { username: username.value, password: password.value })
  localStorage.setItem('token', data.token)
  message.value = `登入成功：${data.username}`
}
</script>
