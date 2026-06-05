<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">租客管理</h2>
    <div class="flex flex-wrap items-end gap-3 mb-3">
      <label class="form-control min-w-64">
        <span class="label-text mb-1">搜尋關鍵字</span>
        <input v-model="keyword" class="input input-bordered" placeholder="姓名/電話" />
      </label>
      <button class="btn" @click="load">查詢</button>
      <button class="btn btn-primary" @click="openCreateModal">新增</button>
    </div>

    <table class="table table-zebra">
      <thead><tr><th>姓名</th><th>電話</th><th>生日</th><th>統編</th><th>備註</th><th></th></tr></thead>
      <tbody>
        <tr v-for="t in items" :key="t.id">
          <td>{{ t.name }}</td><td>{{ t.phone }}</td><td>{{ t.birthdayUtc?.slice(0,10) || '-' }}</td><td>{{ t.taxId || '-' }}</td><td>{{ t.notes || '-' }}</td>
          <td class="flex gap-2 justify-end">
            <button class="btn btn-sm" @click="edit(t)">編輯</button>
            <button class="btn btn-sm btn-error" @click="remove(t.id)">刪除</button>
          </td>
        </tr>
      </tbody>
    </table>

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-5xl max-h-[85vh] overflow-y-auto">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯租客' : '新增租客' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <label class="form-control">
            <span class="label-text mb-1">姓名/公司</span>
            <input v-model="form.name" class="input input-bordered" placeholder="請輸入" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">電話</span>
            <input v-model="form.phone" class="input input-bordered" placeholder="請輸入" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">生日</span>
            <input v-model="form.birthdayUtc" type="date" max="2099-12-31" class="input input-bordered" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">統編</span>
            <input v-model="form.taxId" class="input input-bordered" placeholder="請輸入" />
          </label>
          <label class="form-control md:col-span-4">
            <span class="label-text mb-1">備註</span>
            <input v-model="form.notes" class="input input-bordered" placeholder="請輸入" />
          </label>
        </div>
        <p class="text-error text-sm mt-3">{{ error }}</p>
        <div class="modal-action">
          <button class="btn" @click="closeModal">取消</button>
          <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
        </div>
      </div>
    </dialog>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
const items = ref<any[]>([])
const keyword = ref('')
const showModal = ref(false)
const error = ref('')
const toDateInput = (v: string) => (v ? v.slice(0, 10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()
const seed = ()=>({ id: 0, name: '', phone: '', birthdayUtc: '', taxId: '', notes: '', type: 1 })
const form = ref<any>(seed())
const load = async()=>{ const {data}=await api.get('/tenants',{params:{keyword:keyword.value||undefined}}); items.value=data }
onMounted(load)
const reset = ()=> { form.value = seed(); error.value='' }
const openCreateModal = ()=>{ reset(); showModal.value = true }
const closeModal = ()=>{ showModal.value = false; reset() }
const edit = (t:any)=> { form.value = { ...t, birthdayUtc: toDateInput(t.birthdayUtc), taxId: t.taxId ?? '', notes: t.notes ?? '' }; error.value=''; showModal.value = true }
const save = async()=>{
  if (!String(form.value.name || '').trim()) { error.value = '請輸入姓名/公司'; return }
  const payload = { ...form.value, birthdayUtc: form.value.birthdayUtc ? toIsoDate(form.value.birthdayUtc) : null }
  if (form.value.id) await api.put(`/tenants/${form.value.id}`, payload)
  else await api.post('/tenants', payload)
  closeModal(); await load()
}
const remove = async(id:number)=>{ await api.delete(`/tenants/${id}`); await load() }
</script>
