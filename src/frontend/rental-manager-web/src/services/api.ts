import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const api = axios.create({ baseURL })
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// 後端錯誤統一為 ProblemDetails 格式，這裡把 response.data 正規化回字串，
// 讓既有畫面沿用 e?.response?.data 顯示錯誤訊息。
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const data = error?.response?.data
    if (data && typeof data === 'object') {
      error.response.data = data.detail || data.title || error.message
    }
    return Promise.reject(error)
  }
)

export default api
