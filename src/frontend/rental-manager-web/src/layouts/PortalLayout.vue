<template>
  <div class="min-h-screen bg-base-200 flex flex-col">
    <header class="navbar bg-base-100 border-b border-base-300 px-4">
      <div class="flex-1">
        <span class="text-xs uppercase tracking-wider text-base-content/60 mr-2">Rental Manager</span>
        <span class="font-bold">房客服務入口</span>
      </div>
      <div class="flex-none">
        <div class="dropdown dropdown-end">
          <div tabindex="0" role="button" class="btn btn-ghost">
            <span class="truncate max-w-40">{{ currentUser }}</span>
          </div>
          <ul tabindex="0" class="dropdown-content menu bg-base-100 rounded-box z-10 w-48 p-2 shadow border border-base-300">
            <li><button @click="logout">登出</button></li>
          </ul>
        </div>
      </div>
    </header>
    <main class="flex-1 p-4 lg:p-6 max-w-4xl w-full mx-auto">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()
const currentUser = computed(() => auth.displayName || auth.username || '未登入')

const logout = async () => {
  auth.clear()
  await router.push('/login')
}
</script>
