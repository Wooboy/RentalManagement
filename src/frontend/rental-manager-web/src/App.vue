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
          <li v-if="isAdmin"><RouterLink to="/admin-users" :class="linkClass('/admin-users')">帳號管理</RouterLink></li>
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
    </aside>
    <main class="flex-1 p-6 overflow-auto">
      <RouterView />
    </main>
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
import { computed } from 'vue'
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from './services/api'

const route = useRoute()
const router = useRouter()
const isLoginPage = computed(() => route.path === '/login')
const currentUser = computed(() => localStorage.getItem('auth_username') || '未登入')
const isAdmin = computed(() => localStorage.getItem('auth_role') === '1')
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
  localStorage.removeItem('token')
  localStorage.removeItem('auth_username')
  localStorage.removeItem('auth_user_id')
  localStorage.removeItem('auth_role')
  await router.push('/login')
}
</script>
