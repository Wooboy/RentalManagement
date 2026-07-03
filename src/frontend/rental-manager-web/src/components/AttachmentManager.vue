<template>
  <dialog class="modal modal-open">
    <div class="modal-box max-w-2xl">
      <h3 class="font-bold text-lg mb-1">附件管理</h3>
      <p v-if="subtitle" class="text-sm text-base-content/60 mb-3">{{ subtitle }}</p>

      <div class="flex items-center gap-2 mb-3">
        <input
          ref="fileInput"
          type="file"
          class="file-input file-input-bordered file-input-sm flex-1"
          accept=".jpg,.jpeg,.png,.webp,.heic,.pdf"
        />
        <button class="btn btn-sm btn-primary" :disabled="uploading" @click="upload">
          <span v-if="uploading" class="loading loading-spinner loading-xs"></span>
          上傳
        </button>
      </div>

      <p v-if="error" class="text-error text-sm mb-2">{{ error }}</p>

      <p v-if="!loading && items.length === 0" class="text-sm text-base-content/60 py-6 text-center">
        尚無附件
      </p>
      <table v-else class="table table-sm">
        <thead>
          <tr>
            <th>檔名</th>
            <th>大小</th>
            <th>上傳者</th>
            <th>上傳時間</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in items" :key="a.id">
            <td class="max-w-56 truncate">{{ a.fileName }}</td>
            <td class="whitespace-nowrap">{{ formatSize(a.fileSize) }}</td>
            <td>{{ a.uploadedByName || '-' }}</td>
            <td class="whitespace-nowrap">{{ a.createdAtUtc?.slice(0, 16).replace('T', ' ') }}</td>
            <td class="flex gap-1 justify-end">
              <button class="btn btn-xs" @click="download(a)">下載</button>
              <button class="btn btn-xs btn-error" @click="remove(a.id)">刪除</button>
            </td>
          </tr>
        </tbody>
      </table>

      <div class="modal-action">
        <button class="btn" @click="emit('close')">關閉</button>
      </div>
    </div>
    <div class="modal-backdrop" @click="emit('close')"></div>
  </dialog>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'

type AttachmentItem = {
  id: number
  fileName: string
  contentType: string
  fileSize: number
  uploadedByName?: string | null
  createdAtUtc: string
}

const props = defineProps<{
  /** 對應後端 AttachmentEntityType：1 合約、2 應收、3 支出、4 報修 */
  entityType: number
  entityId: number
  subtitle?: string
}>()

const emit = defineEmits<{ close: [] }>()

const items = ref<AttachmentItem[]>([])
const loading = ref(false)
const uploading = ref(false)
const error = ref('')
const fileInput = ref<HTMLInputElement>()

const formatSize = (bytes: number) => {
  if (bytes >= 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(0)} KB`
  return `${bytes} B`
}

const load = async () => {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get('/attachments', {
      params: { entityType: props.entityType, entityId: props.entityId }
    })
    items.value = data
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入附件失敗'
  } finally {
    loading.value = false
  }
}

const upload = async () => {
  const file = fileInput.value?.files?.[0]
  if (!file) {
    error.value = '請先選擇檔案'
    return
  }
  uploading.value = true
  error.value = ''
  try {
    const formData = new FormData()
    formData.append('entityType', String(props.entityType))
    formData.append('entityId', String(props.entityId))
    formData.append('file', file)
    await api.post('/attachments', formData)
    if (fileInput.value) fileInput.value.value = ''
    await load()
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '上傳失敗'
  } finally {
    uploading.value = false
  }
}

const download = async (a: AttachmentItem) => {
  error.value = ''
  try {
    const { data } = await api.get(`/attachments/${a.id}/download`, { responseType: 'blob' })
    const url = URL.createObjectURL(data)
    const link = document.createElement('a')
    link.href = url
    link.download = a.fileName
    link.click()
    URL.revokeObjectURL(url)
  } catch (e: any) {
    error.value = '下載失敗'
  }
}

const remove = async (id: number) => {
  error.value = ''
  try {
    await api.delete(`/attachments/${id}`)
    await load()
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '刪除失敗'
  }
}

onMounted(load)
</script>
