<template>
  <div class="card bg-base-100 shadow p-4 space-y-3">
    <h2 class="text-lg font-bold">電錶抄表</h2>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
      <label class="form-control">
        <span class="label-text mb-1">房屋</span>
        <select v-model.number="selectedPropertyUnitId" class="select select-bordered" @change="reload">
          <option :value="0">全部房屋</option>
          <option v-for="property in properties" :key="property.id" :value="property.id">{{ property.name }}</option>
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
          <th>房屋</th>
          <th>房間</th>
          <th>最近抄表日</th>
          <th>最近抄表度數</th>
          <th>本次抄表度數</th>
          <th>備註</th>
          <th class="w-40"></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.propertyRoomId">
          <td>{{ row.propertyUnitName || row.propertyUnitId }}</td>
          <td>{{ row.propertyRoomName }}</td>
          <td>{{ row.lastReadingDateUtc ? row.lastReadingDateUtc.slice(0, 10) : '-' }}</td>
          <td>{{ row.lastReadingValue ?? '-' }}</td>
          <td>
            <input v-model.number="row.currentReadingValue" type="number" class="input input-bordered input-sm w-36" />
          </td>
          <td>
            <input v-model="row.notes" class="input input-bordered input-sm w-52" />
          </td>
          <td>
            <div class="flex gap-2 justify-end">
              <button class="btn btn-sm" @click="openHistoryModal(row)">記錄</button>
              <button class="btn btn-sm btn-primary" @click="saveOne(row)">儲存</button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>

    <dialog class="modal" :class="{ 'modal-open': showHistoryModal }">
      <div class="modal-box max-w-4xl">
        <div class="flex items-start justify-between gap-4 mb-3">
          <div>
            <h3 class="font-bold text-lg">抄錶記錄</h3>
            <p class="text-sm opacity-70">
              {{ historyRoomLabel || '-' }}
            </p>
          </div>
          <button class="btn btn-sm" @click="closeHistoryModal">關閉</button>
        </div>

        <div v-if="historyLoading" class="py-6 text-center opacity-70">載入中...</div>
        <template v-else>
          <div v-if="history.length === 0" class="py-6 text-center opacity-70">尚無抄錶記錄</div>
          <table v-else class="table table-zebra">
            <thead>
              <tr>
                <th>日期</th>
                <th>度數</th>
                <th>備註</th>
                <th class="w-24"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in history" :key="item.id">
                <td>{{ item.readingDateUtc?.slice(0, 10) }}</td>
                <td>{{ item.readingValue }}</td>
                <td>{{ item.notes || '-' }}</td>
                <td>
                  <button class="btn btn-sm" @click="openEditModal(item)">編輯</button>
                </td>
              </tr>
            </tbody>
          </table>
        </template>
      </div>
    </dialog>

    <dialog class="modal" :class="{ 'modal-open': showEditModal }">
      <div class="modal-box max-w-2xl">
        <h3 class="font-bold text-lg mb-3">編輯抄錶記錄</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
          <label class="form-control">
            <span class="label-text mb-1">房屋</span>
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
import { computed, onMounted, ref } from 'vue'
import api from '../services/api'

type LatestRow = {
  propertyUnitId: number
  propertyUnitName: string
  propertyRoomId: number
  propertyRoomName: string
  lastReadingDateUtc: string | null
  lastReadingValue: number | null
  currentReadingValue: number
  notes: string
}

type MeterReading = {
  id: number
  propertyUnitId: number
  propertyUnitName: string
  propertyRoomId: number
  propertyRoomName: string
  readingDateUtc: string
  readingValue: number
  notes: string | null
}

type EditForm = {
  id: number
  propertyUnitId: number
  propertyUnitName: string
  propertyRoomId: number
  propertyRoomName: string
  readingDateUtc: string
  readingValue: number
  notes: string
}

const properties = ref<any[]>([])
const selectedPropertyUnitId = ref(0)
const rows = ref<LatestRow[]>([])
const history = ref<MeterReading[]>([])
const error = ref('')
const notice = ref('')
const readingDate = ref(new Date().toISOString().slice(0, 10))
const showHistoryModal = ref(false)
const historyLoading = ref(false)
const historyRoom = ref<LatestRow | null>(null)
const showEditModal = ref(false)
const editError = ref('')
const editForm = ref<EditForm>(createEditSeed())

function createEditSeed(): EditForm {
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

const historyRoomLabel = computed(() => {
  if (!historyRoom.value) return ''
  return `${historyRoom.value.propertyUnitName || historyRoom.value.propertyUnitId} / ${historyRoom.value.propertyRoomName}`
})

const toDateInput = (value: string) => (value ? value.slice(0, 10) : '')
const toIsoDate = (value: string) => new Date(`${value}T00:00:00Z`).toISOString()

const loadProperties = async () => {
  const { data } = await api.get('/properties')
  properties.value = data
}

const loadLatestRows = async () => {
  const { data } = await api.get('/electricity-meter-readings/latest-by-room', {
    params: { propertyUnitId: selectedPropertyUnitId.value || undefined }
  })
  rows.value = data.map((item: any) => ({
    ...item,
    currentReadingValue: item.lastReadingValue ?? 0,
    notes: ''
  }))
}

const loadHistoryForRoom = async (propertyRoomId: number) => {
  historyLoading.value = true
  try {
    const { data } = await api.get('/electricity-meter-readings', {
      params: {
        propertyUnitId: selectedPropertyUnitId.value || undefined,
        propertyRoomId
      }
    })
    history.value = data
  } finally {
    historyLoading.value = false
  }
}

const reload = async () => {
  await loadLatestRows()
  if (showHistoryModal.value && historyRoom.value) {
    const matchedRoom = rows.value.find((item) => item.propertyRoomId === historyRoom.value?.propertyRoomId) ?? historyRoom.value
    historyRoom.value = matchedRoom
    await loadHistoryForRoom(historyRoom.value.propertyRoomId)
  }
}

const saveOne = async (row: LatestRow) => {
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
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '儲存失敗'
  }
}

const openHistoryModal = async (row: LatestRow) => {
  error.value = ''
  notice.value = ''
  historyRoom.value = row
  showHistoryModal.value = true
  await loadHistoryForRoom(row.propertyRoomId)
}

const closeHistoryModal = () => {
  showHistoryModal.value = false
  historyRoom.value = null
  history.value = []
  historyLoading.value = false
}

const openEditModal = (row: MeterReading) => {
  editError.value = ''
  editForm.value = {
    id: row.id,
    propertyUnitId: row.propertyUnitId,
    propertyUnitName: row.propertyUnitName || String(row.propertyUnitId),
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
  } catch (e: any) {
    editError.value = e?.response?.data || e?.message || '更新失敗'
  }
}

onMounted(async () => {
  await loadProperties()
  await reload()
})
</script>
