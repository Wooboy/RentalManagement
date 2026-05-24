<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">合約管理</h2>

    <div class="flex gap-2 mb-3">
      <input v-model="keyword" class="input input-bordered" placeholder="搜尋合約編號/房源" />
      <button class="btn" @click="load">查詢</button>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
      <input v-model="form.contractNo" class="input input-bordered" placeholder="合約編號 *" />
      <select v-model.number="form.tenantId" class="select select-bordered">
        <option :value="0">選擇租客 *</option>
        <option v-for="t in tenants" :key="t.id" :value="t.id">{{ t.name }}</option>
      </select>
      <select v-model.number="selectedPropertyId" class="select select-bordered" @change="onPropertyChange">
        <option :value="0">選擇房源 *</option>
        <option v-for="p in properties" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
      </select>
      <select v-model.number="selectedRoomId" class="select select-bordered" @change="bindRoom">
        <option :value="0">選擇房間 *</option>
        <option v-for="r in rooms" :key="r.id" :value="r.id">{{ r.code }} - {{ r.name }}</option>
      </select>

      <input v-model.number="form.monthlyRent" type="number" class="input input-bordered" placeholder="月租 *" />
      <input v-model.number="form.deposit" type="number" class="input input-bordered" placeholder="押金" />
      <input v-model.number="form.occupantCount" type="number" class="input input-bordered" placeholder="居住人數 *" />
      <input v-model="form.startDateUtc" type="date" class="input input-bordered" />

      <input v-model="form.endDateUtc" type="date" class="input input-bordered" />
      <select v-model.number="form.electricityRuleType" class="select select-bordered">
        <option :value="1">電費規則：依度數</option>
        <option :value="2">電費規則：平均</option>
        <option :value="3">電費規則：多錶</option>
      </select>
      <select v-model.number="form.status" class="select select-bordered">
        <option :value="1">狀態：生效中</option>
        <option :value="2">狀態：已到期</option>
        <option :value="3">狀態：已終止</option>
      </select>
      <input v-model="form.propertyName" class="input input-bordered" placeholder="房源/房間（自動帶入）" disabled />
    </div>

    <div class="flex gap-2 mb-3">
      <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
      <button v-if="form.id" class="btn" @click="reset">取消</button>
      <span class="text-error text-sm self-center">{{ error }}</span>
    </div>

    <table class="table table-zebra">
      <thead><tr><th>合約編號</th><th>租客</th><th>房源/房間</th><th>月租</th><th></th></tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.id">
          <td>{{ c.contractNo }}</td>
          <td>{{ c.tenant?.name }}</td>
          <td>{{ c.propertyName }}</td>
          <td>{{ c.monthlyRent }}</td>
          <td class="flex gap-2 justify-end">
            <button class="btn btn-sm" @click="edit(c)">編輯</button>
            <button class="btn btn-sm btn-error" @click="remove(c.id)">刪除</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'

const items = ref<any[]>([])
const properties = ref<any[]>([])
const rooms = ref<any[]>([])
const tenants = ref<any[]>([])
const selectedPropertyId = ref(0)
const selectedRoomId = ref(0)
const keyword = ref('')
const error = ref('')

const toDateInput = (v: string) => (v ? v.slice(0, 10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()

const seed = () => ({
  id: 0,
  contractNo: '',
  tenantId: 0,
  propertyUnitId: null,
  propertyRoomId: null,
  propertyName: '',
  propertyAddress: '',
  startDateUtc: toDateInput(new Date().toISOString()),
  endDateUtc: toDateInput(new Date().toISOString()),
  monthlyRent: 0,
  deposit: 0,
  occupantCount: 1,
  electricityRuleType: 1,
  status: 1
})

const form = ref<any>(seed())

const load = async () => {
  const { data } = await api.get('/contracts', { params: { keyword: keyword.value || undefined } })
  items.value = data
}
const loadProperties = async () => {
  const { data } = await api.get('/properties')
  properties.value = data
}
const loadRooms = async () => {
  if (!selectedPropertyId.value) { rooms.value = []; return }
  const { data } = await api.get('/rooms', { params: { propertyUnitId: selectedPropertyId.value } })
  rooms.value = data
}
const loadTenants = async () => {
  const { data } = await api.get('/tenants')
  tenants.value = data
}

onMounted(async () => {
  await Promise.all([load(), loadProperties(), loadTenants()])
})

const reset = () => {
  form.value = seed()
  selectedPropertyId.value = 0
  selectedRoomId.value = 0
  rooms.value = []
  error.value = ''
}

const edit = async (c: any) => {
  form.value = {
    ...c,
    startDateUtc: toDateInput(c.startDateUtc),
    endDateUtc: toDateInput(c.endDateUtc)
  }
  selectedPropertyId.value = c.propertyUnitId ?? 0
  await loadRooms()
  selectedRoomId.value = c.propertyRoomId ?? 0
  error.value = ''
}

const onPropertyChange = async () => {
  selectedRoomId.value = 0
  form.value.propertyRoomId = null
  form.value.propertyUnitId = selectedPropertyId.value || null
  await loadRooms()
}

const bindRoom = () => {
  const room = rooms.value.find((x: any) => x.id === selectedRoomId.value)
  const p = properties.value.find((x: any) => x.id === selectedPropertyId.value)
  if (!room || !p) return
  form.value.propertyRoomId = room.id
  form.value.propertyUnitId = p.id
  form.value.propertyName = `${p.name}-${room.name || room.code}`
  form.value.propertyAddress = p.address
}

const validate = () => {
  if (!form.value.contractNo.trim()) return '請輸入合約編號'
  if (!form.value.tenantId) return '請選擇租客'
  if (!selectedPropertyId.value) return '請選擇房源'
  if (!selectedRoomId.value) return '請選擇房間'
  if (form.value.monthlyRent < 0) return '月租不可小於 0'
  if (form.value.occupantCount <= 0) return '居住人數需大於 0'
  if (!form.value.startDateUtc || !form.value.endDateUtc) return '請選擇起訖日'
  return ''
}

const save = async () => {
  error.value = validate()
  if (error.value) return

  const payload = {
    ...form.value,
    propertyUnitId: selectedPropertyId.value,
    propertyRoomId: selectedRoomId.value,
    startDateUtc: toIsoDate(form.value.startDateUtc),
    endDateUtc: toIsoDate(form.value.endDateUtc)
  }

  if (form.value.id) await api.put(`/contracts/${form.value.id}`, payload)
  else await api.post('/contracts', payload)

  reset()
  await load()
}

const remove = async (id: number) => {
  await api.delete(`/contracts/${id}`)
  await load()
}
</script>
