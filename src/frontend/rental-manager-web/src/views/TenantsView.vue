<template>
  <div>
    <PageHeader title="租客名單" :subtitle="`共 ${items.length} 位租客`">
      <template #actions>
        <label class="input input-bordered input-sm flex items-center gap-2 w-56">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
          </svg>
          <input v-model="keyword" class="grow" placeholder="姓名 / 電話" @keyup.enter="load" />
        </label>
        <button class="btn btn-sm btn-ghost" @click="load">查詢</button>
        <button class="btn btn-sm btn-primary" @click="openCreateModal">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          新增租客
        </button>
      </template>
    </PageHeader>

    <div class="card bg-base-100 border border-base-300 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="table">
          <thead><tr><th>姓名</th><th>電話</th><th>生日</th><th>統編</th><th>備註</th><th class="text-right">操作</th></tr></thead>
          <tbody>
            <tr v-for="t in items" :key="t.id">
              <td class="font-medium">{{ t.name }}</td><td>{{ t.phone || '-' }}</td><td>{{ t.birthdayUtc?.slice(0,10) || '-' }}</td><td>{{ t.taxId || '-' }}</td><td class="max-w-60 truncate text-base-content/70">{{ t.notes || '-' }}</td>
              <td>
                <div class="flex gap-1 justify-end">
                  <button class="btn btn-xs btn-ghost" @click="edit(t)">編輯</button>
                  <button class="btn btn-xs btn-ghost text-error" @click="remove(t.id)">刪除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0">
              <td colspan="6" class="text-center text-base-content/50 py-10">尚無租客資料，點右上角「新增租客」開始建立。</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

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
import PageHeader from '../components/PageHeader.vue'
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
