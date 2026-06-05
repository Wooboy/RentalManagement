<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">合約管理</h2>
    <div class="flex gap-2 mb-3">
      <label class="form-control">
        <span class="label-text mb-1">搜尋關鍵字</span>
        <input v-model="keyword" class="input input-bordered" placeholder="房源" />
      </label>
      <label class="form-control">
        <span class="label-text mb-1">狀態</span>
        <select v-model="statusFilter" class="select select-bordered">
          <option value="all">全部</option>
          <option value="1">生效中</option>
          <option value="3">已終止</option>
          <option value="2">已到期</option>
        </select>
      </label>
      <button class="btn" @click="load">查詢</button>
      <button class="btn btn-primary" @click="openCreateModal">新增</button>
    </div>

    <div class="text-error text-sm mb-3">{{ error }}</div>

    <table class="table table-zebra">
      <thead><tr><th>租客</th><th>房源/房間</th><th>月租</th><th>付款間隔</th><th>每期應付</th><th>備註</th><th></th></tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.id">
          <td>{{ c.tenant?.name }}</td><td>{{ c.propertyName }}</td><td>{{ c.monthlyRent }}</td><td>{{ paymentIntervalText(c.paymentIntervalMonths) }}</td><td>{{ c.periodPayableAmount }}</td><td>{{ c.notes || '-' }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="edit(c)">編輯</button><button class="btn btn-sm btn-error" @click="remove(c.id)">刪除</button></td>
        </tr>
      </tbody>
    </table>

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-5xl">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯合約' : '新增合約' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
          <label class="form-control"><span class="label-text mb-1">租客 *</span><select v-model.number="form.tenantId" class="select select-bordered">
            <option :value="0">選擇租客 *</option>
            <option v-for="t in tenants" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">房源 *</span><select v-model.number="selectedPropertyId" class="select select-bordered" @change="onPropertyChange">
            <option :value="0">選擇房源 *</option>
            <option v-for="p in properties" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">月租 *</span><input v-model.number="form.monthlyRent" type="number" class="input input-bordered" placeholder="請輸入" /></label>
          <label class="form-control"><span class="label-text mb-1">付款間隔</span><select v-model.number="form.paymentIntervalMonths" class="select select-bordered">
            <option :value="1">每月</option><option :value="3">每季</option><option :value="12">每年</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">每期應付</span><input :value="periodPayableAmount" type="number" class="input input-bordered" disabled /></label>
        </div>

        <div class="mb-3">
          <p class="text-sm mb-2">選擇房間（可多選）*</p>
          <div class="flex flex-wrap gap-3">
            <label v-for="r in rooms" :key="r.id" class="label cursor-pointer gap-2">
              <input type="checkbox" class="checkbox checkbox-sm" :value="r.id" v-model="selectedRoomIds" />
              <span>{{ r.name }}</span>
            </label>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-4 gap-2">
          <label class="form-control"><span class="label-text mb-1">押金</span><input v-model.number="form.deposit" type="number" class="input input-bordered" placeholder="請輸入" /></label>
          <label class="form-control"><span class="label-text mb-1">居住人數 *</span><input v-model.number="form.occupantCount" type="number" class="input input-bordered" placeholder="請輸入" /></label>
          <label class="form-control"><span class="label-text mb-1">合約起始</span><input v-model="form.startDateUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">合約終止</span><input v-model="form.endDateUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">合約狀態</span><select v-model.number="form.status" class="select select-bordered">
            <option :value="1">狀態：生效中</option><option :value="2">狀態：已到期</option><option :value="3">狀態：已終止</option>
          </select></label>
          <label class="form-control md:col-span-3"><span class="label-text mb-1">備註</span><input v-model="form.notes" class="input input-bordered" placeholder="請輸入備註" /></label>
        </div>

        <div class="modal-action">
          <button class="btn" @click="closeModal">取消</button>
          <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
        </div>
      </div>
    </dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import api from '../services/api'
const items = ref<any[]>([])
const properties = ref<any[]>([])
const rooms = ref<any[]>([])
const tenants = ref<any[]>([])
const selectedPropertyId = ref(0)
const selectedRoomIds = ref<number[]>([])
const keyword = ref('')
const statusFilter = ref<'all' | '1' | '2' | '3'>('all')
const showModal = ref(false)
const error = ref('')
const toDateInput = (v: string) => (v ? v.slice(0, 10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()
const seed = ()=>({ id:0, contractNo:'', tenantId:0, startDateUtc:toDateInput(new Date().toISOString()), endDateUtc:toDateInput(new Date().toISOString()), monthlyRent:0, paymentIntervalMonths:1, deposit:0, occupantCount:1, notes:'', status:1 })
const form = ref<any>(seed())
const periodPayableAmount = computed(() => Number(form.value.monthlyRent || 0) * Number(form.value.paymentIntervalMonths || 1))
const paymentIntervalText = (m:number)=> m===12 ? '每年' : m===3 ? '每季' : '每月'
const load = async()=>{
  const params:any = { keyword: keyword.value || undefined }
  if (statusFilter.value !== 'all') params.status = Number(statusFilter.value)
  const {data}=await api.get('/contracts',{params})
  items.value=data
}
const loadProperties = async()=>{ const {data}=await api.get('/properties'); properties.value=data }
const loadRooms = async()=>{ if(!selectedPropertyId.value){ rooms.value=[]; return }; const {data}=await api.get('/rooms',{params:{propertyUnitId:selectedPropertyId.value}}); rooms.value=data }
const loadTenants = async()=>{ const {data}=await api.get('/tenants'); tenants.value=data }
onMounted(async()=>{ await Promise.all([load(), loadProperties(), loadTenants()]) })
const onPropertyChange = async()=>{ selectedRoomIds.value=[]; await loadRooms() }
const reset = ()=>{ form.value=seed(); selectedPropertyId.value=0; selectedRoomIds.value=[]; rooms.value=[]; error.value='' }
const openCreateModal = ()=>{ reset(); showModal.value = true }
const closeModal = ()=>{ showModal.value = false; reset() }
const edit = async(c:any)=>{ form.value={...c,startDateUtc:toDateInput(c.startDateUtc),endDateUtc:toDateInput(c.endDateUtc)}; selectedPropertyId.value=c.propertyUnitId||0; await loadRooms(); selectedRoomIds.value=c.propertyRoomIds||[]; error.value=''; showModal.value = true }
const validate = ()=>{ if(!form.value.tenantId) return '請選擇租客'; if(!selectedPropertyId.value) return '請選擇房源'; if(!selectedRoomIds.value.length) return '請至少選擇一間房間'; if(form.value.monthlyRent<0) return '月租不可小於0'; if(![1,3,12].includes(Number(form.value.paymentIntervalMonths||0))) return '付款間隔僅允許每月/每季/每年'; return '' }
const save = async()=>{ error.value=validate(); if(error.value) return; const payload={ contractNo:form.value.contractNo, tenantId:form.value.tenantId, propertyRoomIds:selectedRoomIds.value, startDateUtc:toIsoDate(form.value.startDateUtc), endDateUtc:toIsoDate(form.value.endDateUtc), monthlyRent:form.value.monthlyRent, paymentIntervalMonths:form.value.paymentIntervalMonths, deposit:form.value.deposit, occupantCount:form.value.occupantCount, notes:form.value.notes, electricityRuleType:3, status:form.value.status }; if(form.value.id) await api.put(`/contracts/${form.value.id}`,payload); else await api.post('/contracts',payload); closeModal(); await load() }
const remove = async(id:number)=>{ await api.delete(`/contracts/${id}`); await load() }
</script>
