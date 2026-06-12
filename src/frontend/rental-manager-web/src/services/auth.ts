import { reactive } from 'vue'

type AuthState = {
  token: string
  userId: string
  username: string
  role: string
}

const readAuthState = (): AuthState => ({
  token: localStorage.getItem('token') || '',
  userId: localStorage.getItem('auth_user_id') || '',
  username: localStorage.getItem('auth_username') || '',
  role: localStorage.getItem('auth_role') || ''
})

export const authState = reactive<AuthState>(readAuthState())

export const setAuthState = (payload: AuthState) => {
  authState.token = payload.token
  authState.userId = payload.userId
  authState.username = payload.username
  authState.role = payload.role

  localStorage.setItem('token', payload.token)
  localStorage.setItem('auth_user_id', payload.userId)
  localStorage.setItem('auth_username', payload.username)
  localStorage.setItem('auth_role', payload.role)
}

export const clearAuthState = () => {
  authState.token = ''
  authState.userId = ''
  authState.username = ''
  authState.role = ''

  localStorage.removeItem('token')
  localStorage.removeItem('auth_user_id')
  localStorage.removeItem('auth_username')
  localStorage.removeItem('auth_role')
}

export const syncAuthState = () => {
  const nextState = readAuthState()
  authState.token = nextState.token
  authState.userId = nextState.userId
  authState.username = nextState.username
  authState.role = nextState.role
}
