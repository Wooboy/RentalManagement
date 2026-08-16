<template>
  <div class="min-h-screen bg-base-200 flex flex-col">
    <header class="navbar bg-base-100 border-b border-base-300 px-4">
      <div class="flex-1">
        <span class="text-xs uppercase tracking-wider text-base-content/60 mr-2 hidden sm:inline">Rental Manager</span>
        <span class="font-bold">房客服務入口</span>
      </div>
      <div class="flex-none">
        <div class="dropdown dropdown-end">
          <div tabindex="0" role="button" class="btn btn-ghost">
            <span class="truncate max-w-40">{{ currentUser }}</span>
          </div>
          <ul tabindex="0" class="dropdown-content menu bg-base-100 rounded-box z-10 w-48 p-2 shadow border border-base-300">
            <li><button @click="openPasswordModal">修改密碼</button></li>
            <li><button @click="logout">登出</button></li>
          </ul>
        </div>
      </div>
    </header>

    <div class="bg-base-100 border-b border-base-300">
      <div class="max-w-4xl w-full mx-auto px-4">
        <div role="tablist" class="tabs tabs-bordered">
          <RouterLink
            v-for="item in navItems"
            :key="item.to"
            :to="item.to"
            role="tab"
            class="tab"
            :class="{ 'tab-active': route.path === item.to }"
          >
            {{ item.label }}
          </RouterLink>
        </div>
      </div>
    </div>

    <main class="flex-1 p-4 lg:p-6 max-w-4xl w-full mx-auto">
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
        <button class="btn" @click="showPasswordModal = false">取消</button>
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
const currentUser = computed(() => auth.displayName || auth.username || '未登入')

const navItems = [
  { to: '/portal', label: '總覽' },
  { to: '/portal/contracts', label: '我的合約' },
  { to: '/portal/charges', label: '我的帳單' },
  { to: '/portal/repairs', label: '線上報修' }
]

const showPasswordModal = ref(false)
const passwordForm = ref({ currentPassword: '', newPassword: '' })
const passwordMessage = ref('')
const passwordError = ref(false)

const openPasswordModal = () => {
  passwordForm.value = { currentPassword: '', newPassword: '' }
  passwordMessage.value = ''
  passwordError.value = false
  showPasswordModal.value = true
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
