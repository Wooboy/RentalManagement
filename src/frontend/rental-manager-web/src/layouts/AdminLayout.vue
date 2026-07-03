<template>
  <div class="drawer lg:drawer-open h-screen bg-base-200">
    <input id="admin-drawer" v-model="drawerOpen" type="checkbox" class="drawer-toggle" />

    <div class="drawer-content flex flex-col h-screen overflow-hidden">
      <header class="navbar bg-base-100 border-b border-base-300 min-h-14 px-4 shrink-0 lg:hidden">
        <label for="admin-drawer" class="btn btn-ghost btn-square">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </label>
        <span class="font-bold ml-2">租屋管理系統</span>
      </header>
      <main class="flex-1 overflow-y-auto p-4 lg:p-6">
        <RouterView />
      </main>
    </div>

    <aside class="drawer-side z-20">
      <label for="admin-drawer" aria-label="close sidebar" class="drawer-overlay"></label>
      <div class="w-72 h-full bg-base-100 border-r border-base-300 flex flex-col">
        <div class="px-5 py-5 border-b border-base-300">
          <p class="text-xs uppercase tracking-wider text-base-content/60">Rental Manager</p>
          <h1 class="text-xl font-bold">租屋管理系統</h1>
        </div>
        <nav class="p-3 flex-1 overflow-y-auto">
          <ul class="menu w-full gap-1">
            <li v-for="item in navItems" :key="item.to">
              <RouterLink :to="item.to" :class="linkClass(item.to)" @click="drawerOpen = false">
                {{ item.label }}
              </RouterLink>
            </li>
          </ul>
        </nav>
        <div class="p-4 border-t border-base-300">
          <p class="text-xs text-base-content/60 mb-1">目前登入</p>
          <div class="dropdown dropdown-top w-full">
            <div tabindex="0" role="button" class="btn btn-outline w-full justify-between">
              <span class="truncate">{{ currentUser }}</span>
            </div>
            <ul tabindex="0" class="dropdown-content menu bg-base-100 rounded-box z-10 w-full p-2 shadow border border-base-300">
              <li><button @click="openPasswordModal">修改密碼</button></li>
              <li><button @click="logout">登出</button></li>
            </ul>
          </div>
        </div>
      </div>
    </aside>
  </div>

  <dialog class="modal" :class="{ 'modal-open': showPasswordModal }">
    <div class="modal-box max-w-md">
      <h3 class="font-bold text-lg mb-3">修改密碼</h3>
      <div class="space-y-3">
        <label class="form-control">
          <span class="label-text mb-1">目前密碼</span>
          <input v-model="passwordForm.currentPassword" type="password" class="input input-bordered" />
        </label>
        <label class="form-control">
          <span class="label-text mb-1">新密碼</span>
          <input v-model="passwordForm.newPassword" type="password" class="input input-bordered" />
        </label>
      </div>
      <p class="text-sm mt-3" :class="passwordError ? 'text-error' : 'text-success'">{{ passwordMessage }}</p>
      <div class="modal-action">
        <button class="btn" @click="closePasswordModal">取消</button>
        <button class="btn btn-primary" @click="changePassword">儲存</button>
      </div>
    </div>
  </dialog>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../services/api'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const navItems = [
  { to: '/admin', label: '儀表板' },
  { to: '/admin/tenants', label: '租客名單' },
  { to: '/admin/properties', label: '房源管理' },
  { to: '/admin/contracts', label: '合約管理' },
  { to: '/admin/charges', label: '應收費用' },
  { to: '/admin/expenses', label: '支出費用' },
  { to: '/admin/electricity', label: '電費試算' },
  { to: '/admin/electricity-meter-readings', label: '電錶抄表' },
  { to: '/admin/repairs', label: '報修管理' },
  { to: '/admin/users', label: '帳號管理' }
]

const drawerOpen = ref(false)
const currentUser = computed(() => auth.displayName || auth.username || '未登入')
const linkClass = (path: string) => (route.path === path ? 'active' : '')

const showPasswordModal = ref(false)
const passwordForm = ref({ currentPassword: '', newPassword: '' })
const passwordMessage = ref('')
const passwordError = ref(false)

const resetPasswordForm = () => {
  passwordForm.value = { currentPassword: '', newPassword: '' }
  passwordMessage.value = ''
  passwordError.value = false
}

const openPasswordModal = () => {
  resetPasswordForm()
  showPasswordModal.value = true
}

const closePasswordModal = () => {
  showPasswordModal.value = false
  resetPasswordForm()
}

const changePassword = async () => {
  try {
    await api.post('/auth/change-password', passwordForm.value)
    passwordError.value = false
    passwordMessage.value = '密碼修改成功'
  } catch (e: any) {
    passwordError.value = true
    passwordMessage.value = e?.response?.data || e?.message || '修改密碼失敗'
  }
}

const logout = async () => {
  auth.clear()
  await router.push('/login')
}
</script>
