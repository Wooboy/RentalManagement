<template>
  <div class="space-y-4">
    <PageHeader title="報修管理" :subtitle="`共 ${items.length} 筆`" />

    <div class="card bg-base-100 border border-base-300 shadow-sm p-3">
      <div class="flex flex-wrap items-end gap-3">
        <label class="form-control">
          <span class="label-text mb-1">狀態</span>
          <select v-model.number="statusFilter" class="select select-bordered select-sm" @change="load">
            <option :value="0">全部</option>
            <option :value="1">已送出</option>
            <option :value="2">處理中</option>
            <option :value="3">已解決</option>
            <option :value="4">已結案</option>
            <option :value="5">已取消</option>
          </select>
        </label>
        <label class="form-control w-64">
          <span class="label-text mb-1">關鍵字</span>
          <input v-model="keyword" class="input input-bordered input-sm" placeholder="標題、合約、租客" @keyup.enter="load" />
        </label>
        <button class="btn btn-sm btn-ghost" @click="load">查詢</button>
      </div>
    </div>

    <p v-if="error" class="text-error text-sm">{{ error }}</p>

    <div class="card bg-base-100 border border-base-300 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="table">
          <thead>
            <tr>
              <th>#</th>
              <th>標題</th>
              <th>租客</th>
              <th>房源/房間</th>
              <th>優先度</th>
              <th>狀態</th>
              <th>建立時間</th>
              <th class="text-right">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="t in items" :key="t.id">
              <td class="tabular-nums text-base-content/60">{{ t.id }}</td>
              <td class="max-w-52 truncate font-medium">{{ t.title }}</td>
              <td>{{ t.tenantName || '-' }}</td>
              <td>{{ t.propertyUnitName || '-' }}{{ t.propertyRoomName ? ` / ${t.propertyRoomName}` : '' }}</td>
              <td><span class="badge badge-sm" :class="priorityBadge(t.priority)">{{ priorityText(t.priority) }}</span></td>
              <td><span class="badge badge-sm" :class="statusBadge(t.status)">{{ statusText(t.status) }}</span></td>
              <td class="whitespace-nowrap tabular-nums">{{ t.createdAtUtc?.slice(0, 16).replace('T', ' ') }}</td>
              <td class="text-right"><button class="btn btn-xs btn-ghost" @click="openDetail(t.id)">處理</button></td>
            </tr>
            <tr v-if="!loading && items.length === 0"><td colspan="8" class="text-center text-base-content/50 py-10">沒有符合條件的報修單。</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 明細/處理 -->
    <dialog class="modal" :class="{ 'modal-open': !!detail }">
      <div v-if="detail" class="modal-box max-w-2xl max-h-[85vh] overflow-y-auto">
        <div class="flex items-start justify-between gap-2">
          <h3 class="font-bold text-lg">#{{ detail.ticket.id }} {{ detail.ticket.title }}</h3>
          <span class="badge" :class="statusBadge(detail.ticket.status)">{{ statusText(detail.ticket.status) }}</span>
        </div>
        <p class="text-sm text-base-content/60 mt-1">
          {{ detail.ticket.tenantName }} · {{ detail.ticket.contractName }} ·
          {{ detail.ticket.propertyUnitName || '' }}{{ detail.ticket.propertyRoomName ? ` / ${detail.ticket.propertyRoomName}` : '' }}
        </p>
        <p class="text-sm text-base-content/60">
          建立於 {{ detail.ticket.createdAtUtc?.slice(0, 16).replace('T', ' ') }}
          <template v-if="detail.ticket.handledByName">· 處理人 {{ detail.ticket.handledByName }}</template>
        </p>
        <p class="mt-3 whitespace-pre-wrap">{{ detail.ticket.description }}</p>

        <div class="flex flex-wrap items-end gap-2 mt-4">
          <label class="form-control">
            <span class="label-text mb-1">更新狀態</span>
            <select v-model.number="nextStatus" class="select select-bordered select-sm">
              <option :value="1">已送出</option>
              <option :value="2">處理中</option>
              <option :value="3">已解決</option>
              <option :value="4">已結案</option>
              <option :value="5">已取消</option>
            </select>
          </label>
          <button class="btn btn-sm btn-primary" @click="updateStatus">更新</button>
          <button class="btn btn-sm" @click="showAttachments = true">照片/附件</button>
          <button class="btn btn-sm btn-error ml-auto" @click="removeTicket">刪除</button>
        </div>

        <div class="divider my-2">處理紀錄</div>
        <p v-if="detail.comments.length === 0" class="text-sm text-base-content/60 text-center py-2">尚無留言</p>
        <div class="space-y-2">
          <div
            v-for="c in detail.comments"
            :key="c.id"
            class="chat"
            :class="c.isAdmin ? 'chat-end' : 'chat-start'"
          >
            <div class="chat-header text-xs text-base-content/60">
              {{ c.userName || '-' }} · {{ c.createdAtUtc?.slice(0, 16).replace('T', ' ') }}
            </div>
            <div class="chat-bubble chat-bubble-sm whitespace-pre-wrap" :class="c.isAdmin ? 'chat-bubble-primary' : ''">
              {{ c.content }}
            </div>
          </div>
        </div>

        <div class="flex gap-2 mt-4">
          <input v-model="newComment" class="input input-bordered input-sm flex-1" placeholder="回覆租客..." @keyup.enter="addComment" />
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
      @close="showAttachments = false"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
import AttachmentManager from '../components/AttachmentManager.vue'
import PageHeader from '../components/PageHeader.vue'

const items = ref<any[]>([])
const statusFilter = ref(0)
const keyword = ref('')
const loading = ref(true)
const error = ref('')
const detail = ref<any>(null)
const detailError = ref('')
const nextStatus = ref(1)
const newComment = ref('')
const showAttachments = ref(false)

const statusText = (v: number) =>
  ({ 1: '已送出', 2: '處理中', 3: '已解決', 4: '已結案', 5: '已取消' } as Record<number, string>)[v] || String(v)
const statusBadge = (v: number) =>
  ({ 1: 'badge-warning', 2: 'badge-info', 3: 'badge-success', 4: 'badge-ghost', 5: 'badge-ghost' } as Record<number, string>)[v] || ''
const priorityText = (v: number) => ({ 1: '低', 2: '一般', 3: '高', 4: '緊急' } as Record<number, string>)[v] || String(v)
const priorityBadge = (v: number) => (v >= 4 ? 'badge-error' : v === 3 ? 'badge-warning' : 'badge-ghost')

const load = async () => {
  error.value = ''
  try {
    const params: any = {}
    if (statusFilter.value > 0) params.status = statusFilter.value
    if (keyword.value.trim()) params.keyword = keyword.value.trim()
    const { data } = await api.get('/repair-tickets', { params })
    items.value = data
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入報修單失敗'
  } finally {
    loading.value = false
  }
}

const openDetail = async (id: number) => {
  detailError.value = ''
  newComment.value = ''
  showAttachments.value = false
  try {
    const { data } = await api.get(`/repair-tickets/${id}`)
    detail.value = data
    nextStatus.value = data.ticket.status
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入明細失敗'
  }
}

const updateStatus = async () => {
  if (!detail.value) return
  detailError.value = ''
  try {
    await api.put(`/repair-tickets/${detail.value.ticket.id}/status`, { status: nextStatus.value })
    await openDetail(detail.value.ticket.id)
    await load()
  } catch (e: any) {
    detailError.value = e?.response?.data || e?.message || '更新狀態失敗'
  }
}

const removeTicket = async () => {
  if (!detail.value) return
  if (!window.confirm(`確定刪除報修單 #${detail.value.ticket.id}？附件將一併刪除。`)) return
  detailError.value = ''
  try {
    await api.delete(`/repair-tickets/${detail.value.ticket.id}`)
    detail.value = null
    await load()
  } catch (e: any) {
    detailError.value = e?.response?.data || e?.message || '刪除失敗'
  }
}

const addComment = async () => {
  if (!newComment.value.trim() || !detail.value) return
  detailError.value = ''
  try {
    await api.post(`/repair-tickets/${detail.value.ticket.id}/comments`, { content: newComment.value })
    newComment.value = ''
    await openDetail(detail.value.ticket.id)
  } catch (e: any) {
    detailError.value = e?.response?.data || e?.message || '留言失敗'
  }
}

onMounted(load)
</script>
