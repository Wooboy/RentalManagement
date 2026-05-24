<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">帳號管理</h2>
    <div class="flex flex-wrap items-end gap-3 mb-3">
      <label class="form-control min-w-64">
        <span class="label-text mb-1">帳號關鍵字</span>
        <input v-model="keyword" class="input input-bordered" placeholder="輸入帳號關鍵字" />
      </label>
      <button class="btn" @click="load">查詢</button>
      <button class="btn btn-primary" @click="openCreateModal">新增</button>
    </div>

    <p v-if="listError" class="text-error text-sm mb-2">{{ listError }}</p>
    <table class="table table-zebra">
      <thead>
        <tr>
          <th>帳號</th>
          <th>角色</th>
          <th>建立時間</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="u in items" :key="u.id">
          <td>{{ u.username }}</td>
          <td>{{ u.role === 1 ? '管理員' : '租客' }}</td>
          <td>{{ formatUtc(u.createdAtUtc) }}</td>
          <td class="flex gap-2 justify-end">
            <button class="btn btn-sm" @click="edit(u)">編輯</button>
            <button class="btn btn-sm btn-error" @click="remove(u.id)">刪除</button>
          </td>
        </tr>
      </tbody>
    </table>

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-xl">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯管理員' : '新增管理員' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <label class="form-control">
            <span class="label-text mb-1">帳號 *</span>
            <input v-model="form.username" class="input input-bordered" placeholder="輸入帳號" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">角色 *</span>
            <select v-model.number="form.role" class="select select-bordered">
              <option :value="1">管理員</option>
              <option :value="2">租客</option>
            </select>
          </label>
          <label class="form-control">
            <span class="label-text mb-1">密碼 {{ form.id ? '(留白表示不變更)' : '*' }}</span>
            <input
              v-model="form.password"
              type="password"
              class="input input-bordered"
              placeholder="輸入密碼"
            />
          </label>
        </div>
        <p class="text-error text-sm mt-3">{{ formError }}</p>
        <div class="modal-action">
          <button class="btn" @click="closeModal">取消</button>
          <button class="btn btn-primary" @click="save">{{ form.id ? '儲存' : '新增' }}</button>
        </div>
      </div>
    </dialog>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'

type AdminUser = {
  id: number
  username: string
  role: number
  createdAtUtc: string
}

const items = ref<AdminUser[]>([])
const keyword = ref('')
const listError = ref('')
const formError = ref('')
const showModal = ref(false)
const form = ref({ id: 0, username: '', role: 1, password: '' })

const load = async () => {
  listError.value = ''
  try {
    const { data } = await api.get('/admin-users', { params: { keyword: keyword.value || undefined } })
    items.value = data
  } catch (e: any) {
    listError.value = e?.response?.data || e?.message || '載入管理員資料失敗'
  }
}

onMounted(load)

const formatUtc = (value?: string) => {
  if (!value) return ''
  return value.slice(0, 19).replace('T', ' ')
}

const reset = () => {
  form.value = { id: 0, username: '', role: 1, password: '' }
  formError.value = ''
}

const openCreateModal = () => {
  reset()
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  reset()
}

const edit = (u: AdminUser) => {
  form.value = { id: u.id, username: u.username, role: u.role, password: '' }
  formError.value = ''
  showModal.value = true
}

const save = async () => {
  formError.value = ''
  if (!String(form.value.username || '').trim()) {
    formError.value = '請輸入帳號'
    return
  }
  if (!form.value.id && !String(form.value.password || '').trim()) {
    formError.value = '請輸入密碼'
    return
  }

  const payload = {
    username: form.value.username,
    role: form.value.role,
    password: form.value.password || null
  }
  try {
    if (form.value.id) await api.put(`/admin-users/${form.value.id}`, payload)
    else await api.post('/admin-users', payload)
    closeModal()
    await load()
  } catch (e: any) {
    formError.value = e?.response?.data || e?.message || '儲存失敗'
  }
}

const remove = async (id: number) => {
  listError.value = ''
  try {
    await api.delete(`/admin-users/${id}`)
    await load()
  } catch (e: any) {
    listError.value = e?.response?.data || e?.message || '刪除失敗'
  }
}
</script>
