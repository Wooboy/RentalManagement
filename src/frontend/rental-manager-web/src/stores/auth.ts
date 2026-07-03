import { defineStore } from 'pinia'

export type AuthPayload = {
  token: string
  userId: number
  username: string
  role: number
  tenantId: number | null
  displayName: string | null
}

const STORAGE_KEYS = {
  token: 'token',
  userId: 'auth_user_id',
  username: 'auth_username',
  role: 'auth_role',
  tenantId: 'auth_tenant_id',
  displayName: 'auth_display_name'
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem(STORAGE_KEYS.token) || '',
    userId: Number(localStorage.getItem(STORAGE_KEYS.userId)) || 0,
    username: localStorage.getItem(STORAGE_KEYS.username) || '',
    role: Number(localStorage.getItem(STORAGE_KEYS.role)) || 0,
    tenantId: Number(localStorage.getItem(STORAGE_KEYS.tenantId)) || 0,
    displayName: localStorage.getItem(STORAGE_KEYS.displayName) || ''
  }),
  getters: {
    isLoggedIn: (s) => !!s.token,
    isAdmin: (s) => s.role === 1,
    isTenant: (s) => s.role === 2,
    /** 依角色決定登入後首頁 */
    homePath: (s) => (s.role === 2 ? '/portal' : '/admin')
  },
  actions: {
    setAuth(payload: AuthPayload) {
      this.token = payload.token
      this.userId = payload.userId
      this.username = payload.username
      this.role = payload.role
      this.tenantId = payload.tenantId || 0
      this.displayName = payload.displayName || ''

      localStorage.setItem(STORAGE_KEYS.token, payload.token)
      localStorage.setItem(STORAGE_KEYS.userId, String(payload.userId))
      localStorage.setItem(STORAGE_KEYS.username, payload.username)
      localStorage.setItem(STORAGE_KEYS.role, String(payload.role))
      localStorage.setItem(STORAGE_KEYS.tenantId, payload.tenantId ? String(payload.tenantId) : '')
      localStorage.setItem(STORAGE_KEYS.displayName, payload.displayName || '')
    },
    clear() {
      this.token = ''
      this.userId = 0
      this.username = ''
      this.role = 0
      this.tenantId = 0
      this.displayName = ''
      Object.values(STORAGE_KEYS).forEach((key) => localStorage.removeItem(key))
    }
  }
})
