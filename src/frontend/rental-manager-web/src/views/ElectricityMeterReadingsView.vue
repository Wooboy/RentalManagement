<template>
  <div class="card bg-base-100 shadow p-4 space-y-3">
    <h2 class="text-lg font-bold">電錶抄表</h2>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
      <label class="form-control">
        <span class="label-text mb-1">房源篩選</span>
        <select v-model.number="selectedPropertyUnitId" class="select select-bordered" @change="reload">
          <option :value="0">全部房源</option>
          <option v-for="p in properties" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </label>
      <label class="form-control">
        <span class="label-text mb-1">本次抄表日期</span>
        <input v-model="readingDate" type="date" max="2099-12-31" class="input input-bordered" />
      </label>
      <div class="form-control justify-end">
        <button class="btn mt-6" @click="reload">重新整理</button>
      </div>
    </div>

    <table class="table table-zebra">
      <thead>
        <tr>
          <th>房源</th>
          <th>房間</th>
          <th>最近抄表日</th>
          <th>最近度數</th>
          <th>本次度數</th>
          <th>備註</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r.propertyRoomId">
          <td>{{ r.propertyUnitName || r.propertyUnitId }}</td>
          <td>{{ r.propertyRoomName }}</td>
          <td>{{ r.lastReadingDateUtc ? r.lastReadingDateUtc.slice(0,10) : '-' }}</td>
          <td>{{ r.lastReadingValue ?? '-' }}</td>
          <td><input v-model.number="r.currentReadingValue" type="number" class="input input-bordered input-sm w-36" /></td>
          <td><input v-model="r.notes" class="input input-bordered input-sm w-52" /></td>
          <td><button class="btn btn-sm btn-primary" @click="saveOne(r)">儲存</button></td>
        </tr>
      </tbody>
    </table>

    <div class="divider my-1"></div>
    <h3 class="font-semibold">抄表歷史</h3>
    <table class="table table-zebra">
      <thead>
        <tr>
          <th>日期</th>
          <th>房源</th>
          <th>房間</th>
          <th>度數</th>
          <th>備註</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="h in history" :key="h.id">
          <td>{{ h.readingDateUtc?.slice(0,10) }}</td>
          <td>{{ h.propertyUnitName || h.propertyUnitId }}</td>
          <td>{{ h.propertyRoomName }}</td>
          <td>{{ h.readingValue }}</td>
          <td>{{ h.notes || '-' }}</td>
          <td><button class="btn btn-sm" @click="openEditModal(h)">編輯</button></td>
        </tr>
      </tbody>
    </table>

    <dialog class="modal" :class="{ 'modal-open': showEditModal }">
      <div class="modal-box max-w-2xl">
        <h3 class="font-bold text-lg mb-3">編輯抄表記錄</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
          <label class="form-control">
            <span class="label-text mb-1">房源</span>
            <input :value="editForm.propertyUnitName" class="input input-bordered" disabled />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">房間</span>
            <input :value="editForm.propertyRoomName" class="input input-bordered" disabled />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">抄表日期</span>
            <input v-model="editForm.readingDateUtc" type="date" max="2099-12-31" class="input input-bordered" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">抄表度數</span>
            <input v-model.number="editForm.readingValue" type="number" class="input input-bordered" />
          </label>
          <label class="form-control md:col-span-2">
            <span class="label-text mb-1">備註</span>
            <input v-model="editForm.notes" class="input input-bordered" />
          </label>
        </div>
        <p class="text-error text-sm mt-3">{{ editError }}</p>
        <div class="modal-action">
          <button class="btn" @click="closeEditModal">取消</button>
          <button class="btn btn-primary" @click="updateOne">更新</button>
        </div>
      </div>
    </dialog>

    <p class="text-error text-sm">{{ error }}</p>
    <p class="text-success text-sm">{{ notice }}</p>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'

const properties = ref<any[]>([])
const selectedPropertyUnitId = ref(0)
const rows = ref<any[]>([])
const history = ref<any[]>([])
const error = ref('')
const notice = ref('')
const readingDate = ref(new Date().toISOString().slice(0, 10))
const showEditModal = ref(false)
const editError = ref('')
const editForm = ref<any>(createEditSeed())

function createEditSeed() {
  return {
    id: 0,
    propertyUnitId: 0,
    propertyUnitName: '',
    propertyRoomId: 0,
    propertyRoomName: '',
    readingDateUtc: '',
    readingValue: 0,
    notes: ''
  }
}

const toDateInput = (value: string) => value ? value.slice(0, 10) : ''
const toIsoDate = (value: string) => new Date(`${value}T00:00:00Z`).toISOString()

const loadProperties = async () => {
  const { data } = await api.get('/properties')
  properties.value = data
}

const loadLatestRows = async () => {
  const { data } = await api.get('/electricity-meter-readings/latest-by-room', {
    params: { propertyUnitId: selectedPropertyUnitId.value || undefined }
  })
  rows.value = data.map((x:any) => ({
    ...x,
    currentReadingValue: x.lastReadingValue ?? 0,
    notes: ''
  }))
}

const loadHistory = async () => {
  const { data } = await api.get('/electricity-meter-readings', {
    params: { propertyUnitId: selectedPropertyUnitId.value || undefined }
  })
  history.value = data
}

const reload = async () => {
  await Promise.all([loadLatestRows(), loadHistory()])
}

const saveOne = async (row:any) => {
  error.value = ''
  notice.value = ''
  if (row.currentReadingValue == null || Number(row.currentReadingValue) < 0) {
    error.value = '請輸入有效的抄表度數'
    return
  }
  try {
    await api.post('/electricity-meter-readings', {
      propertyUnitId: row.propertyUnitId,
      propertyRoomId: row.propertyRoomId,
      readingDateUtc: toIsoDate(readingDate.value),
      readingValue: Number(row.currentReadingValue),
      notes: row.notes || null
    })
    notice.value = `已儲存 ${row.propertyUnitName || ''} ${row.propertyRoomName} 的抄表記錄`
    await reload()
  } catch (e:any) {
    error.value = e?.response?.data || e?.message || '儲存失敗'
  }
}

const openEditModal = (row:any) => {
  editError.value = ''
  editForm.value = {
    id: row.id,
    propertyUnitId: row.propertyUnitId,
    propertyUnitName: row.propertyUnitName || row.propertyUnitId,
    propertyRoomId: row.propertyRoomId,
    propertyRoomName: row.propertyRoomName,
    readingDateUtc: toDateInput(row.readingDateUtc),
    readingValue: Number(row.readingValue || 0),
    notes: row.notes || ''
  }
  showEditModal.value = true
}

const closeEditModal = () => {
  showEditModal.value = false
  editError.value = ''
  editForm.value = createEditSeed()
}

const updateOne = async () => {
  editError.value = ''
  error.value = ''
  notice.value = ''
  if (!editForm.value.id) return
  if (!editForm.value.readingDateUtc) {
    editError.value = '請輸入抄表日期'
    return
  }
  if (editForm.value.readingValue == null || Number(editForm.value.readingValue) < 0) {
    editError.value = '請輸入有效的抄表度數'
    return
  }
  try {
    await api.put(`/electricity-meter-readings/${editForm.value.id}`, {
      propertyUnitId: editForm.value.propertyUnitId,
      propertyRoomId: editForm.value.propertyRoomId,
      readingDateUtc: toIsoDate(editForm.value.readingDateUtc),
      readingValue: Number(editForm.value.readingValue),
      notes: editForm.value.notes || null
    })
    notice.value = `已更新 ${editForm.value.propertyRoomName} 的抄表記錄`
    closeEditModal()
    await reload()
  } catch (e:any) {
    editError.value = e?.response?.data || e?.message || '更新失敗'
  }
}

onMounted(async () => {
  await loadProperties()
  await reload()
})
</script>
