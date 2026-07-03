<template>
  <div class="space-y-4">
    <p v-if="error" class="text-error text-sm">{{ error }}</p>
    <p v-if="!loading && items.length === 0" class="text-sm text-base-content/60 py-8 text-center">目前沒有合約資料</p>

    <div v-for="c in items" :key="c.id" class="card bg-base-100 shadow">
      <div class="card-body p-5">
        <div class="flex items-start justify-between gap-2 flex-wrap">
          <div>
            <h3 class="card-title text-base">{{ c.contractName || c.contractNo }}</h3>
            <p class="text-sm text-base-content/60">{{ c.propertyName }}</p>
          </div>
          <span class="badge" :class="c.status === 1 ? 'badge-success' : 'badge-ghost'">
            {{ statusText(c.status) }}
          </span>
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 mt-2 text-sm">
          <div>
            <p class="text-base-content/60">租期</p>
            <p>{{ c.startDateUtc?.slice(0, 10) }} ~ {{ c.endDateUtc?.slice(0, 10) }}</p>
          </div>
          <div>
            <p class="text-base-content/60">月租</p>
            <p>${{ Number(c.monthlyRent).toLocaleString() }}</p>
          </div>
          <div>
            <p class="text-base-content/60">押金</p>
            <p>${{ Number(c.deposit).toLocaleString() }}</p>
          </div>
          <div>
            <p class="text-base-content/60">房間</p>
            <p>{{ c.rooms?.map((r: any) => r.roomName || r.roomCode).join('、') || '-' }}</p>
          </div>
        </div>

        <div class="card-actions justify-end mt-2">
          <button class="btn btn-sm" @click="attachmentTarget = c">合約附件</button>
        </div>
      </div>
    </div>

    <AttachmentManager
      v-if="attachmentTarget"
      :entity-type="1"
      :entity-id="attachmentTarget.id"
      :subtitle="`合約：${attachmentTarget.contractName || attachmentTarget.contractNo}`"
      base-path="/portal/attachments"
      readonly
      :can-delete="false"
      @close="attachmentTarget = null"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../../services/api'
import AttachmentManager from '../../components/AttachmentManager.vue'

const items = ref<any[]>([])
const loading = ref(true)
const error = ref('')
const attachmentTarget = ref<any>(null)

const statusText = (v: number) => (v === 1 ? '生效中' : v === 2 ? '已到期' : '已終止')

onMounted(async () => {
  try {
    const { data } = await api.get('/portal/contracts')
    items.value = data
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入合約失敗'
  } finally {
    loading.value = false
  }
})
</script>
