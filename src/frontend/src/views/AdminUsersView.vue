<template>
  <div class="space-y-4">
    <PageHeader title="帳號管理" :subtitle="`共 ${items.length} 個帳號`">
      <template #actions>
        <label class="input input-bordered input-sm flex items-center gap-2 w-56">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" /></svg>
          <input v-model="keyword" class="grow" placeholder="帳號 / 名稱" @keyup.enter="load" />
        </label>
        <button class="btn btn-sm btn-ghost" @click="load">查詢</button>
        <button class="btn btn-sm btn-primary" @click="openCreateModal">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
          新增帳號
        </button>
      </template>
    </PageHeader>

    <p v-if="listError" class="text-error text-sm">{{ listError }}</p>

    <div class="card bg-base-100 border border-base-300 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="table">
          <thead>
            <tr>
              <th>帳號</th>
              <th>名稱</th>
              <th>角色</th>
              <th>關聯租客</th>
              <th>狀態</th>
              <th>建立時間</th>
              <th class="text-right">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in items" :key="u.id">
              <td class="font-medium">{{ u.username }}</td>
              <td>{{ u.displayName || '-' }}</td>
              <td><span class="badge badge-sm" :class="u.role === 1 ? 'badge-neutral badge-soft' : 'badge-ghost'">{{ u.role === 1 ? '管理員' : '租客' }}</span></td>
              <td>{{ u.tenantName || '-' }}</td>
              <td>
                <span class="badge badge-sm" :class="u.isActive ? 'badge-success badge-soft' : 'badge-ghost'">
                  {{ u.isActive ? '啟用' : '停用' }}
                </span>
              </td>
              <td class="tabular-nums">{{ formatUtc(u.createdAtUtc) }}</td>
              <td>
                <div class="flex gap-1 justify-end">
                  <button class="btn btn-xs btn-ghost" @click="edit(u)">編輯</button>
                  <button class="btn btn-xs btn-ghost text-error" @click="remove(u.id)">刪除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0"><td colspan="7" class="text-center text-base-content/50 py-10">尚無帳號資料。</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-xl">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯帳號' : '新增帳號' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
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
            <span class="label-text mb-1">名稱</span>
            <input v-model="form.displayName" class="input input-bordered" placeholder="顯示名稱" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">Email</span>
            <input v-model="form.email" class="input input-bordered" placeholder="Email" />
          </label>
          <label v-if="form.role === 2" class="form-control">
            <span class="label-text mb-1">關聯租客 *</span>
            <select v-model.number="form.tenantId" class="select select-bordered">
              <option :value="0" disabled>請選擇租客</option>
              <option v-for="t in tenants" :key="t.id" :value="t.id">{{ t.name }}</option>
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
          <label class="form-control">
            <span class="label-text mb-1">狀態</span>
            <label class="label cursor-pointer justify-start gap-3">
              <input v-model="form.isActive" type="checkbox" class="toggle toggle-success" />
              <span>{{ form.isActive ? '啟用' : '停用' }}</span>
            </label>
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
import PageHeader from '../components/PageHeader.vue'

type AppUser = {
  id: number
  username: string
  role: number
  displayName?: string | null
  email?: string | null
  tenantId?: number | null
  tenantName?: string | null
  isActive: boolean
  createdAtUtc: string
}

type TenantOption = { id: number; name: string }

const items = ref<AppUser[]>([])
const tenants = ref<TenantOption[]>([])
const keyword = ref('')
const listError = ref('')
const formError = ref('')
const showModal = ref(false)
const form = ref({ id: 0, username: '', role: 1, displayName: '', email: '', tenantId: 0, password: '', isActive: true })

const load = async () => {
  listError.value = ''
  try {
    const { data } = await api.get('/users', { params: { keyword: keyword.value || undefined } })
    items.value = data
  } catch (e: any) {
    listError.value = e?.response?.data || e?.message || '載入帳號資料失敗'
  }
}

const loadTenants = async () => {
  try {
    const { data } = await api.get('/tenants')
    tenants.value = data
  } catch {
    tenants.value = []
  }
}

onMounted(() => {
  load()
  loadTenants()
})

const formatUtc = (value?: string) => {
  if (!value) return ''
  return value.slice(0, 19).replace('T', ' ')
}

const reset = () => {
  form.value = { id: 0, username: '', role: 1, displayName: '', email: '', tenantId: 0, password: '', isActive: true }
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

const edit = (u: AppUser) => {
  form.value = {
    id: u.id,
    username: u.username,
    role: u.role,
    displayName: u.displayName || '',
    email: u.email || '',
    tenantId: u.tenantId || 0,
    password: '',
    isActive: u.isActive
  }
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
  if (form.value.role === 2 && !form.value.tenantId) {
    formError.value = '租客帳號必須關聯租客'
    return
  }

  const payload = {
    username: form.value.username,
    role: form.value.role,
    displayName: form.value.displayName || null,
    email: form.value.email || null,
    tenantId: form.value.role === 2 ? form.value.tenantId : null,
    password: form.value.password || null,
    isActive: form.value.isActive
  }
  try {
    if (form.value.id) await api.put(`/users/${form.value.id}`, payload)
    else await api.post('/users', payload)
    closeModal()
    await load()
  } catch (e: any) {
    formError.value = e?.response?.data || e?.message || '儲存失敗'
  }
}

const remove = async (id: number) => {
  listError.value = ''
  try {
    await api.delete(`/users/${id}`)
    await load()
  } catch (e: any) {
    listError.value = e?.response?.data || e?.message || '刪除失敗'
  }
}
</script>
