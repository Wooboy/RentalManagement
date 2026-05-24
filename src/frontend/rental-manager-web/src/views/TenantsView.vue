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
      <thead><tr><th>姓名</th><th>電話</th><th></th></tr></thead>
      <tbody>
        <tr v-for="t in items" :key="t.id">
          <td>{{ t.name }}</td><td>{{ t.phone }}</td>
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
const form = ref<any>({ id: 0, name: '', phone: '', type: 1 })
const load = async()=>{ const {data}=await api.get('/tenants',{params:{keyword:keyword.value||undefined}}); items.value=data }
onMounted(load)
const reset = ()=> { form.value = { id: 0, name: '', phone: '', type: 1 }; error.value='' }
const openCreateModal = ()=>{ reset(); showModal.value = true }
const closeModal = ()=>{ showModal.value = false; reset() }
const edit = (t:any)=> { form.value = { ...t }; error.value=''; showModal.value = true }
const save = async()=>{
  if (!String(form.value.name || '').trim()) { error.value = '請輸入姓名/公司'; return }
  if (form.value.id) await api.put(`/tenants/${form.value.id}`, form.value)
  else await api.post('/tenants', form.value)
  closeModal(); await load()
}
const remove = async(id:number)=>{ await api.delete(`/tenants/${id}`); await load() }
</script>
