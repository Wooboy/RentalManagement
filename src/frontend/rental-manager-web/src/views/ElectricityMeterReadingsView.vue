<template>
  <div class="card bg-base-100 shadow p-4 space-y-3">
    <h2 class="text-lg font-bold">電錶抄表</h2>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
      <label class="form-control">
        <span class="label-text mb-1">房源篩選</span>
        <select v-model.number="selectedPropertyUnitId" class="select select-bordered" @change="loadLatestRows">
          <option :value="0">全部房源</option>
          <option v-for="p in properties" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </label>
      <label class="form-control">
        <span class="label-text mb-1">抄表日期</span>
        <input v-model="readingDate" type="date" max="2099-12-31" class="input input-bordered" />
      </label>
      <div class="form-control justify-end">
        <button class="btn mt-6" @click="loadLatestRows">重新載入</button>
      </div>
    </div>

    <table class="table table-zebra">
      <thead>
        <tr>
          <th>房源</th>
          <th>房間</th>
          <th>上次抄表日</th>
          <th>上次度數</th>
          <th>目前度數</th>
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
    <h3 class="font-semibold">抄表紀錄</h3>
    <table class="table table-zebra">
      <thead>
        <tr>
          <th>日期</th>
          <th>房源</th>
          <th>房間</th>
          <th>度數</th>
          <th>備註</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="h in history" :key="h.id">
          <td>{{ h.readingDateUtc?.slice(0,10) }}</td>
          <td>{{ h.propertyUnitName || h.propertyUnitId }}</td>
          <td>{{ h.propertyRoomName }}</td>
          <td>{{ h.readingValue }}</td>
          <td>{{ h.notes || '-' }}</td>
        </tr>
      </tbody>
    </table>

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

const saveOne = async (row:any) => {
  error.value = ''
  notice.value = ''
  if (row.currentReadingValue == null || Number(row.currentReadingValue) < 0) {
    error.value = '請輸入正確的目前度數'
    return
  }
  try {
    await api.post('/electricity-meter-readings', {
      propertyUnitId: row.propertyUnitId,
      propertyRoomId: row.propertyRoomId,
      readingDateUtc: new Date(`${readingDate.value}T00:00:00Z`).toISOString(),
      readingValue: Number(row.currentReadingValue),
      notes: row.notes || null
    })
    notice.value = `已儲存：${row.propertyUnitName || ''} ${row.propertyRoomName}`
    await Promise.all([loadLatestRows(), loadHistory()])
  } catch (e:any) {
    error.value = e?.response?.data || e?.message || '儲存失敗'
  }
}

onMounted(async () => {
  await loadProperties()
  await Promise.all([loadLatestRows(), loadHistory()])
})
</script>
