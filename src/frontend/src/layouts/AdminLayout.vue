<template>
  <div class="drawer lg:drawer-open h-screen bg-base-200">
    <input id="admin-drawer" v-model="drawerOpen" type="checkbox" class="drawer-toggle" />

    <div class="drawer-content flex flex-col h-screen overflow-hidden">
      <!-- 頂部列：目前所在區塊 + 頁面標題 -->
      <header class="shrink-0 flex items-center gap-3 h-16 px-4 lg:px-6 bg-base-100 border-b border-base-300">
        <label for="admin-drawer" class="btn btn-ghost btn-square btn-sm lg:hidden">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </label>
        <div class="min-w-0">
          <p class="text-[11px] font-medium uppercase tracking-widest text-base-content/40 leading-none">{{ currentGroup }}</p>
          <h1 class="text-base font-semibold leading-tight truncate mt-0.5">{{ currentTitle }}</h1>
        </div>
        <div class="ml-auto hidden sm:flex items-center gap-2 text-xs text-base-content/50">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
            <path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0V11.25A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5" />
          </svg>
          <span class="tabular-nums">{{ today }}</span>
        </div>
      </header>

      <main class="flex-1 overflow-y-auto">
        <div class="p-4 lg:p-6 max-w-[1600px] mx-auto w-full">
          <RouterView />
        </div>
      </main>
    </div>

    <aside class="drawer-side z-20">
      <label for="admin-drawer" aria-label="關閉側欄" class="drawer-overlay"></label>
      <div class="w-72 h-full bg-slate-950 text-slate-300 flex flex-col">
        <!-- 品牌 -->
        <div class="flex items-center gap-3 px-5 h-16 border-b border-white/5">
          <div class="grid place-items-center w-9 h-9 rounded-lg bg-emerald-500 text-slate-950 shadow-lg shadow-emerald-500/25">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
              <path d="M11.25 3v8.25H4.5L12.75 21v-8.25H19.5L11.25 3Z" />
            </svg>
          </div>
          <div class="leading-tight">
            <p class="text-[11px] font-medium uppercase tracking-widest text-emerald-400/80">Rental Manager</p>
            <h1 class="text-sm font-semibold text-white">租屋管理系統</h1>
          </div>
        </div>

        <!-- 導覽 -->
        <nav class="flex-1 overflow-y-auto py-4 px-3 space-y-6">
          <div v-for="group in navGroups" :key="group.label">
            <p class="px-3 mb-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-500">{{ group.label }}</p>
            <ul class="space-y-0.5">
              <li v-for="item in group.items" :key="item.to">
                <RouterLink
                  :to="item.to"
                  @click="drawerOpen = false"
                  class="group flex items-center gap-3 rounded-lg pl-2 pr-3 py-2 text-sm transition-colors"
                  :class="isActive(item.to) ? 'bg-emerald-500/10 text-white' : 'text-slate-400 hover:bg-white/5 hover:text-slate-100'"
                >
                  <span class="w-1 h-5 rounded-full shrink-0" :class="isActive(item.to) ? 'bg-emerald-400' : 'bg-transparent'"></span>
                  <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.7"
                    :class="isActive(item.to) ? 'text-emerald-400' : 'text-slate-500 group-hover:text-slate-300'">
                    <path stroke-linecap="round" stroke-linejoin="round" :d="item.icon" />
                  </svg>
                  <span class="truncate">{{ item.label }}</span>
                </RouterLink>
              </li>
            </ul>
          </div>
        </nav>

        <!-- 使用者 -->
        <div class="border-t border-white/5 p-3">
          <div class="dropdown dropdown-top w-full">
            <div tabindex="0" role="button" class="flex items-center gap-3 w-full rounded-lg pl-2 pr-3 py-2 hover:bg-white/5 cursor-pointer">
              <div class="grid place-items-center w-9 h-9 rounded-full bg-emerald-500/15 text-emerald-400 text-sm font-semibold shrink-0">{{ initials }}</div>
              <div class="min-w-0 text-left flex-1">
                <p class="text-sm font-medium text-white truncate">{{ currentUser }}</p>
                <p class="text-xs text-slate-500 truncate" :title="buildTime ? `建置於 ${buildTime}` : ''">v{{ appVersion }}<template v-if="buildTime"> · {{ buildTime }}</template></p>
              </div>
              <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-slate-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="m8.25 15 3.75 3.75L15.75 15m-7.5-6L12 5.25 15.75 9" />
              </svg>
            </div>
            <ul tabindex="0" class="dropdown-content menu bg-base-100 text-base-content rounded-box z-10 w-full p-2 shadow-lg border border-base-300 mb-2">
              <li><button @click="openPasswordModal">修改密碼</button></li>
              <li><button class="text-error" @click="logout">登出</button></li>
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

type NavItem = { to: string; label: string; icon: string }
type NavGroup = { label: string; items: NavItem[] }

const navGroups: NavGroup[] = [
  {
    label: '總覽',
    items: [
      { to: '/admin', label: '儀表板', icon: 'M3.75 6A2.25 2.25 0 0 1 6 3.75h2.25A2.25 2.25 0 0 1 10.5 6v2.25a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25V6ZM3.75 15.75A2.25 2.25 0 0 1 6 13.5h2.25a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25H6A2.25 2.25 0 0 1 3.75 18v-2.25ZM13.5 6a2.25 2.25 0 0 1 2.25-2.25H18A2.25 2.25 0 0 1 20.25 6v2.25A2.25 2.25 0 0 1 18 10.5h-2.25a2.25 2.25 0 0 1-2.25-2.25V6ZM13.5 15.75a2.25 2.25 0 0 1 2.25-2.25H18a2.25 2.25 0 0 1 2.25 2.25V18A2.25 2.25 0 0 1 18 20.25h-2.25a2.25 2.25 0 0 1-2.25-2.25v-2.25Z' }
    ]
  },
  {
    label: '出租管理',
    items: [
      { to: '/admin/tenants', label: '租客名單', icon: 'M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z' },
      { to: '/admin/properties', label: '房源管理', icon: 'M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Z' },
      { to: '/admin/contracts', label: '合約管理', icon: 'M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z' }
    ]
  },
  {
    label: '帳務',
    items: [
      { to: '/admin/charges', label: '應收費用', icon: 'M2.25 18.75a60.07 60.07 0 0 1 15.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 0 1 3 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 0 0-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 0 1-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 0 0 3 15h-.75M15 10.5a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm3 0h.008v.008H18V10.5Zm-12 0h.008v.008H6V10.5Z' },
      { to: '/admin/expenses', label: '支出費用', icon: 'M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 0 0 2.25-2.25V6.75A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25v10.5A2.25 2.25 0 0 0 4.5 19.5Z' },
      { to: '/admin/electricity', label: '電費試算', icon: 'M15.75 15.75V18m-7.5-6.75h.008v.008H8.25v-.008Zm0 2.25h.008v.008H8.25V13.5Zm0 2.25h.008v.008H8.25v-.008Zm0 2.25h.008v.008H8.25V18Zm2.498-6.75h.007v.008h-.007v-.008Zm0 2.25h.007v.008h-.007V13.5Zm0 2.25h.007v.008h-.007v-.008Zm0 2.25h.007v.008h-.007V18Zm2.504-6.75h.008v.008h-.008v-.008Zm0 2.25h.008v.008h-.008V13.5Zm0 2.25h.008v.008h-.008v-.008Zm0 2.25h.008v.008h-.008V18Zm2.498-6.75h.008v.008h-.008v-.008Zm0 2.25h.008v.008h-.008V13.5ZM8.25 6h7.5v2.25h-7.5V6ZM12 2.25c-1.892 0-3.758.11-5.593.322C5.307 2.7 4.5 3.65 4.5 4.757V19.5a2.25 2.25 0 0 0 2.25 2.25h10.5a2.25 2.25 0 0 0 2.25-2.25V4.757c0-1.108-.806-2.056-1.907-2.185A48.507 48.507 0 0 0 12 2.25Z' },
      { to: '/admin/electricity-meter-readings', label: '電錶抄表', icon: 'M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75Z' }
    ]
  },
  {
    label: '維運',
    items: [
      { to: '/admin/repairs', label: '報修管理', icon: 'M11.42 15.17 17.25 21A2.652 2.652 0 0 0 21 17.25l-5.877-5.877M11.42 15.17l2.496-3.03c.317-.384.74-.626 1.208-.766M11.42 15.17l-4.655 5.653a2.548 2.548 0 1 1-3.586-3.586l6.837-5.63m5.108-.233c.55-.164 1.163-.188 1.743-.14a4.5 4.5 0 0 0 4.486-6.336l-3.276 3.277a3.004 3.004 0 0 1-2.25-2.25l3.276-3.276a4.5 4.5 0 0 0-6.336 4.486c.091 1.076-.071 2.264-.904 2.95l-.102.085m-1.745 1.437L5.909 7.5H4.5L2.25 3.75l1.5-1.5L7.5 4.5v1.409l4.26 4.26' }
    ]
  },
  {
    label: '系統',
    items: [
      { to: '/admin/users', label: '帳號管理', icon: 'M17.982 18.725A7.488 7.488 0 0 0 12 15.75a7.488 7.488 0 0 0-5.982 2.975m11.963 0a9 9 0 1 0-11.963 0m11.963 0A8.966 8.966 0 0 1 12 21a8.966 8.966 0 0 1-5.982-2.275M15 9.75a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z' }
    ]
  }
]

const appVersion = import.meta.env.APP_VERSION || 'dev'
const buildTime = import.meta.env.BUILD_TIME || ''

const drawerOpen = ref(false)
const currentUser = computed(() => auth.displayName || auth.username || '未登入')
const initials = computed(() => (currentUser.value || '?').trim().charAt(0).toUpperCase())
const isActive = (path: string) => route.path === path

const flatItems = navGroups.flatMap(g => g.items.map(i => ({ ...i, group: g.label })))
const activeEntry = computed(() => flatItems.find(i => i.to === route.path))
const currentTitle = computed(() => activeEntry.value?.label || '租屋管理系統')
const currentGroup = computed(() => activeEntry.value?.group || '總覽')

const today = computed(() =>
  new Date().toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', weekday: 'short' })
)

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
