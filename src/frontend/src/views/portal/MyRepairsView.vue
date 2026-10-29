<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <h2 class="text-lg font-bold">線上報修</h2>
      <button class="btn btn-primary btn-sm" @click="openCreateModal">我要報修</button>
    </div>

    <p v-if="error" class="text-error text-sm">{{ error }}</p>
    <p v-if="!loading && items.length === 0" class="text-sm text-base-content/60 py-8 text-center">
      目前沒有報修紀錄，點「我要報修」建立第一筆
    </p>

    <div v-for="t in items" :key="t.id" class="card bg-base-100 shadow cursor-pointer hover:shadow-md transition-shadow" @click="openDetail(t.id)">
      <div class="card-body p-4">
        <div class="flex items-start justify-between gap-2">
          <div>
            <h3 class="font-semibold">{{ t.title }}</h3>
            <p class="text-sm text-base-content/60">
              {{ t.propertyUnitName || '' }}{{ t.propertyRoomName ? ` / ${t.propertyRoomName}` : '' }}
              · {{ t.createdAtUtc?.slice(0, 10) }}
            </p>
          </div>
          <div class="flex gap-1">
            <span class="badge badge-sm" :class="priorityBadge(t.priority)">{{ priorityText(t.priority) }}</span>
            <span class="badge badge-sm" :class="statusBadge(t.status)">{{ statusText(t.status) }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 建立報修 -->
    <dialog class="modal" :class="{ 'modal-open': showCreateModal }">
      <div class="modal-box max-w-lg">
        <h3 class="font-bold text-lg mb-3">我要報修</h3>
        <div class="space-y-3">
          <label class="form-control">
            <span class="label-text mb-1">合約 *</span>
            <select v-model.number="form.contractId" class="select select-bordered" @change="form.propertyRoomId = 0">
              <option :value="0" disabled>請選擇合約</option>
              <option v-for="c in contracts" :key="c.id" :value="c.id">{{ c.contractName || c.contractNo }}</option>
            </select>
          </label>
          <label class="form-control">
            <span class="label-text mb-1">房間（可選）</span>
            <select v-model.number="form.propertyRoomId" class="select select-bordered">
              <option :value="0">不指定房間</option>
              <option v-for="r in selectedContractRooms" :key="r.propertyRoomId" :value="r.propertyRoomId">
                {{ r.roomName || r.roomCode }}
              </option>
            </select>
          </label>
          <label class="form-control">
            <span class="label-text mb-1">標題 *</span>
            <input v-model="form.title" class="input input-bordered" placeholder="例如：浴室水龍頭漏水" />
          </label>
          <label class="form-control">
            <span class="label-text mb-1">問題描述 *</span>
            <textarea v-model="form.description" class="textarea textarea-bordered" rows="4" placeholder="請描述問題狀況、發生位置與時間"></textarea>
          </label>
          <label class="form-control">
            <span class="label-text mb-1">緊急程度</span>
            <select v-model.number="form.priority" class="select select-bordered">
              <option :value="1">低</option>
              <option :value="2">一般</option>
              <option :value="3">高</option>
              <option :value="4">緊急</option>
            </select>
          </label>
        </div>
        <p class="text-error text-sm mt-3">{{ formError }}</p>
        <div class="modal-action">
          <button class="btn" @click="showCreateModal = false">取消</button>
          <button class="btn btn-primary" :disabled="saving" @click="save">
            <span v-if="saving" class="loading loading-spinner loading-xs"></span>
            送出報修
          </button>
        </div>
      </div>
    </dialog>

    <!-- 報修明細 -->
    <dialog class="modal" :class="{ 'modal-open': !!detail }">
      <div v-if="detail" class="modal-box max-w-2xl max-h-[85vh] overflow-y-auto">
        <div class="flex items-start justify-between gap-2">
          <h3 class="font-bold text-lg">{{ detail.ticket.title }}</h3>
          <span class="badge" :class="statusBadge(detail.ticket.status)">{{ statusText(detail.ticket.status) }}</span>
        </div>
        <p class="text-sm text-base-content/60 mt-1">
          {{ detail.ticket.propertyUnitName || '' }}{{ detail.ticket.propertyRoomName ? ` / ${detail.ticket.propertyRoomName}` : '' }}
          · 建立於 {{ detail.ticket.createdAtUtc?.slice(0, 16).replace('T', ' ') }}
        </p>
        <p class="mt-3 whitespace-pre-wrap">{{ detail.ticket.description }}</p>

        <div class="flex gap-2 mt-3">
          <button class="btn btn-sm" @click="showAttachments = true">照片/附件</button>
        </div>

        <div class="divider my-2">處理紀錄</div>
        <p v-if="detail.comments.length === 0" class="text-sm text-base-content/60 text-center py-2">尚無留言</p>
        <div class="space-y-2">
          <div
            v-for="c in detail.comments"
            :key="c.id"
            class="chat"
            :class="c.isAdmin ? 'chat-start' : 'chat-end'"
          >
            <div class="chat-header text-xs text-base-content/60">
              {{ c.userName || (c.isAdmin ? '管理員' : '我') }} · {{ c.createdAtUtc?.slice(0, 16).replace('T', ' ') }}
            </div>
            <div class="chat-bubble chat-bubble-sm whitespace-pre-wrap" :class="c.isAdmin ? '' : 'chat-bubble-primary'">
              {{ c.content }}
            </div>
          </div>
        </div>

        <div class="flex gap-2 mt-4">
          <input v-model="newComment" class="input input-bordered input-sm flex-1" placeholder="輸入留言..." @keyup.enter="addComment" />
          <button class="btn btn-sm btn-primary" @click="addComment">送出</button>
        </div>
        <p class="text-error text-sm mt-2">{{ detailError }}</p>

        <div class="modal-action">
          <button class="btn" @click="detail = null">關閉</button>
        </div>
      </div>
    </dialog>

    <AttachmentManager
      v-if="detail && showAttachments"
      :entity-type="4"
      :entity-id="detail.ticket.id"
      :subtitle="`報修：${detail.ticket.title}`"
      base-path="/portal/attachments"
      :can-delete="false"
      @close="showAttachments = false"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import api from '../../services/api'
import AttachmentManager from '../../components/AttachmentManager.vue'

const items = ref<any[]>([])
const contracts = ref<any[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const formError = ref('')
const detailError = ref('')
const showCreateModal = ref(false)
const showAttachments = ref(false)
const detail = ref<any>(null)
const newComment = ref('')
const form = ref({ contractId: 0, propertyRoomId: 0, title: '', description: '', priority: 2 })

const statusText = (v: number) =>
  ({ 1: '已送出', 2: '處理中', 3: '已解決', 4: '已結案', 5: '已取消' } as Record<number, string>)[v] || String(v)
const statusBadge = (v: number) =>
  ({ 1: 'badge-warning', 2: 'badge-info', 3: 'badge-success', 4: 'badge-ghost', 5: 'badge-ghost' } as Record<number, string>)[v] || ''
const priorityText = (v: number) => ({ 1: '低', 2: '一般', 3: '高', 4: '緊急' } as Record<number, string>)[v] || String(v)
const priorityBadge = (v: number) => (v >= 4 ? 'badge-error' : v === 3 ? 'badge-warning' : 'badge-ghost')

const selectedContractRooms = computed(() => {
  const contract = contracts.value.find((c: any) => c.id === form.value.contractId)
  return contract?.rooms || []
})

const load = async () => {
  error.value = ''
  try {
    const { data } = await api.get('/portal/repair-tickets')
    items.value = data
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入報修紀錄失敗'
  } finally {
    loading.value = false
  }
}

const openCreateModal = () => {
  form.value = {
    contractId: contracts.value.length === 1 ? contracts.value[0].id : 0,
    propertyRoomId: 0,
    title: '',
    description: '',
    priority: 2
  }
  formError.value = ''
  showCreateModal.value = true
}

const save = async () => {
  formError.value = ''
  if (!form.value.contractId) { formError.value = '請選擇合約'; return }
  if (!form.value.title.trim()) { formError.value = '請輸入標題'; return }
  if (!form.value.description.trim()) { formError.value = '請描述報修內容'; return }

  saving.value = true
  try {
    const { data } = await api.post('/portal/repair-tickets', {
      contractId: form.value.contractId,
      propertyRoomId: form.value.propertyRoomId || null,
      title: form.value.title,
      description: form.value.description,
      priority: form.value.priority
    })
    showCreateModal.value = false
    await load()
    await openDetail(data.id)
  } catch (e: any) {
    formError.value = e?.response?.data || e?.message || '送出報修失敗'
  } finally {
    saving.value = false
  }
}

const openDetail = async (id: number) => {
  detailError.value = ''
  newComment.value = ''
  try {
    const { data } = await api.get(`/portal/repair-tickets/${id}`)
    detail.value = data
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入明細失敗'
  }
}

const addComment = async () => {
  if (!newComment.value.trim() || !detail.value) return
  detailError.value = ''
  try {
    await api.post(`/portal/repair-tickets/${detail.value.ticket.id}/comments`, { content: newComment.value })
    newComment.value = ''
    await openDetail(detail.value.ticket.id)
  } catch (e: any) {
    detailError.value = e?.response?.data || e?.message || '留言失敗'
  }
}

onMounted(async () => {
  try {
    const { data } = await api.get('/portal/contracts')
    contracts.value = data.filter((c: any) => c.status === 1)
  } catch { /* ignore */ }
  await load()
})
</script>
