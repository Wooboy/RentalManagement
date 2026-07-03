import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const api = axios.create({ baseURL })
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// 後端錯誤統一為 ProblemDetails 格式，這裡把 response.data 正規化回字串，
// 讓畫面沿用 e?.response?.data 顯示錯誤訊息。
// Token 過期（401）時自動導回登入頁；登入頁本身的 401 交由畫面顯示錯誤。
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const data = error?.response?.data
    if (data && typeof data === 'object') {
      error.response.data = data.detail || data.title || error.message
    }
    if (error?.response?.status === 401 && !error?.config?.url?.includes('/auth/login')) {
      localStorage.removeItem('token')
      if (window.location.pathname !== '/login') {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export default api
