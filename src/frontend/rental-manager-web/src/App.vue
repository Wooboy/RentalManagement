<template>
  <div v-if="isLoginPage" class="min-h-screen bg-base-200">
    <main class="min-h-screen">
      <RouterView />
    </main>
  </div>
  <div v-else class="min-h-screen bg-base-200 flex">
    <aside class="w-72 h-screen bg-base-100 border-r border-base-300 flex flex-col shrink-0">
      <div class="px-5 py-5 border-b border-base-300">
        <p class="text-xs uppercase tracking-wider text-base-content/60">Rental Manager</p>
        <h1 class="text-xl font-bold">租屋管理系統</h1>
      </div>
      <nav class="p-3 flex-1 overflow-y-auto">
        <ul class="menu w-full gap-1">
          <li><RouterLink to="/" :class="linkClass('/')">儀表板</RouterLink></li>
          <li><RouterLink to="/tenants" :class="linkClass('/tenants')">租客</RouterLink></li>
          <li><RouterLink to="/properties" :class="linkClass('/properties')">房源</RouterLink></li>
          <li><RouterLink to="/contracts" :class="linkClass('/contracts')">合約</RouterLink></li>
          <li><RouterLink to="/charges" :class="linkClass('/charges')">應收</RouterLink></li>
          <li><RouterLink to="/expenses" :class="linkClass('/expenses')">支出</RouterLink></li>
          <li><RouterLink to="/electricity" :class="linkClass('/electricity')">電費試算</RouterLink></li>
        </ul>
      </nav>
      <div class="p-4 border-t border-base-300">
        <p class="text-xs text-base-content/60 mb-1">目前登入</p>
        <p class="font-medium truncate">{{ currentUser }}</p>
        <button class="btn btn-sm btn-outline mt-3 w-full" @click="logout">登出</button>
      </div>
    </aside>
    <main class="flex-1 p-6 overflow-auto">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()
const isLoginPage = computed(() => route.path === '/login')
const currentUser = computed(() => localStorage.getItem('auth_username') || '未登入')
const linkClass = (path: string) => (route.path === path ? 'active' : '')
const logout = async () => {
  localStorage.removeItem('token')
  localStorage.removeItem('auth_username')
  await router.push('/login')
}
</script>
